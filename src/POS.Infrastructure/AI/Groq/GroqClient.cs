using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.AI;

namespace POS.Infrastructure.AI.Groq;

public sealed class GroqClient(
    HttpClient httpClient,
    IOptions<GroqOptions> options,
    ILogger<GroqClient> logger) : IAiModelClient
{
    private readonly GroqOptions _options = options.Value;

    public async Task<AiGenerationResponse> GenerateAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new AiModelException(
                "AI_DISABLED",
                "Trợ lý AI hiện chưa được bật.",
                503);
        }

        var messages = BuildMessages(request);
        var tools = BuildTools(request.Tools);

        var body = new GroqChatCompletionRequest(
            _options.Model,
            messages,
            _options.Temperature,
            _options.MaxCompletionTokens,
            Stream: false,
            Tools: tools,
            ToolChoice: tools is { Count: > 0 } ? "auto" : null);

        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Groq request timed out after {ElapsedMs} ms.", stopwatch.ElapsedMilliseconds);
            throw new AiModelException(
                "AI_TIMEOUT",
                "Trợ lý AI phản hồi quá lâu. Vui lòng thử lại.",
                504);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Groq could not be reached.");
            throw new AiModelException(
                "AI_UNAVAILABLE",
                "Không thể kết nối tới dịch vụ AI. Vui lòng thử lại sau.",
                503);
        }

        using (response)
        {
            var providerRequestId = GetProviderRequestId(response);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowMappedProviderErrorAsync(response, providerRequestId, cancellationToken);
            }

            GroqChatCompletionResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<GroqChatCompletionResponse>(cancellationToken);
            }
            catch (JsonException)
            {
                logger.LogWarning(
                    "Groq returned a response with an invalid JSON shape. ProviderRequestId: {ProviderRequestId}",
                    providerRequestId);
                throw CreateInvalidResponseException();
            }

            var effectiveRequestId = result?.Id ?? providerRequestId;
            var choice = result?.Choices?.FirstOrDefault();
            if (choice?.Message is null)
            {
                logger.LogWarning(
                    "Groq response contained no valid choice message. ProviderRequestId: {ProviderRequestId}",
                    effectiveRequestId);
                throw CreateInvalidResponseException();
            }

            var isToolCallsFinish = string.Equals(choice.FinishReason, "tool_calls", StringComparison.OrdinalIgnoreCase);
            var hasToolCalls = choice.Message.ToolCalls is { Count: > 0 };

            if (isToolCallsFinish || hasToolCalls)
            {
                var parsedCalls = ParseToolCalls(choice.Message.ToolCalls, effectiveRequestId);
                var assistantText = string.IsNullOrWhiteSpace(choice.Message.Content)
                    ? null
                    : choice.Message.Content;

                stopwatch.Stop();
                logger.LogInformation(
                    "Groq tool-call turn completed in {ElapsedMs} ms. Model: {Model}; ToolCallCount: {ToolCallCount}; InputTokens: {InputTokens}; OutputTokens: {OutputTokens}; ProviderRequestId: {ProviderRequestId}",
                    stopwatch.ElapsedMilliseconds,
                    _options.Model,
                    parsedCalls.Count,
                    result?.Usage?.InputTokens,
                    result?.Usage?.OutputTokens,
                    effectiveRequestId);

                return AiGenerationResponse.RequiresAction(
                    parsedCalls,
                    assistantText,
                    effectiveRequestId,
                    result?.Usage?.InputTokens,
                    result?.Usage?.OutputTokens);
            }

            if (!IsSupportedCompletionFinishReason(choice.FinishReason))
            {
                logger.LogWarning(
                    "Groq returned unsupported finish_reason '{FinishReason}'. ProviderRequestId: {ProviderRequestId}",
                    choice.FinishReason,
                    effectiveRequestId);
                throw CreateInvalidResponseException();
            }

            var replyText = result?.Choices?
                .Select(c => c.Message?.Content)
                .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content));

            if (string.IsNullOrWhiteSpace(replyText))
            {
                logger.LogWarning(
                    "Groq response had no text output. FinishReason: {FinishReason}; ProviderRequestId: {ProviderRequestId}",
                    choice.FinishReason,
                    effectiveRequestId);
                throw new AiModelException(
                    "AI_EMPTY_RESPONSE",
                    "Trợ lý AI chưa tạo được câu trả lời. Vui lòng thử lại.",
                    502);
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Groq request completed in {ElapsedMs} ms. Model: {Model}; InputTokens: {InputTokens}; OutputTokens: {OutputTokens}; ProviderRequestId: {ProviderRequestId}",
                stopwatch.ElapsedMilliseconds,
                _options.Model,
                result?.Usage?.InputTokens,
                result?.Usage?.OutputTokens,
                effectiveRequestId);

            return AiGenerationResponse.Completed(
                replyText,
                effectiveRequestId,
                result?.Usage?.InputTokens,
                result?.Usage?.OutputTokens);
        }
    }

    private static List<GroqChatMessage> BuildMessages(AiGenerationRequest request)
    {
        var messages = new List<GroqChatMessage>(request.Messages.Count + 1)
        {
            new("system", request.SystemInstruction)
        };

        foreach (var turn in request.Messages)
        {
            if (string.Equals(turn.Role, "tool", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(turn.ToolCallId))
                {
                    throw new ArgumentException("Tool conversation turn must specify a non-empty ToolCallId.", nameof(request));
                }

                messages.Add(new GroqChatMessage(
                    Role: "tool",
                    Content: turn.Content ?? "{}",
                    ToolCallId: turn.ToolCallId,
                    Name: string.IsNullOrWhiteSpace(turn.Name) ? null : turn.Name));
                continue;
            }

            if (string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                if (turn.ToolCalls is { Count: > 0 })
                {
                    var payloads = turn.ToolCalls
                        .Select(call => new GroqToolCallPayload(
                            call.Id,
                            "function",
                            new GroqFunctionCallPayload(
                                call.Name,
                                call.Arguments.ValueKind == JsonValueKind.Undefined
                                    ? "{}"
                                    : call.Arguments.GetRawText())))
                        .ToArray();

                    messages.Add(new GroqChatMessage(
                        Role: "assistant",
                        Content: string.IsNullOrWhiteSpace(turn.Content) ? null : turn.Content,
                        ToolCalls: payloads));
                }
                else
                {
                    messages.Add(new GroqChatMessage(
                        Role: "assistant",
                        Content: turn.Content ?? string.Empty));
                }

                continue;
            }

            messages.Add(new GroqChatMessage(
                Role: "user",
                Content: turn.Content ?? string.Empty));
        }

        return messages;
    }

    private static IReadOnlyList<GroqToolDefinition>? BuildTools(IReadOnlyList<AiToolDefinition>? definitions)
    {
        if (definitions is null || definitions.Count == 0)
            return null;

        return definitions
            .Select(def => new GroqToolDefinition(
                def.Type,
                new GroqFunctionDefinition(def.Name, def.Description, def.Parameters)))
            .ToArray();
    }

    private IReadOnlyList<AiToolCall> ParseToolCalls(
        List<GroqToolCallPayload>? rawCalls,
        string? providerRequestId)
    {
        if (rawCalls is null || rawCalls.Count == 0)
        {
            logger.LogWarning(
                "Groq indicated tool_calls finish_reason without tool_calls items. ProviderRequestId: {ProviderRequestId}",
                providerRequestId);
            throw CreateInvalidResponseException();
        }

        var parsed = new List<AiToolCall>(rawCalls.Count);
        foreach (var raw in rawCalls)
        {
            if (string.IsNullOrWhiteSpace(raw.Id) ||
                (!string.IsNullOrWhiteSpace(raw.Type) &&
                 !string.Equals(raw.Type, "function", StringComparison.OrdinalIgnoreCase)) ||
                string.IsNullOrWhiteSpace(raw.Function?.Name))
            {
                logger.LogWarning(
                    "Groq returned a malformed tool_call entry. ProviderRequestId: {ProviderRequestId}",
                    providerRequestId);
                throw CreateInvalidResponseException();
            }

            var rawArgs = string.IsNullOrWhiteSpace(raw.Function.Arguments)
                ? "{}"
                : raw.Function.Arguments;

            JsonElement clonedArgs;
            try
            {
                using var doc = JsonDocument.Parse(rawArgs);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    logger.LogWarning(
                        "Groq tool_call arguments was not a JSON object. Tool: {ToolName}; ProviderRequestId: {ProviderRequestId}",
                        raw.Function.Name,
                        providerRequestId);
                    throw CreateInvalidResponseException();
                }

                clonedArgs = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                logger.LogWarning(
                    "Groq tool_call arguments contained invalid JSON. Tool: {ToolName}; ProviderRequestId: {ProviderRequestId}",
                    raw.Function.Name,
                    providerRequestId);
                throw CreateInvalidResponseException();
            }

            parsed.Add(new AiToolCall(raw.Id, raw.Function.Name, clonedArgs));
        }

        return parsed;
    }

    private async Task ThrowMappedProviderErrorAsync(
        HttpResponseMessage response,
        string? providerRequestId,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var providerErrorCode = await TryReadErrorCodeAsync(response, cancellationToken);
            if (string.Equals(providerErrorCode, "tool_use_failed", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Groq tool generation failed with code tool_use_failed. ProviderRequestId: {ProviderRequestId}",
                    providerRequestId);
                throw new AiModelException(
                    "AI_TOOL_USE_FAILED",
                    "Trợ lý AI chưa thể tra cứu chính xác cho yêu cầu này. Vui lòng diễn đạt cụ thể hơn và thử lại.",
                    502);
            }
        }

        var (code, safeMessage) = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                ("AI_CONFIGURATION_ERROR", "Dịch vụ AI chưa được cấu hình đúng."),
            (HttpStatusCode)429 =>
                ("AI_RATE_LIMITED", "Dịch vụ AI đang quá tải hoặc đã vượt giới hạn sử dụng."),
            _ when statusCode >= 500 =>
                ("AI_PROVIDER_UNAVAILABLE", "Dịch vụ AI đang tạm thời không khả dụng."),
            _ =>
                ("AI_PROVIDER_ERROR", "Không thể xử lý câu hỏi với dịch vụ AI.")
        };

        logger.LogWarning(
            "Groq request failed with status {StatusCode}. ProviderRequestId: {ProviderRequestId}",
            statusCode,
            providerRequestId);
        throw new AiModelException(code, safeMessage, 502);
    }

    private static async Task<string?> TryReadErrorCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<GroqErrorEnvelope>(cancellationToken);
            return envelope?.Error?.Code;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSupportedCompletionFinishReason(string? finishReason) =>
        string.Equals(finishReason, "stop", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase);

    private static AiModelException CreateInvalidResponseException() =>
        new(
            "AI_INVALID_RESPONSE",
            "Dịch vụ AI trả về kết quả không hợp lệ. Vui lòng thử lại.",
            502);

    private static string? GetProviderRequestId(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-request-id", out var requestIds))
            return requestIds.FirstOrDefault();

        return response.Headers.TryGetValues("x-groq-request-id", out var groqRequestIds)
            ? groqRequestIds.FirstOrDefault()
            : null;
    }
}

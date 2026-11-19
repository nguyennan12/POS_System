using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        var messages = new List<GroqChatMessage>(request.Messages.Count + 1)
        {
            new("system", request.SystemInstruction)
        };
        messages.AddRange(request.Messages.Select(turn => new GroqChatMessage(
            string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? "assistant"
                : "user",
            turn.Content)));

        var body = new GroqChatCompletionRequest(
            _options.Model,
            messages,
            _options.Temperature,
            _options.MaxCompletionTokens);

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
                var statusCode = (int)response.StatusCode;
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

            GroqChatCompletionResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<GroqChatCompletionResponse>(cancellationToken);
            }
            catch (System.Text.Json.JsonException)
            {
                logger.LogWarning(
                    "Groq returned a response with an invalid JSON shape. ProviderRequestId: {ProviderRequestId}",
                    providerRequestId);
                throw new AiModelException(
                    "AI_INVALID_RESPONSE",
                    "Dịch vụ AI trả về kết quả không hợp lệ. Vui lòng thử lại.",
                    502);
            }

            var replyText = result?.Choices?
                .Select(choice => choice.Message?.Content)
                .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content));

            if (string.IsNullOrWhiteSpace(replyText))
            {
                logger.LogWarning(
                    "Groq response had no text output. FinishReason: {FinishReason}; ProviderRequestId: {ProviderRequestId}",
                    result?.Choices?.FirstOrDefault()?.FinishReason,
                    result?.Id ?? providerRequestId);
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
                result?.Id ?? providerRequestId);

            return new AiGenerationResponse(
                replyText,
                result?.Id ?? providerRequestId,
                result?.Usage?.InputTokens,
                result?.Usage?.OutputTokens);
        }
    }

    private static string? GetProviderRequestId(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-request-id", out var requestIds))
            return requestIds.FirstOrDefault();

        return response.Headers.TryGetValues("x-groq-request-id", out var groqRequestIds)
            ? groqRequestIds.FirstOrDefault()
            : null;
    }
}

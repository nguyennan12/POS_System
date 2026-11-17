using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.AI;

namespace POS.Infrastructure.AI.Gemini;

public sealed class GeminiClient(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiClient> logger) : IAiModelClient
{
    private readonly GeminiOptions _options = options.Value;

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

        var input = request.Messages.Select(turn => new GeminiInputStep(
            turn.Role == "assistant" ? "model_output" : "user_input",
            [new GeminiTextContent("text", turn.Content)])).ToArray();

        var body = new GeminiInteractionRequest(
            _options.Model,
            input,
            request.SystemInstruction,
            new GeminiGenerationConfig(_options.Temperature, _options.MaxOutputTokens));

        using var message = new HttpRequestMessage(HttpMethod.Post, "interactions")
        {
            Content = JsonContent.Create(body)
        };
        message.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini request timed out after {ElapsedMs} ms.", stopwatch.ElapsedMilliseconds);
            throw new AiModelException(
                "AI_TIMEOUT",
                "Trợ lý AI phản hồi quá lâu. Vui lòng thử lại.",
                504);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Gemini could not be reached.");
            throw new AiModelException(
                "AI_UNAVAILABLE",
                "Không thể kết nối tới dịch vụ AI. Vui lòng thử lại sau.",
                503);
        }

        using (response)
        {
            var providerRequestId = response.Headers.TryGetValues("x-goog-request-id", out var values)
                ? values.FirstOrDefault()
                : null;

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
                    "Gemini request failed with status {StatusCode}. ProviderRequestId: {ProviderRequestId}",
                    statusCode,
                    providerRequestId);
                throw new AiModelException(code, safeMessage, 502);
            }

            GeminiInteractionResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<GeminiInteractionResponse>(cancellationToken);
            }
            catch (System.Text.Json.JsonException)
            {
                logger.LogWarning("Gemini returned a response with an invalid JSON shape. ProviderRequestId: {ProviderRequestId}", providerRequestId);
                throw new AiModelException(
                    "AI_INVALID_RESPONSE",
                    "Dịch vụ AI trả về kết quả không hợp lệ. Vui lòng thử lại.",
                    502);
            }

            var replyParts = result?.Steps?
                .Where(step => string.Equals(step.Type, "model_output", StringComparison.OrdinalIgnoreCase))
                .SelectMany(step => step.Content ?? [])
                .Where(content => string.Equals(content.Type, "text", StringComparison.OrdinalIgnoreCase))
                .Select(content => content.Text)
                .Where(part => !string.IsNullOrWhiteSpace(part));
            var replyText = replyParts is null ? null : string.Join("\n", replyParts);

            if (string.IsNullOrWhiteSpace(replyText) ||
                !string.Equals(result?.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Gemini response had no completed text output. Status: {Status}; ProviderRequestId: {ProviderRequestId}",
                    result?.Status,
                    providerRequestId);
                throw new AiModelException(
                    "AI_EMPTY_RESPONSE",
                    "Trợ lý AI chưa tạo được câu trả lời. Vui lòng thử lại.",
                    502);
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Gemini request completed in {ElapsedMs} ms. Model: {Model}; InputTokens: {InputTokens}; OutputTokens: {OutputTokens}; ProviderRequestId: {ProviderRequestId}",
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
}

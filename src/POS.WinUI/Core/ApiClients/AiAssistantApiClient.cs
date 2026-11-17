using System.Net.Http;
using POS.Contracts.V1.Chatbot;
using POS.Contracts.V1.Common;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

public sealed class AiAssistantApiClient(HttpClient http, SessionService session)
    : BaseApiClient(http, session)
{
    public async Task<AskAiResponse> AskAsync(
        AskAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await PostAsync<ApiResponse<AskAiResponse>>(
            ApiRoutes.AiAssistant.Ask,
            request,
            cancellationToken);

        return response?.Data
            ?? throw new InvalidOperationException("Không nhận được câu trả lời từ trợ lý AI.");
    }
}

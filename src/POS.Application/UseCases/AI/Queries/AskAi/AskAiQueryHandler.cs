using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.AI.Prompts;
using POS.Domain.Common;

namespace POS.Application.UseCases.AI.Queries.AskAi;

public sealed class AskAiQueryHandler(IAiModelClient aiModelClient)
    : IQueryHandler<AskAiQuery, string>
{
    public async Task<Result<string>> Handle(AskAiQuery query, CancellationToken cancellationToken)
    {
        var messages = query.History
            .TakeLast(19)
            .Append(AiConversationTurn.User(query.Message.Trim()))
            .ToArray();

        var response = await aiModelClient.GenerateAsync(
            new AiGenerationRequest(AiSystemPrompt.BasicAssistant, messages),
            cancellationToken);

        if (response.Status != AiModelStatus.Completed || string.IsNullOrWhiteSpace(response.Text))
        {
            throw new AiModelException(
                "AI_INVALID_RESPONSE",
                "Dịch vụ AI trả về kết quả không hợp lệ. Vui lòng thử lại.",
                502);
        }

        return Result<string>.Success(response.Text);
    }
}

using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Messaging;
using POS.Domain.Common;
using POS.Application.UseCases.AI.Prompts;

namespace POS.Application.UseCases.AI.Queries.AskAi;

public sealed class AskAiQueryHandler(IAiModelClient aiModelClient)
    : IQueryHandler<AskAiQuery, string>
{
    public async Task<Result<string>> Handle(AskAiQuery query, CancellationToken cancellationToken)
    {
        var messages = query.History
            .TakeLast(19)
            .Append(new AiConversationTurn("user", query.Message.Trim()))
            .ToArray();

        var response = await aiModelClient.GenerateAsync(
            new AiGenerationRequest(AiSystemPrompt.BasicAssistant, messages),
            cancellationToken);

        return Result<string>.Success(response.Text);
    }
}

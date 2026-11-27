using System.Text.Json;
using System.Text.Json.Serialization;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.AI.Prompts;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Domain.Common;

namespace POS.Application.UseCases.AI.Queries.AskAi;

public sealed class AskAiQueryHandler(
    IAiModelClient aiModelClient,
    IAiToolRegistry toolRegistry,
    IAiToolExecutor toolExecutor) : IQueryHandler<AskAiQuery, string>
{
    private const int MaxHistoryTurns = 19;
    private const int MaxToolCallRounds = 3;

    private static readonly JsonSerializerOptions ToolResultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<Result<string>> Handle(AskAiQuery query, CancellationToken cancellationToken)
    {
        var conversation = new List<AiConversationTurn>(MaxHistoryTurns + 10);
        conversation.AddRange(query.History.TakeLast(MaxHistoryTurns));
        conversation.Add(AiConversationTurn.User(query.Message.Trim()));

        var availableTools = toolRegistry.GetAvailableTools();

        for (var round = 0; ; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await aiModelClient.GenerateAsync(
                new AiGenerationRequest(AiSystemPrompt.BasicAssistant, conversation, availableTools),
                cancellationToken);

            if (response.Status == AiModelStatus.Completed)
            {
                if (string.IsNullOrWhiteSpace(response.Text))
                {
                    throw CreateInvalidResponseException();
                }

                return Result<string>.Success(response.Text);
            }

            if (response.Status != AiModelStatus.RequiresAction ||
                response.ToolCalls is not { Count: > 0 } toolCalls ||
                toolCalls.Any(c => c is null || string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Name)))
            {
                throw CreateInvalidResponseException();
            }

            if (round >= MaxToolCallRounds)
            {
                return Result<string>.Failure(new Error(
                    ErrorType.Invalid,
                    AiToolErrors.AiToolLoopLimit,
                    "Yêu cầu cần quá nhiều bước tra cứu liên tiếp. Vui lòng thu hẹp câu hỏi."));
            }

            conversation.Add(AiConversationTurn.AssistantToolCalls(response.Text, toolCalls));

            // Thực thi tuần tự trên cùng scoped DbContext để đảm bảo an toàn luồng
            foreach (var call in toolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var toolResult = await toolExecutor.ExecuteAsync(call, cancellationToken);
                var serializedResult = JsonSerializer.Serialize(toolResult.Result, ToolResultSerializerOptions);
                conversation.Add(AiConversationTurn.Tool(toolResult.CallId, toolResult.Name, serializedResult));
            }
        }
    }

    private static AiModelException CreateInvalidResponseException() =>
        new(
            "AI_INVALID_RESPONSE",
            "Dịch vụ AI trả về kết quả không hợp lệ. Vui lòng thử lại.",
            502);
}

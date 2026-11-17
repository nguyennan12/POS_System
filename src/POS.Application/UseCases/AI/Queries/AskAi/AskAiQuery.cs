using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.AI.Queries.AskAi;

public sealed record AskAiQuery(
    string Message,
    IReadOnlyList<AiConversationTurn> History) : IQuery<string>;

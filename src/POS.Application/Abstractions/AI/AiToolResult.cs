using POS.Application.UseCases.AI.Tools.Common;

namespace POS.Application.Abstractions.AI;

public sealed record AiToolResult(
    string CallId,
    string Name,
    AiToolExecutionResult Result,
    bool IsError);

namespace POS.Application.Abstractions.AI;

public interface IAiToolExecutor
{
    Task<AiToolResult> ExecuteAsync(AiToolCall call, CancellationToken cancellationToken);
}

using System.Text.Json;
using POS.Application.UseCases.AI.Tools.Common;

namespace POS.Application.Abstractions.AI;

public interface IAiToolHandler
{
    string Name { get; }

    Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken);
}

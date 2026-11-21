namespace POS.Application.Abstractions.AI;

public interface IAiToolHandler
{
    string ToolName { get; }
    string Description { get; }
    string InputSchemaJson { get; }
    Task<string> ExecuteAsync(string inputJson, CancellationToken cancellationToken = default);
}

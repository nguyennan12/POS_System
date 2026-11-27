namespace POS.Application.Abstractions.AI;

public interface IAiToolRegistry
{
    IReadOnlyList<AiToolDefinition> GetAvailableTools();
}

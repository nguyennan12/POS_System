namespace POS.Application.Abstractions.AI;

public interface IAiToolRegistry
{
    IReadOnlyCollection<IAiToolHandler> GetAllTools();
    IAiToolHandler? GetTool(string toolName);
}

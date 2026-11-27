using System.Text.Json;

namespace POS.Application.Abstractions.AI;

public sealed record AiToolDefinition
{
    public string Type { get; }
    public string Name { get; }
    public string Description { get; }
    public JsonElement Parameters { get; }

    public AiToolDefinition(string type, string name, string description, JsonElement parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Type = type;
        Name = name;
        Description = description;
        Parameters = parameters.ValueKind == JsonValueKind.Undefined
            ? default
            : parameters.Clone();
    }

    public void Deconstruct(out string type, out string name, out string description, out JsonElement parameters)
    {
        type = Type;
        name = Name;
        description = Description;
        parameters = Parameters;
    }
}

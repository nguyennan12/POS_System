using System.Text.Json;

namespace POS.Application.Abstractions.AI;

public sealed record AiToolCall
{
    public string Id { get; }
    public string Name { get; }
    public JsonElement Arguments { get; }

    public AiToolCall(string id, string name, JsonElement arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name;
        Arguments = arguments.ValueKind == JsonValueKind.Undefined
            ? default
            : arguments.Clone();
    }

    public void Deconstruct(out string id, out string name, out JsonElement arguments)
    {
        id = Id;
        name = Name;
        arguments = Arguments;
    }
}

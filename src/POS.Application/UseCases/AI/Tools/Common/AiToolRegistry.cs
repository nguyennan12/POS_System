using System.Collections.ObjectModel;
using System.Text.Json;
using POS.Application.Abstractions.AI;

namespace POS.Application.UseCases.AI.Tools.Common;

public sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly ReadOnlyCollection<AiToolDefinition> _definitions;

    public AiToolRegistry(IEnumerable<AiToolDefinition>? additionalDefinitions = null)
    {
        var combined = new List<AiToolDefinition>(25);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in AiToolSchemaFactory.CreateCoreR1Definitions())
        {
            AddValidatedDefinition(definition, combined, seenNames);
        }

        if (additionalDefinitions is not null)
        {
            foreach (var definition in additionalDefinitions)
            {
                AddValidatedDefinition(definition, combined, seenNames);
            }
        }

        _definitions = combined.AsReadOnly();
    }

    public IReadOnlyList<AiToolDefinition> GetAvailableTools() => _definitions;

    private static void AddValidatedDefinition(
        AiToolDefinition? definition,
        List<AiToolDefinition> target,
        HashSet<string> seenNames)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!string.Equals(definition.Type, "function", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"AI tool '{definition.Name}' must have type 'function', but found '{definition.Type}'.");
        }

        if (!seenNames.Add(definition.Name))
        {
            throw new InvalidOperationException(
                $"Duplicate AI tool definition registered for name '{definition.Name}'.");
        }

        ValidateRootParametersSchema(definition);
        target.Add(definition);
    }

    private static void ValidateRootParametersSchema(AiToolDefinition definition)
    {
        if (definition.Parameters.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"AI tool '{definition.Name}' parameters schema must be a JSON object.");
        }

        if (!definition.Parameters.TryGetProperty("type", out var typeProp) ||
            typeProp.ValueKind != JsonValueKind.String ||
            !string.Equals(typeProp.GetString(), "object", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"AI tool '{definition.Name}' root schema must specify type='object'.");
        }

        if (!definition.Parameters.TryGetProperty("additionalProperties", out var additionalProp) ||
            additionalProp.ValueKind != JsonValueKind.False)
        {
            throw new InvalidOperationException(
                $"AI tool '{definition.Name}' root schema must set additionalProperties=false.");
        }
    }
}

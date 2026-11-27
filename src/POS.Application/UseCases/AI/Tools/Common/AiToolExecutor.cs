using System.Text.Json;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.AI;

namespace POS.Application.UseCases.AI.Tools.Common;

public sealed class AiToolExecutor : IAiToolExecutor
{
    private readonly Dictionary<string, IAiToolHandler> _handlers;
    private readonly ILogger<AiToolExecutor> _logger;

    public AiToolExecutor(
        IEnumerable<IAiToolHandler> handlers,
        ILogger<AiToolExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _handlers = new Dictionary<string, IAiToolHandler>(StringComparer.OrdinalIgnoreCase);

        foreach (var handler in handlers)
        {
            if (handler is null)
                throw new InvalidOperationException("Registered IAiToolHandler instance cannot be null.");

            if (string.IsNullOrWhiteSpace(handler.Name))
            {
                throw new InvalidOperationException(
                    $"AI tool handler '{handler.GetType().Name}' must specify a non-empty Name.");
            }

            if (!_handlers.TryAdd(handler.Name, handler))
            {
                throw new InvalidOperationException(
                    $"Duplicate AI tool handler registered for tool name '{handler.Name}'.");
            }
        }
    }

    public async Task<AiToolResult> ExecuteAsync(
        AiToolCall call,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_handlers.TryGetValue(call.Name, out var handler))
        {
            return new AiToolResult(
                call.Id,
                call.Name,
                AiToolErrors.UnknownTool(call.Name),
                IsError: true);
        }

        if (call.Arguments.ValueKind != JsonValueKind.Object)
        {
            return new AiToolResult(
                call.Id,
                handler.Name,
                AiToolErrors.InvalidArgument("Tham số gọi công cụ phải là một đối tượng JSON."),
                IsError: true);
        }

        try
        {
            var executionResult = await handler.ExecuteAsync(call.Arguments, cancellationToken);
            if (executionResult is null)
            {
                _logger.LogError(
                    "AI tool handler '{ToolName}' returned a null AiToolExecutionResult.",
                    handler.Name);

                var fallbackError = AiToolErrors.ExecutionFailed();
                return new AiToolResult(call.Id, handler.Name, fallbackError, IsError: true);
            }

            return new AiToolResult(
                call.Id,
                handler.Name,
                executionResult,
                IsError: !executionResult.Success);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred while executing AI tool '{ToolName}'.",
                handler.Name);

            return new AiToolResult(
                call.Id,
                handler.Name,
                AiToolErrors.ExecutionFailed(),
                IsError: true);
        }
    }
}

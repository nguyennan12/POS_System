using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Orders.Queries.GetAccessibleOrders;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.AI.Tools.Orders;

public sealed record OrderSummaryToolItemDto(
    Guid OrderId,
    string Status,
    string CurrencyCode,
    decimal GrandTotal,
    int TotalItems,
    Guid? CustomerId,
    string? CustomerName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);

public sealed class ListOrdersToolHandler(ISender sender) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "status", "from", "to", "limit" };

    public string Name => AiToolNames.ListOrders;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadOptionalEnum<OrderStatus>(arguments, "status", out var status, out _, out error) ||
            !AiToolArgumentReader.TryReadOptionalDateRangeUtc(arguments, "from", "to", out var fromUtc, out var toUtc, out error) ||
            !AiToolArgumentReader.TryReadOptionalInt(arguments, "limit", defaultValue: 20, min: 1, max: 50, out var limit, out error))
        {
            return error!;
        }

        var result = await sender.Send(
            new GetAccessibleOrdersQuery(status, fromUtc, toUtc, limit),
            cancellationToken);

        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var safeItems = result.Value.Items
            .Select(order => new OrderSummaryToolItemDto(
                OrderId: order.Id,
                Status: order.Status,
                CurrencyCode: order.CurrencyCode,
                GrandTotal: order.GrandTotal,
                TotalItems: order.TotalItems,
                CustomerId: order.CustomerId,
                CustomerName: order.CustomerName,
                CreatedAt: order.CreatedAt,
                PaidAt: order.PaidAt))
            .ToArray();

        return AiToolExecutionResult.Ok(safeItems, safeItems.Length);
    }
}

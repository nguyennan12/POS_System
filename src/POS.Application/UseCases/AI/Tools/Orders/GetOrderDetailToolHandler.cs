using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Orders.Queries.GetAccessibleOrderById;

namespace POS.Application.UseCases.AI.Tools.Orders;

public sealed record OrderItemToolDto(
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal Qty,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LineTotal);

public sealed record OrderDiscountToolDto(
    Guid? PromotionId,
    Guid? VoucherId,
    decimal DiscountAmount,
    string? Description);

public sealed record OrderPaymentToolDto(
    string Method,
    decimal Amount,
    decimal? ChangeAmount,
    string Status,
    DateTimeOffset? PaidAt);

public sealed record OrderDetailToolDto(
    Guid OrderId,
    string Status,
    string CurrencyCode,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    Guid? CustomerId,
    string? CustomerName,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    IReadOnlyList<OrderItemToolDto> Items,
    IReadOnlyList<OrderDiscountToolDto> Discounts,
    IReadOnlyList<OrderPaymentToolDto> Payments);

public sealed class GetOrderDetailToolHandler(ISender sender) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "orderId" };

    public string Name => AiToolNames.GetOrderDetail;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadRequiredGuid(arguments, "orderId", out var orderId, out error))
        {
            return error!;
        }

        var result = await sender.Send(new GetAccessibleOrderByIdQuery(orderId), cancellationToken);
        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var order = result.Value;
        var safeDetail = new OrderDetailToolDto(
            OrderId: order.Id,
            Status: order.Status,
            CurrencyCode: order.CurrencyCode,
            Subtotal: order.Subtotal,
            DiscountTotal: order.DiscountTotal,
            TaxTotal: order.TaxTotal,
            GrandTotal: order.GrandTotal,
            CustomerId: order.CustomerId,
            CustomerName: order.CustomerName,
            Note: order.Note,
            CreatedAt: order.CreatedAt,
            PaidAt: order.PaidAt,
            Items: order.Items
                .Select(i => new OrderItemToolDto(
                    i.SkuId,
                    i.SkuCode,
                    i.ProductName,
                    i.Qty,
                    i.UnitPrice,
                    i.DiscountAmount,
                    i.TaxAmount,
                    i.LineTotal))
                .ToArray(),
            Discounts: order.Discounts
                .Select(d => new OrderDiscountToolDto(
                    d.PromotionId,
                    d.VoucherId,
                    d.DiscountAmount,
                    d.Description))
                .ToArray(),
            Payments: order.Payments
                .Select(p => new OrderPaymentToolDto(
                    p.Method,
                    p.Amount,
                    p.ChangeAmount,
                    p.Status,
                    p.PaidAt))
                .ToArray());

        return AiToolExecutionResult.Ok(safeDetail, 1);
    }
}

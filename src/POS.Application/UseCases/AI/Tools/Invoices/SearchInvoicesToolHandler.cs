using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Invoices.Queries.GetInvoices;
using POS.Contracts.V1.Invoices;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.AI.Tools.Invoices;

public sealed record InvoiceSummaryToolItemDto(
    Guid InvoiceId,
    Guid OrderId,
    string InvoiceNo,
    string? BuyerName,
    decimal TotalBeforeTax,
    decimal TaxAmount,
    decimal GrandTotal,
    DateTimeOffset IssuedAt);

public sealed class SearchInvoicesToolHandler(
    ISender sender,
    IStoreAccessPolicy storeAccessPolicy) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "search", "orderId", "from", "to", "paymentMethod", "limit" };

    public string Name => AiToolNames.SearchInvoices;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadOptionalString(arguments, "search", maxLength: 100, out var search, out error) ||
            !AiToolArgumentReader.TryReadOptionalGuid(arguments, "orderId", out var orderId, out error) ||
            !AiToolArgumentReader.TryReadOptionalDateRangeUtc(arguments, "from", "to", out var fromUtc, out var toUtc, out error) ||
            !AiToolArgumentReader.TryReadOptionalEnum<PaymentMethod>(arguments, "paymentMethod", out _, out var paymentMethodName, out error) ||
            !AiToolArgumentReader.TryReadOptionalInt(arguments, "limit", defaultValue: 20, min: 1, max: 50, out var limit, out error))
        {
            return error!;
        }

        var storeContext = storeAccessPolicy.EnforceStoreAccess();
        if (storeContext.IsFailure)
        {
            return AiToolErrors.FromDomainError(storeContext.Error);
        }

        var filter = new InvoiceFilterRequest(
            StoreId: storeContext.Value,
            OrderId: orderId,
            From: fromUtc,
            To: toUtc,
            PageNumber: 1,
            PageSize: limit,
            Search: search,
            PaymentMethod: paymentMethodName);

        var result = await sender.Send(new GetInvoicesQuery(filter), cancellationToken);
        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var safeItems = result.Value.Items
            .Select(inv => new InvoiceSummaryToolItemDto(
                InvoiceId: inv.Id,
                OrderId: inv.OrderId,
                InvoiceNo: inv.InvoiceNo,
                BuyerName: inv.BuyerName,
                TotalBeforeTax: inv.TotalBeforeTax,
                TaxAmount: inv.TaxAmount,
                GrandTotal: inv.GrandTotal,
                IssuedAt: inv.IssuedAt))
            .ToArray();

        return AiToolExecutionResult.Ok(safeItems, safeItems.Length);
    }
}

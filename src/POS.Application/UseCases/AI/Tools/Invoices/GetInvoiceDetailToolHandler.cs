using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Invoices.Queries.GetInvoiceById;

namespace POS.Application.UseCases.AI.Tools.Invoices;

public sealed record InvoiceItemToolDto(
    Guid SkuId,
    string ProductName,
    string SkuCode,
    decimal Qty,
    decimal UnitPrice,
    decimal TotalPrice);

public sealed record InvoiceDetailToolDto(
    Guid InvoiceId,
    Guid OrderId,
    string InvoiceNo,
    string? BuyerName,
    string? BuyerTaxCode,
    string? BuyerAddress,
    decimal TotalBeforeTax,
    decimal TaxAmount,
    decimal GrandTotal,
    DateTimeOffset IssuedAt,
    IReadOnlyList<InvoiceItemToolDto> Items);

public sealed class GetInvoiceDetailToolHandler(
    ISender sender,
    IStoreAccessPolicy storeAccessPolicy) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "invoiceId" };

    public string Name => AiToolNames.GetInvoiceDetail;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadRequiredGuid(arguments, "invoiceId", out var invoiceId, out error))
        {
            return error!;
        }

        var storeContext = storeAccessPolicy.EnforceStoreAccess();
        if (storeContext.IsFailure)
        {
            return AiToolErrors.FromDomainError(storeContext.Error);
        }

        var result = await sender.Send(new GetInvoiceByIdQuery(invoiceId), cancellationToken);
        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var invoice = result.Value;
        var safeDetail = new InvoiceDetailToolDto(
            InvoiceId: invoice.Id,
            OrderId: invoice.OrderId,
            InvoiceNo: invoice.InvoiceNo,
            BuyerName: invoice.BuyerName,
            BuyerTaxCode: invoice.BuyerTaxCode,
            BuyerAddress: invoice.BuyerAddress,
            TotalBeforeTax: invoice.TotalBeforeTax,
            TaxAmount: invoice.TaxAmount,
            GrandTotal: invoice.GrandTotal,
            IssuedAt: invoice.IssuedAt,
            Items: invoice.Items
                .Select(i => new InvoiceItemToolDto(
                    i.SkuId,
                    i.ProductName,
                    i.SkuCode,
                    i.Qty,
                    i.UnitPrice,
                    i.TotalPrice))
                .ToArray());

        return AiToolExecutionResult.Ok(safeDetail, 1);
    }
}

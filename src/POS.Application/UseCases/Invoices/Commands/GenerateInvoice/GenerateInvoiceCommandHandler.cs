using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Application.UseCases.Stores.Errors;

namespace POS.Application.UseCases.Invoices.Commands.GenerateInvoice;

public sealed class GenerateInvoiceCommandHandler(
    IOrderRepository orderRepository,
    IStoreRepository storeRepository,
    IInvoiceRepository invoiceRepository,
    ILogger<GenerateInvoiceCommandHandler> logger) : ICommandHandler<GenerateInvoiceCommand>
{
    public async Task<Result> Handle(GenerateInvoiceCommand command, CancellationToken cancellationToken)
    {
        // Tracking preserves the Paid state set by checkout before SaveChanges.
        var order = await orderRepository.GetByIdWithDetailsAsync(command.OrderId, cancellationToken);
        if (order is null) return Result.Failure(OrderErrors.OrderNotFound);
        if (order.Status != OrderStatus.Paid) return Result.Failure(InvoiceErrors.OrderNotPaid);
        if (await invoiceRepository.ExistsForOrderAsync(order.Id, cancellationToken))
            return Result.Failure(InvoiceErrors.AlreadyInvoiced);

        var store = await storeRepository.GetByIdAsync(order.StoreId, cancellationToken);
        if (store is null) return Result.Failure(StoreErrors.StoreNotFound);

        TimeZoneInfo timezone;
        try
        {
            timezone = TimeZoneInfo.FindSystemTimeZoneById(store.Timezone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning(ex, "Invalid timezone {Timezone} for store {StoreId}; using UTC for invoice date.",
                store.Timezone, store.Id);
            timezone = TimeZoneInfo.Utc;
        }

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone).Date);
        var sequence = await invoiceRepository.GetNextSequenceAsync(order.StoreId, localDate, cancellationToken);
        var invoiceNo = FormattableString.Invariant($"HD-{store.Code}-{localDate:yyyyMMdd}-{sequence:D6}");
        var invoice = Invoice.Create(order.Id, invoiceNo, order.Subtotal, order.TaxTotal,
            order.GrandTotal, buyerName: order.Customer?.Name);
        await invoiceRepository.AddAsync(invoice, cancellationToken);
        return Result.Success();
    }
}

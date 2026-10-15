using MediatR;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Stores.Errors;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;

namespace POS.Application.UseCases.Orders.Events;

/// <summary>
/// Handles OrderPaidDomainEvent to generate the sequential electronic invoice number (Task T48).
/// Runs inside the same serializable transaction via the checkout sender dispatch.
/// </summary>
public sealed class OrderPaidInvoiceHandler(
    IOrderRepository orderRepository,
    IStoreRepository storeRepository,
    IInvoiceRepository invoiceRepository,
    ILogger<OrderPaidInvoiceHandler> logger) : INotificationHandler<OrderPaidDomainEvent>
{
    public async Task Handle(OrderPaidDomainEvent notification, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(notification.OrderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Paid)
            return;

        if (await invoiceRepository.ExistsForOrderAsync(order.Id, cancellationToken))
            return;

        var store = await storeRepository.GetByIdAsync(order.StoreId, cancellationToken);
        if (store is null)
        {
            logger.LogError("Store {StoreId} not found when generating invoice for order {OrderId}", order.StoreId, order.Id);
            return;
        }

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
    }
}

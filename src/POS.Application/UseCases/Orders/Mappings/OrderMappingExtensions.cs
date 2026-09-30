using POS.Application.UseCases.Orders.DTOs;
using POS.Domain.Orders;

namespace POS.Application.UseCases.Orders.Mappings;

public static class OrderMappingExtensions
{
    public static OrderDetailDto ToDetailDto(this Order order)
    {
        var items = order.Items.Select(i => new OrderItemDto(
            Id: i.Id,
            SkuId: i.SkuId,
            SkuCode: i.Sku?.SkuCode ?? string.Empty,
            ProductName: i.Sku?.Product?.Name ?? string.Empty,
            Qty: i.Qty,
            UnitPrice: i.UnitPrice,
            DiscountAmount: i.DiscountAmount,
            TaxAmount: i.TaxAmount,
            LineTotal: i.LineTotal
        )).ToList();

        var discounts = order.Discounts.Select(d => new OrderDiscountDto(
            Id: d.Id,
            PromotionId: d.PromotionId,
            VoucherId: d.VoucherId,
            DiscountAmount: d.DiscountAmount,
            Description: d.Description,
            AppliedAt: new DateTimeOffset(d.AppliedAt, TimeSpan.Zero)
        )).ToList();

        var payments = order.Payments.Select(p => new OrderPaymentDto(
            Id: p.Id,
            Method: p.Method.ToString(),
            Amount: p.Amount,
            ChangeAmount: p.ChangeAmount,
            TransactionRef: p.TransactionRef,
            Status: p.Status.ToString(),
            PaidAt: p.PaidAt.HasValue ? new DateTimeOffset(p.PaidAt.Value, TimeSpan.Zero) : null
        )).ToList();

        return new OrderDetailDto(
            Id: order.Id,
            StoreId: order.StoreId,
            ShiftId: order.ShiftId,
            CustomerId: order.CustomerId,
            CustomerName: order.Customer?.Name,
            CustomerPhone: order.Customer?.Phone,
            Status: order.Status.ToString(),
            CurrencyCode: order.CurrencyCode,
            Subtotal: order.Subtotal,
            DiscountTotal: order.DiscountTotal,
            TaxTotal: order.TaxTotal,
            GrandTotal: order.GrandTotal,
            Note: order.Note,
            CreatedBy: order.CreatedBy,
            CreatedAt: new DateTimeOffset(order.CreatedAt, TimeSpan.Zero),
            PaidAt: order.PaidAt.HasValue ? new DateTimeOffset(order.PaidAt.Value, TimeSpan.Zero) : null,
            Items: items,
            Discounts: discounts,
            Payments: payments
        );
    }

    public static OrderSummaryDto ToSummaryDto(this Order order)
    {
        return new OrderSummaryDto(
            Id: order.Id,
            StoreId: order.StoreId,
            ShiftId: order.ShiftId,
            CustomerId: order.CustomerId,
            CustomerName: order.Customer?.Name,
            CustomerPhone: order.Customer?.Phone,
            Status: order.Status.ToString(),
            CurrencyCode: order.CurrencyCode,
            GrandTotal: order.GrandTotal,
            TotalItems: (int)order.Items.Sum(i => i.Qty),
            CreatedAt: new DateTimeOffset(order.CreatedAt, TimeSpan.Zero),
            PaidAt: order.PaidAt.HasValue ? new DateTimeOffset(order.PaidAt.Value, TimeSpan.Zero) : null
        );
    }

    public static CheckoutDto ToCheckoutDto(this Order order, POS.Domain.Employees.Employee employee, POS.Domain.Stores.Store store)
    {
        var (totalPaid, changeAmount) = order.GetPaymentTotals();

        ReceiptDataDto? receiptData = null;
        if (order.Status == POS.Domain.Orders.Enums.OrderStatus.Paid)
        {
            var itemsDto = order.Items.Select(i => new OrderItemDto(
                Id: i.Id,
                SkuId: i.SkuId,
                SkuCode: i.Sku?.SkuCode ?? string.Empty,
                ProductName: i.Sku?.Product?.Name ?? string.Empty,
                Qty: i.Qty,
                UnitPrice: i.UnitPrice,
                DiscountAmount: i.DiscountAmount,
                TaxAmount: i.TaxAmount,
                LineTotal: i.LineTotal
            )).ToList();

            receiptData = new ReceiptDataDto(
                StoreName: order.Store?.Name ?? string.Empty,
                StoreAddress: order.Store?.Address,
                StorePhone: order.Store?.Phone,
                OrderNo: order.Id.ToString("N")[..8].ToUpper(),
                CashierName: employee.Name,
                CreatedAt: new DateTimeOffset(order.CreatedAt, TimeSpan.Zero),
                Items: itemsDto,
                Subtotal: order.Subtotal,
                DiscountTotal: order.DiscountTotal,
                TaxTotal: order.TaxTotal,
                GrandTotal: order.GrandTotal,
                AmountPaid: totalPaid,
                ChangeAmount: changeAmount,
                ReceiptHeader: store.ReceiptHeader,
                ReceiptFooter: store.ReceiptFooter
            );
        }

        var paymentDtos = order.Payments.Select(p => new OrderPaymentDto(
            Id: p.Id,
            Method: p.Method.ToString(),
            Amount: p.Amount,
            ChangeAmount: p.ChangeAmount,
            TransactionRef: p.TransactionRef,
            Status: p.Status.ToString(),
            PaidAt: p.PaidAt.HasValue ? new DateTimeOffset(p.PaidAt.Value, TimeSpan.Zero) : null
        )).ToList();

        return new CheckoutDto(
            OrderId: order.Id,
            GrandTotal: order.GrandTotal,
            TotalPaid: totalPaid,
            ChangeAmount: changeAmount,
            Status: order.Status.ToString(),
            Payments: paymentDtos,
            ReceiptData: receiptData
        );
    }
}

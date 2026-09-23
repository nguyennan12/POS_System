using POS.Application.UseCases.Orders.DTOs;
using POS.Contracts.V1.Orders;

namespace POS.Api.Mappings;

public static class OrderMapping
{
    public static OrderDetailResponse ToResponse(this OrderDetailDto dto)
    {
        var items = dto.Items.Select(i => new OrderItemResponse(
            Id: i.Id,
            SkuId: i.SkuId,
            SkuCode: i.SkuCode,
            ProductName: i.ProductName,
            Qty: i.Qty,
            UnitPrice: i.UnitPrice,
            DiscountAmount: i.DiscountAmount,
            TaxAmount: i.TaxAmount,
            LineTotal: i.LineTotal
        )).ToList();

        var discounts = dto.Discounts.Select(d => new OrderDiscountResponse(
            Id: d.Id,
            PromotionId: d.PromotionId,
            VoucherId: d.VoucherId,
            DiscountAmount: d.DiscountAmount,
            Description: d.Description,
            AppliedAt: d.AppliedAt
        )).ToList();

        var payments = dto.Payments.Select(p => new OrderPaymentResponse(
            Id: p.Id,
            Method: p.Method,
            Amount: p.Amount,
            ChangeAmount: p.ChangeAmount,
            TransactionRef: p.TransactionRef,
            Status: p.Status,
            PaidAt: p.PaidAt
        )).ToList();

        return new OrderDetailResponse(
            Id: dto.Id,
            StoreId: dto.StoreId,
            ShiftId: dto.ShiftId,
            CustomerId: dto.CustomerId,
            CustomerName: dto.CustomerName,
            CustomerPhone: dto.CustomerPhone,
            Status: dto.Status,
            CurrencyCode: dto.CurrencyCode,
            Subtotal: dto.Subtotal,
            DiscountTotal: dto.DiscountTotal,
            TaxTotal: dto.TaxTotal,
            GrandTotal: dto.GrandTotal,
            Note: dto.Note,
            CreatedBy: dto.CreatedBy,
            CreatedAt: dto.CreatedAt,
            PaidAt: dto.PaidAt,
            Items: items,
            Discounts: discounts,
            Payments: payments
        );
    }

    public static OrderSummaryResponse ToResponse(this OrderSummaryDto dto)
    {
        return new OrderSummaryResponse(
            Id: dto.Id,
            StoreId: dto.StoreId,
            ShiftId: dto.ShiftId,
            CustomerId: dto.CustomerId,
            CustomerName: dto.CustomerName,
            CustomerPhone: dto.CustomerPhone,
            Status: dto.Status,
            CurrencyCode: dto.CurrencyCode,
            GrandTotal: dto.GrandTotal,
            TotalItems: dto.TotalItems,
            CreatedAt: dto.CreatedAt,
            PaidAt: dto.PaidAt
        );
    }
}

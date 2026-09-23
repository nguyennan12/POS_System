using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Employees;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Promotions.Services.Models;
using POS.Domain.Stores;

namespace POS.Domain.Orders;

public class Order : BaseEntity
{
    public Order() : base()
    {
    }

    private Order(
        Guid storeId,
        Guid shiftId,
        Guid createdBy,
        Guid? customerId = null,
        string currencyCode = "VND",
        string? note = null,
        Guid? id = null) : base(id)
    {
        StoreId = storeId;
        ShiftId = shiftId;
        CreatedBy = createdBy;
        CustomerId = customerId;
        CurrencyCode = currencyCode;
        Note = note;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public static Order CreateDraft(
        Guid storeId,
        Guid shiftId,
        Guid createdBy,
        Guid? customerId = null,
        string currencyCode = "VND",
        string? note = null,
        Guid? id = null)
    {
        return new Order(storeId, shiftId, createdBy, customerId, currencyCode, note, id);
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid ShiftId { get; private set; }
    public Shift Shift { get; private set; } = default!;

    public Guid? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public string CurrencyCode { get; private set; } = "VND";
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string? Note { get; private set; }
    public Guid? AppliedVoucherId { get; private set; }
    public string? AppliedVoucherCode { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }

    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public ICollection<OrderDiscount> Discounts { get; private set; } = new List<OrderDiscount>();

    public void SetCustomer(Guid? customerId)
    {
        CustomerId = customerId;
    }

    public void SetNote(string? note)
    {
        Note = note;
    }

    public void SetAppliedVoucher(Guid? voucherId, string? voucherCode)
    {
        AppliedVoucherId = voucherId;
        AppliedVoucherCode = voucherCode;
    }

    public void ClearAppliedVoucher()
    {
        AppliedVoucherId = null;
        AppliedVoucherCode = null;
    }

    public OrderItem AddOrUpdateItem(Sku sku, decimal qty)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể chỉnh sửa giỏ hàng khi đơn ở trạng thái Draft.");

        var existingItem = Items.FirstOrDefault(i => i.SkuId == sku.Id);
        if (existingItem != null)
        {
            existingItem.AddQty(qty);
            existingItem.Sku = sku;
            return existingItem;
        }

        var newItem = new OrderItem(Id, sku.Id, qty, sku.SellPrice)
        {
            Sku = sku
        };
        Items.Add(newItem);
        return newItem;
    }

    public void RemoveItem(Guid skuId)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể chỉnh sửa giỏ hàng khi đơn ở trạng thái Draft.");

        var item = Items.FirstOrDefault(i => i.SkuId == skuId);
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    public void ClearItems()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể chỉnh sửa giỏ hàng khi đơn ở trạng thái Draft.");

        Items.Clear();
        Discounts.Clear();
        Subtotal = 0;
        DiscountTotal = 0;
        TaxTotal = 0;
        GrandTotal = 0;
    }

    public void ApplyPromotionEvaluation(
        PromotionResult result,
        IDictionary<Guid, decimal> skuTaxRates)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể tính lại khuyến mãi khi đơn ở trạng thái Draft.");

        // 1. Cập nhật chiết khấu và thuế cho từng OrderItem
        var discountMap = result.ItemDiscounts.ToDictionary(d => d.SkuId, d => d.DiscountAmount);

        foreach (var item in Items)
        {
            decimal itemDiscount = discountMap.TryGetValue(item.SkuId, out var d) ? d : 0;
            decimal taxRate = skuTaxRates.TryGetValue(item.SkuId, out var tr) ? tr : 0;
            item.ApplyDiscountAndTax(itemDiscount, taxRate);
        }

        // 2. Làm mới danh sách OrderDiscount
        Discounts.Clear();
        foreach (var od in result.OrderDiscounts)
        {
            Discounts.Add(new OrderDiscount(
                orderId: Id,
                promotionId: od.PromotionId,
                voucherId: od.VoucherId,
                discountAmount: od.DiscountAmount,
                description: od.Description,
                appliedAt: od.AppliedAt.UtcDateTime
            ));
        }

        // 3. Tính lại tổng tiền
        Subtotal = Math.Round(Items.Sum(i => i.UnitPrice * i.Qty), 2);
        DiscountTotal = Math.Round(result.TotalDiscount, 2);
        TaxTotal = Math.Round(Items.Sum(i => i.TaxAmount), 2);
        GrandTotal = Math.Max(0, Math.Round(Subtotal - DiscountTotal + TaxTotal, 2));
    }
}

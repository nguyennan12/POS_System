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
    public string? CancelReason { get; private set; }

    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public ICollection<OrderDiscount> Discounts { get; private set; } = new List<OrderDiscount>();
    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

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

    public Result Confirm()
    {
        if (Status != OrderStatus.Draft)
            return new Error(ErrorType.Invalid, "ORDER.NOT_DRAFT",
                "Chỉ có thể xác nhận đơn ở trạng thái Draft.");
        if (!Items.Any())
            return new Error(ErrorType.Invalid, "ORDER.CART_EMPTY",
                "Giỏ hàng trống.");
        Status = OrderStatus.Confirmed;
        return Result.Success();
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

    public (decimal TotalApplied, decimal ChangeAmount, OrderStatus Status) ProcessPayments(
        IEnumerable<(PaymentMethod Method, decimal Amount, string? TransactionRef)> newPayments)
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Chỉ có thể thanh toán đơn ở trạng thái Confirmed.");
        if (!Items.Any())
            throw new InvalidOperationException("Giỏ hàng đang trống.");

        // Stage the whole batch before changing the aggregate.
        var staged = new List<Payment>();
        var remaining = GrandTotal - GetPaymentTotals().TotalApplied;
        if (remaining < 0)
            throw new InvalidOperationException("Tổng thanh toán vượt quá tổng đơn.");
        var references = Payments.Where(p => p.TransactionRef != null)
            .Select(p => (p.Method, p.TransactionRef)).ToHashSet();

        foreach (var (method, amount, transactionRef) in newPayments)
        {
            if (amount <= 0 || amount != Math.Round(amount, 2) || amount > 9999999999999999.99m)
                throw new ArgumentOutOfRangeException(nameof(newPayments), "Số tiền thanh toán phải lớn hơn 0.");
            if (!Enum.IsDefined(method))
                throw new ArgumentException("Phương thức thanh toán không hợp lệ.", nameof(newPayments));
            if (transactionRef != null && !references.Add((method, transactionRef)))
                throw new InvalidOperationException("Mã giao dịch đã được sử dụng.");
            if (method != PaymentMethod.Cash && amount > remaining)
                throw new InvalidOperationException("Thanh toán không dùng tiền mặt vượt số tiền còn lại.");

            var change = method == PaymentMethod.Cash ? Math.Max(0, amount - remaining) : 0;

            var payment = Payment.CreateSuccess(
                orderId: Id,
                method: method,
                amount: amount,
                transactionRef: transactionRef,
                changeAmount: change > 0 ? change : null);

            staged.Add(payment);
            remaining -= amount - change;
        }

        foreach (var payment in staged)
            Payments.Add(payment);

        AggregatePaymentStatus();
        var totals = GetPaymentTotals();
        return (totals.TotalApplied, totals.ChangeAmount, Status);
    }

    public (decimal TotalApplied, decimal ChangeAmount) GetPaymentTotals()
    {
        var successful = Payments.Where(p => p.Status == PaymentStatus.Success).ToList();
        var change = successful.Sum(p => p.ChangeAmount ?? 0);
        return (successful.Sum(p => p.Amount) - change, change);
    }

    public void AggregatePaymentStatus()
    {
        // Pending, Failed and Timeout never contribute to settlement.
        if (Status == OrderStatus.Confirmed && GetPaymentTotals().TotalApplied == GrandTotal)
        {
            Status = OrderStatus.Paid;
            PaidAt = DateTime.UtcNow;
        }
    }

    public void Cancel(string? reason = null)
    {
        if (Status == OrderStatus.Paid)
            throw new InvalidOperationException("Không thể hủy đơn hàng đã thanh toán.");
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Đơn hàng đã bị hủy trước đó.");

        Status = OrderStatus.Cancelled;
        CancelReason = reason;
    }
}

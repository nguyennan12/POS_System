using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Suppliers;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.StockIn;

public class StockInVoucher : BaseEntity
{
    public StockInVoucher() : base()
    {
    }

    private StockInVoucher(
        Guid storeId,
        Guid supplierId,
        Guid createdBy,
        string? note,
        Guid? id = null) : base(id)
    {
        StoreId = storeId;
        SupplierId = supplierId;
        CreatedBy = createdBy;
        Note = note;
        Status = StockInVoucherStatus.Draft;
        TotalAmount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public static StockInVoucher Create(
        Guid storeId,
        Guid supplierId,
        Guid createdBy,
        string? note = null)
    {
        return new StockInVoucher(storeId, supplierId, createdBy, note);
    }

    public Result AddItem(Guid skuId, decimal qty, decimal unitPrice,
        string? batchNo = null, DateOnly? expiryDate = null)
    {
        if (Status != StockInVoucherStatus.Draft)
            return new Error(ErrorType.Invalid, "StockIn.VoucherNotDraft",
                "Chỉ có thể thêm hàng hóa khi phiếu nhập đang ở trạng thái Nháp.");

        if (qty <= 0)
            return new Error(ErrorType.Validation, "StockIn.InvalidQty", "Số lượng phải lớn hơn 0.");
        if (unitPrice < 0)
            return new Error(ErrorType.Validation, "StockIn.InvalidUnitPrice", "Đơn giá không được âm.");

        var item = new StockInVoucherItem(Id, skuId, qty, unitPrice, batchNo, expiryDate);
        Items.Add(item);
        TotalAmount += item.TotalPrice;
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != StockInVoucherStatus.Draft)
            return new Error(ErrorType.Invalid, "StockIn.CannotComplete",
                "Chỉ có thể hoàn thành phiếu nhập đang ở trạng thái Nháp.");
        if (!Items.Any())
            return new Error(ErrorType.Invalid, "StockIn.NoItems",
                "Phiếu nhập phải có ít nhất một mặt hàng.");

        Status = StockInVoucherStatus.Completed;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == StockInVoucherStatus.Completed)
            return new Error(ErrorType.Invalid, "StockIn.CannotCancel",
                "Không thể hủy phiếu nhập đã hoàn thành.");
        if (Status == StockInVoucherStatus.Cancelled)
            return new Error(ErrorType.Invalid, "StockIn.AlreadyCancelled", "Phiếu nhập đã bị hủy trước đó.");

        Status = StockInVoucherStatus.Cancelled;
        return Result.Success();
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = default!;

    public decimal TotalAmount { get; private set; }
    public StockInVoucherStatus Status { get; private set; } = StockInVoucherStatus.Draft;
    public string? Note { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<StockInVoucherItem> Items { get; private set; } = new List<StockInVoucherItem>();
}

using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.StockTake;

public class StockTake : BaseEntity
{
    public StockTake() : base()
    {
    }

    private StockTake(
        Guid storeId,
        Guid createdBy,
        string? note,
        Guid? id = null) : base(id)
    {
        StoreId = storeId;
        CreatedBy = createdBy;
        Note = note;
        Status = StockTakeStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public static StockTake Create(
        Guid storeId,
        Guid createdBy,
        string? note = null)
    {
        return new StockTake(storeId, createdBy, note);
    }

    public Result UpdateItem(Guid skuId, decimal actualQty, decimal systemQty, string? note = null)
    {
        if (Status == StockTakeStatus.Approved)
            return new Error(ErrorType.Invalid, "StockTake.AlreadyApproved",
                "Không thể chỉnh sửa phiếu kiểm kê đã được duyệt.");

        if (actualQty < 0)
            return new Error(ErrorType.Validation, "StockTake.InvalidActualQty",
                "Số lượng thực tế không được âm.");

        var existingItem = Items.FirstOrDefault(i => i.SkuId == skuId);
        if (existingItem is not null)
        {
            existingItem.Update(actualQty, systemQty, note);
        }
        else
        {
            var item = new StockTakeItem(Id, skuId, systemQty, actualQty, note);
            Items.Add(item);
        }

        return Result.Success();
    }

    public Result Submit()
    {
        if (Status == StockTakeStatus.Approved)
            return new Error(ErrorType.Invalid, "StockTake.AlreadyApproved",
                "Phiếu kiểm kê đã được duyệt.");

        Status = StockTakeStatus.Pending;
        return Result.Success();
    }

    public Result Approve(Guid approvedBy)
    {
        if (Status == StockTakeStatus.Approved)
            return new Error(ErrorType.Invalid, "StockTake.AlreadyApproved",
                "Phiếu kiểm kê đã được duyệt trước đó.");

        if (!Items.Any())
            return new Error(ErrorType.Invalid, "StockTake.NoItems",
                "Phiếu kiểm kê phải có ít nhất một mặt hàng để duyệt.");

        Status = StockTakeStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public StockTakeStatus Status { get; private set; } = StockTakeStatus.Draft;
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public Guid? ApprovedBy { get; private set; }
    public Employee? ApprovedByEmployee { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; private set; }

    public ICollection<StockTakeItem> Items { get; private set; } = new List<StockTakeItem>();
}

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public partial class StockInVoucherItemModel : ObservableObject
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    
    [ObservableProperty]
    private string _status = "Draft";

    public string? Note { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string TotalAmountFormatted => TotalAmount.ToString("N0") + " đ";
    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public string ShortId => $"PN-{Id.ToString("N")[..8].ToUpperInvariant()}";

    public bool CanComplete => Status == "Draft";
    public bool CanCancel => Status == "Draft";

    public string StatusText => Status switch
    {
        "Draft" => "Chờ duyệt",
        "Completed" => "Đã nhập kho",
        "Cancelled" => "Đã hủy",
        _ => Status
    };

    public string StatusBadgeBg => Status switch
    {
        "Draft" => "#FEF3C7",
        "Completed" => "#ECFDF5",
        "Cancelled" => "#FEF2F2",
        _ => "#F1F5F9"
    };

    public string StatusBadgeFg => Status switch
    {
        "Draft" => "#D97706",
        "Completed" => "#10B981",
        "Cancelled" => "#DC2626",
        _ => "#64748B"
    };

    public static StockInVoucherItemModel FromDto(StockInVoucherSummaryResponse dto)
    {
        return new StockInVoucherItemModel
        {
            Id = dto.Id,
            StoreId = dto.StoreId,
            SupplierId = dto.SupplierId,
            SupplierName = dto.SupplierName,
            TotalAmount = dto.TotalAmount,
            Status = dto.Status,
            Note = dto.Note,
            CreatedBy = dto.CreatedBy,
            CreatedAt = dto.CreatedAt
        };
    }
}

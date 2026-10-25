using System;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public partial class StockTakeLineItemModel : ObservableObject
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal SystemQty { get; set; }

    [ObservableProperty]
    private decimal _actualQty;

    [ObservableProperty]
    private string? _note;

    public decimal DiffQty => ActualQty - SystemQty;

    public string DiffStatus => DiffQty switch
    {
        0 => "Khớp (0)",
        > 0 => $"+{DiffQty:0.##} (Thừa)",
        _ => $"{DiffQty:0.##} (Thiếu)"
    };

    public string DiffBadgeBg => DiffQty switch
    {
        0 => "#ECFDF5",
        > 0 => "#EFF6FF",
        _ => "#FEF2F2"
    };

    public string DiffBadgeFg => DiffQty switch
    {
        0 => "#10B981",
        > 0 => "#2563EB",
        _ => "#DC2626"
    };

    partial void OnActualQtyChanged(decimal value)
    {
        OnPropertyChanged(nameof(DiffQty));
        OnPropertyChanged(nameof(DiffStatus));
        OnPropertyChanged(nameof(DiffBadgeBg));
        OnPropertyChanged(nameof(DiffBadgeFg));
    }

    public static StockTakeLineItemModel FromDto(StockTakeItemResponse dto)
    {
        return new StockTakeLineItemModel
        {
            Id = dto.Id,
            SkuId = dto.SkuId,
            SkuCode = dto.SkuCode,
            Barcode = dto.Barcode,
            ProductName = dto.ProductName,
            SystemQty = dto.SystemQty,
            ActualQty = dto.ActualQty,
            Note = dto.Note
        };
    }
}

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public partial class StockEntryItemModel : ObservableObject
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    
    [ObservableProperty]
    private decimal _qtyOnHand;

    [ObservableProperty]
    private decimal _minStock;

    public DateTimeOffset LastUpdated { get; set; }

    public bool IsOutOfStock => QtyOnHand <= 0;
    public bool IsLowStock => QtyOnHand > 0 && QtyOnHand <= MinStock;
    public bool IsHealthy => QtyOnHand > MinStock;

    public string StatusText => IsOutOfStock 
        ? "Hết hàng" 
        : IsLowStock 
            ? "Sắp hết" 
            : "Đủ tồn kho";

    public string StatusBadgeBg => IsOutOfStock 
        ? "#FEF2F2" 
        : IsLowStock 
            ? "#FFF7ED" 
            : "#ECFDF5";

    public string StatusBadgeFg => IsOutOfStock 
        ? "#DC2626" 
        : IsLowStock 
            ? "#ED7A1C" 
            : "#10B981";

    public string LastUpdatedFormatted => LastUpdated.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static StockEntryItemModel FromDto(StockEntryResponse dto)
    {
        return new StockEntryItemModel
        {
            Id = dto.Id,
            SkuId = dto.SkuId,
            SkuCode = dto.SkuCode,
            Barcode = dto.Barcode,
            ProductName = dto.ProductName,
            QtyOnHand = dto.QtyOnHand,
            MinStock = dto.MinStock,
            LastUpdated = dto.LastUpdated
        };
    }
}

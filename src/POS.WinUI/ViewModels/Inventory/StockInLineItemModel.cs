using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public partial class StockInLineItemModel : ObservableObject
{
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    [ObservableProperty]
    private decimal _qty = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private string? _batchNo;

    [ObservableProperty]
    private DateTime? _expiryDate;

    public decimal TotalPrice => Qty * UnitPrice;
    public string TotalPriceFormatted => TotalPrice.ToString("N0") + " đ";
    public string UnitPriceFormatted => UnitPrice.ToString("N0") + " đ";

    [RelayCommand]
    public void IncreaseQty()
    {
        Qty += 1;
    }

    [RelayCommand]
    public void DecreaseQty()
    {
        if (Qty > 1)
        {
            Qty -= 1;
        }
    }

    partial void OnQtyChanged(decimal value)
    {
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(TotalPriceFormatted));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(TotalPriceFormatted));
    }

    public static StockInLineItemModel FromDto(StockInVoucherItemResponse dto)
    {
        return new StockInLineItemModel
        {
            SkuId = dto.SkuId,
            SkuCode = dto.SkuCode,
            ProductName = dto.ProductName,
            Qty = dto.Qty,
            UnitPrice = dto.UnitPrice,
            BatchNo = dto.BatchNo,
            ExpiryDate = dto.ExpiryDate.HasValue 
                ? new DateTime(dto.ExpiryDate.Value.Year, dto.ExpiryDate.Value.Month, dto.ExpiryDate.Value.Day) 
                : null
        };
    }
}

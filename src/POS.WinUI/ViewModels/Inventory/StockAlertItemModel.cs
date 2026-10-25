using System;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public partial class StockAlertItemModel : ObservableObject
{
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal QtyOnHand { get; set; }
    public decimal MinStock { get; set; }
    public string AlertType { get; set; } = string.Empty;

    public bool IsNearExpiry => AlertType == "NearExpiry" || AlertType == "Expired";
    public bool IsLowStock => AlertType == "LowStock" || AlertType == "OutOfStock";

    public string AlertTypeText => AlertType switch
    {
        "NearExpiry" => "Cận hạn sử dụng (≤30 ngày)",
        "Expired" => "Đã quá hạn sử dụng",
        "OutOfStock" => "Đã hết hàng tồn kho (0)",
        _ => "Dưới định mức tồn an toàn"
    };

    public string BadgeBg => AlertType switch
    {
        "NearExpiry" => "#FAF5FF",
        "Expired" => "#FEF2F2",
        "OutOfStock" => "#FEF2F2",
        _ => "#FFF7ED"
    };

    public string BadgeFg => AlertType switch
    {
        "NearExpiry" => "#7C3AED",
        "Expired" => "#DC2626",
        "OutOfStock" => "#DC2626",
        _ => "#EA580C"
    };

    public static StockAlertItemModel FromDto(StockAlertResponse dto)
    {
        return new StockAlertItemModel
        {
            SkuId = dto.SkuId,
            SkuCode = dto.SkuCode,
            Barcode = dto.Barcode,
            ProductName = dto.ProductName,
            QtyOnHand = dto.QtyOnHand,
            MinStock = dto.MinStock,
            AlertType = dto.AlertType
        };
    }
}

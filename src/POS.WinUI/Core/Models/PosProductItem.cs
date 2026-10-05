using Wpf.Ui.Controls;

namespace POS.WinUI.Core.Models;

/// <summary>
/// Model hiển thị sản phẩm trên lưới bán hàng quầy thu ngân
/// </summary>
public class PosProductItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Unit { get; set; } = "Cái";
    public string? Specification { get; set; }
    public string DisplaySpecification => !string.IsNullOrWhiteSpace(Specification) ? Specification : Unit;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string BackgroundTint { get; set; } = "#F8FAFC";
    public string IconForeground { get; set; } = "#0284C7";
    public SymbolRegular IconSymbol { get; set; } = SymbolRegular.Box24;
}

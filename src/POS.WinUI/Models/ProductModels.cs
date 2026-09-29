using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.Models;

/// <summary>
/// Model dùng để bind vào DataGrid SKU trong ProductEditView.
/// Là bản ghi có thể quan sát, hỗ trợ thêm/xóa động.
/// </summary>
public partial class SkuEditItem : ObservableObject
{
    [ObservableProperty] private string _skuCode = string.Empty;
    [ObservableProperty] private string _barcode = string.Empty;
    [ObservableProperty] private decimal _costPrice;
    [ObservableProperty] private decimal _sellPrice;
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private bool _isActive = true;

    // Attributes hiển thị là text JSON đơn giản (key:value) để user nhập
    [ObservableProperty] private string _color = string.Empty;
    [ObservableProperty] private string _size  = string.Empty;

    /// <summary>Dùng khi chỉnh sửa — giữ ID SKU từ server.</summary>
    public Guid? Id { get; set; }

    /// <summary>Margin lợi nhuận tính từ giá bán / giá vốn.</summary>
    public decimal MarginPercent =>
        CostPrice > 0 ? Math.Round((SellPrice - CostPrice) / CostPrice * 100, 1) : 0;
}

/// <summary>Item danh sách sản phẩm hiển thị trong DataGrid.</summary>
public class ProductListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string? Brand { get; init; }
    public string BaseUnit { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int SkuCount { get; init; }
    public string StatusBadge => Status == "Active" ? "Đang bán" : "Ngừng bán";
}

/// <summary>Item danh mục cho ComboBox lọc.</summary>
public class CategoryItem
{
    public Guid? Id { get; init; }          // null = "Tất cả danh mục"
    public string Name { get; init; } = string.Empty;
}

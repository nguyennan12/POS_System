using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.ViewModels.Products;

public sealed partial class ProductItemModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Guid _categoryId;

    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private string? _brand;

    [ObservableProperty]
    private string _baseUnit = "Cái";

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private string _status = "Active";

    [ObservableProperty]
    private int _skuCount;

    [ObservableProperty]
    private DateTimeOffset _createdAt;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isLoadingSkus;

    public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);

    public string StatusBadgeText => IsActive ? "Đang bán" : "Ngưng bán";
    public string StatusBadgeBackground => IsActive ? "#ECFDF5" : "#F1F5F9";
    public string StatusBadgeForeground => IsActive ? "#059669" : "#64748B";

    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public ObservableCollection<ProductSkuItemModel> Skus { get; } = new();

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(StatusBadgeText));
        OnPropertyChanged(nameof(StatusBadgeBackground));
        OnPropertyChanged(nameof(StatusBadgeForeground));
    }
}

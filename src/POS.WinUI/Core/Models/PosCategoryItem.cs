using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace POS.WinUI.Core.Models;

public partial class PosCategoryItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public string Color { get; set; } = "#EA580C";
    public SymbolRegular IconSymbol { get; set; } = SymbolRegular.Apps24;
    public string IconColor { get; set; } = "#EA580C";

    public System.Collections.ObjectModel.ObservableCollection<PosCategoryItem> SubCategories { get; } = new();
    public bool HasSubCategories => SubCategories.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentBackground))]
    [NotifyPropertyChangedFor(nameof(CurrentBorderBrush))]
    [NotifyPropertyChangedFor(nameof(CurrentForeground))]
    [NotifyPropertyChangedFor(nameof(CurrentFontWeight))]
    private bool _isSelected;

    public string CurrentBackground => IsSelected ? "#FFF7ED" : "#F8FAFC";
    public string CurrentBorderBrush => IsSelected ? "#EA580C" : "#E2E8F0";
    public string CurrentForeground => IsSelected ? "#EA580C" : "#334155";
    public string CurrentFontWeight => IsSelected ? "Bold" : "SemiBold";
}

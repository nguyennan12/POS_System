using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Management;

/// <summary>
/// Đại diện cho một mục tab điều hướng trên Sidebar của màn hình quản lý.
/// </summary>
public partial class NavigationItemViewModel : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public SymbolRegular Icon { get; init; }

    [ObservableProperty]
    private bool _isSelected;
}

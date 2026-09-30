using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Management;

public partial class NavigationItemViewModel : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public SymbolRegular Icon { get; init; }

    public string[]? AllowedRoles { get; init; }
    public string[]? RequiredPermissions { get; init; }
    public int MinRoleLevel { get; init; } = 0;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;
}

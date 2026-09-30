using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Management;

/// <summary>
/// Lớp cơ sở cho tất cả các ViewModel phân hệ quản lý (Dashboard, Products, Orders, ...)
/// </summary>
public abstract partial class ManagementTabViewModelBase : ObservableObject
{
    public abstract string TabId { get; }
    public abstract string Title { get; }
    public abstract string Subtitle { get; }
    public abstract SymbolRegular Icon { get; }

    [ObservableProperty]
    private bool _isLoading;
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.Services;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Management;

public abstract partial class ManagementTabViewModelBase : ObservableObject, IDisposable
{
    protected readonly SessionService SessionService = default!;

    public abstract string TabId { get; }
    public abstract string Title { get; }
    public abstract string Subtitle { get; }
    public abstract SymbolRegular Icon { get; }

    [ObservableProperty]
    private bool _isLoading;

    public bool IsOwner => SessionService?.IsOwner ?? false;
    public bool IsStoreManager => SessionService?.IsStoreManager ?? false;
    public bool IsCashier => SessionService?.IsCashier ?? false;
    public int RoleLevel => SessionService?.RoleLevel ?? 0;
    public string? CurrentStoreId => SessionService?.EffectiveStoreId;
    public string? CurrentStoreName => SessionService?.StoreName;

    protected ManagementTabViewModelBase(SessionService? sessionService = null)
    {
        SessionService = sessionService 
                      ?? SessionService.Current 
                      ?? App.Services?.GetService<SessionService>()!;

        if (SessionService != null)
        {
            SessionService.StoreChanged += OnStoreChangedInternal;
        }
    }

    private void OnStoreChangedInternal(string storeId, string? storeName)
    {
        OnPropertyChanged(nameof(CurrentStoreId));
        OnPropertyChanged(nameof(CurrentStoreName));
        OnStoreChanged(storeId, storeName);
    }

    protected virtual void OnStoreChanged(string storeId, string? storeName)
    {
    }

    protected bool HasPermission(string permissionCode) => SessionService?.HasPermission(permissionCode) ?? false;

    protected bool IsInRole(string roles) => SessionService?.IsInRole(roles) ?? false;

    public virtual void Dispose()
    {
        if (SessionService != null)
        {
            SessionService.StoreChanged -= OnStoreChangedInternal;
        }
        GC.SuppressFinalize(this);
    }
}

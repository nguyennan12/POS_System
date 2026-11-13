using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Customers;

public sealed partial class CustomersViewModel : ManagementTabViewModelBase
{
    private readonly CustomerApiClient _api;
    private readonly IConfirmDialogService _dialogs;
    private bool _initialized;

    public override string TabId => "Customers";
    public override string Title => Loc["customers.title"];
    public override string Subtitle => Loc["customers.subtitle"];
    public override SymbolRegular Icon => SymbolRegular.People24;

    [ObservableProperty] private bool _isCustomerListTab = true;
    [ObservableProperty] private bool _isTiersTab;
    [ObservableProperty] private bool _isHistoryTab;

    [ObservableProperty] private string? _listError;

    // KPI Summary
    [ObservableProperty] private int _totalCustomersCount;
    [ObservableProperty] private int _newCustomersThisMonth;
    [ObservableProperty] private decimal _totalLoyaltyPoints;
    [ObservableProperty] private string _activeCustomersRate = "100%";

    // Modal Dialog states
    [ObservableProperty] private CustomerFormDialogViewModel? _customerFormDialog;
    [ObservableProperty] private PointAdjustDialogViewModel? _pointAdjustDialog;

    public bool IsCustomerModalOpen => CustomerFormDialog != null;
    public bool IsPointModalOpen => PointAdjustDialog != null;

    partial void OnCustomerFormDialogChanged(CustomerFormDialogViewModel? value)
        => OnPropertyChanged(nameof(IsCustomerModalOpen));

    partial void OnPointAdjustDialogChanged(PointAdjustDialogViewModel? value)
        => OnPropertyChanged(nameof(IsPointModalOpen));

    partial void OnIsCustomerListTabChanged(bool value)
    {
        if (value)
        {
            _isTiersTab = false;
            _isHistoryTab = false;
            OnPropertyChanged(nameof(IsTiersTab));
            OnPropertyChanged(nameof(IsHistoryTab));
            _ = LoadCustomersAsync();
        }
    }

    partial void OnIsTiersTabChanged(bool value)
    {
        if (value)
        {
            _isCustomerListTab = false;
            _isHistoryTab = false;
            OnPropertyChanged(nameof(IsCustomerListTab));
            OnPropertyChanged(nameof(IsHistoryTab));
            _ = LoadMemberTiersAsync();
        }
    }

    partial void OnIsHistoryTabChanged(bool value)
    {
        if (value)
        {
            _isCustomerListTab = false;
            _isTiersTab = false;
            OnPropertyChanged(nameof(IsCustomerListTab));
            OnPropertyChanged(nameof(IsTiersTab));
            _ = LoadPointTransactionsAsync();
        }
    }

    public CustomersViewModel(CustomerApiClient api, SessionService session, IConfirmDialogService dialogs)
        : base(session)
    {
        _api = api;
        _dialogs = dialogs;
        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        await Task.WhenAll(
            LoadCustomersAsync(),
            LoadMemberTiersAsync()
        );
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsCustomerListTab) await LoadCustomersAsync();
        else if (IsTiersTab) await LoadMemberTiersAsync();
        else if (IsHistoryTab) await LoadPointTransactionsAsync();
    }

    [RelayCommand]
    private void CloseModal()
    {
        CustomerFormDialog = null;
        PointAdjustDialog = null;
    }
}

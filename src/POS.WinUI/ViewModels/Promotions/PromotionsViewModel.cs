using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Promotions;

public sealed partial class PromotionsViewModel : ManagementTabViewModelBase
{
    private readonly PromotionApiClient _api;
    private readonly IConfirmDialogService _dialogs;
    private bool _initialized;

    public override string TabId => "Promotions";
    public override string Title => Loc["promotions.title"];
    public override string Subtitle => Loc["promotions.subtitle"];
    public override SymbolRegular Icon => SymbolRegular.TicketDiagonal24;

    [ObservableProperty] private bool _isPromotionsTab = true;
    [ObservableProperty] private bool _isVouchersTab;

    [ObservableProperty] private string? _listError;

    // KPI Metrics
    [ObservableProperty] private int _activePromotionsCount;
    [ObservableProperty] private int _activeVouchersCount;
    [ObservableProperty] private int _totalVouchersUsed;
    [ObservableProperty] private decimal _promotionRevenueEstimate;

    // Modals
    [ObservableProperty] private PromotionFormDialogViewModel? _promotionFormDialog;
    [ObservableProperty] private VoucherCreateDialogViewModel? _voucherCreateDialog;

    public bool IsPromotionModalOpen => PromotionFormDialog != null;
    public bool IsVoucherModalOpen => VoucherCreateDialog != null;

    partial void OnPromotionFormDialogChanged(PromotionFormDialogViewModel? value)
        => OnPropertyChanged(nameof(IsPromotionModalOpen));

    partial void OnVoucherCreateDialogChanged(VoucherCreateDialogViewModel? value)
        => OnPropertyChanged(nameof(IsVoucherModalOpen));

    partial void OnIsPromotionsTabChanged(bool value)
    {
        if (value)
        {
            _isVouchersTab = false;
            OnPropertyChanged(nameof(IsVouchersTab));
            _ = LoadPromotionsAsync();
        }
    }

    partial void OnIsVouchersTabChanged(bool value)
    {
        if (value)
        {
            _isPromotionsTab = false;
            OnPropertyChanged(nameof(IsPromotionsTab));
            _ = LoadVouchersAsync();
        }
    }

    public PromotionsViewModel(PromotionApiClient api, SessionService session, IConfirmDialogService dialogs)
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

        await LoadPromotionsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsPromotionsTab) await LoadPromotionsAsync();
        else if (IsVouchersTab) await LoadVouchersAsync();
    }

    [RelayCommand]
    private void CloseModal()
    {
        PromotionFormDialog = null;
        VoucherCreateDialog = null;
    }
}

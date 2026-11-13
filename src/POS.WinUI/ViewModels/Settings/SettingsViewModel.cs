using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Settings;

/// <summary>
/// ViewModel quản lý Cài đặt hệ thống: Thông tin cửa hàng, Máy in hóa đơn và Đa ngôn ngữ.
/// </summary>
public sealed partial class SettingsViewModel : ManagementTabViewModelBase
{
    private readonly StoreApiClient _storeApi;
    private readonly ConfigApiClient _configApi;
    private readonly LocalizationService _localization;

    public override string TabId => "Settings";
    public override string Title => Loc["settings.title"];
    public override string Subtitle => Loc["settings.subtitle"];
    public override SymbolRegular Icon => SymbolRegular.Settings24;

    // ── Phân tab chức năng ─────────────────────────────────────────
    [ObservableProperty]
    private bool _isStoreTab = true;

    [ObservableProperty]
    private bool _isPrinterTab;

    [ObservableProperty]
    private bool _isSaving;

    // ── Ngôn ngữ hệ thống ──────────────────────────────────────────
    [ObservableProperty]
    private string _selectedLanguage = "vi";

    public ObservableCollection<string> SupportedLanguages { get; } = ["vi", "en"];

    public SettingsViewModel(
        StoreApiClient storeApi,
        ConfigApiClient configApi,
        LocalizationService localization,
        SessionService sessionService) : base(sessionService)
    {
        _storeApi = storeApi;
        _configApi = configApi;
        _localization = localization;
        _selectedLanguage = localization.LanguageCode;

        InitializeStoreSettings();
        InitializePrinterSettings();

        // Tải dữ liệu ban đầu
        _ = LoadSettingsAsync();
    }

    [RelayCommand]
    private void SelectStoreTab()
    {
        IsStoreTab = true;
        IsPrinterTab = false;
    }

    [RelayCommand]
    private void SelectPrinterTab()
    {
        IsStoreTab = false;
        IsPrinterTab = true;
    }

    [RelayCommand]
    public async Task SwitchLanguageAsync(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return;
        SelectedLanguage = lang;

        try
        {
            var res = await _configApi.GetI18nDictionaryAsync(lang);
            if (res?.Success == true && res.Data?.Translations is { } trans)
            {
                LocalizationService.Instance.Apply(lang, trans);
            }
            else
            {
                LocalizationService.Instance.Apply(lang, new Dictionary<string, string>());
            }

            var langName = lang == "vi" ? "Tiếng Việt" : "English";
            ShowSuccess(string.Format(Loc["settings.lang_switched"], langName));
        }
        catch
        {
            LocalizationService.Instance.Apply(lang, new Dictionary<string, string>());
            ShowInfo($"Đã chuyển ngôn ngữ sang {lang}.");
        }
    }

    [RelayCommand]
    private async Task RefreshSettingsAsync()
    {
        await LoadSettingsAsync();
        ShowInfo("Đã làm mới thông tin cài đặt.");
    }

    protected override void OnStoreChanged(string newStoreId, string? storeName)
    {
        base.OnStoreChanged(newStoreId, storeName);
        _ = LoadSettingsAsync();
    }
}

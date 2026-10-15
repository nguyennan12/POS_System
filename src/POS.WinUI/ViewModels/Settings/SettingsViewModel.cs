using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Config;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Settings;

public sealed partial class SettingsViewModel : ManagementTabViewModelBase
{
    private readonly ConfigApiClient _configApi;
    private readonly LocalizationService _localization;

    public override string TabId => "Settings";
    public override string Title => "Cài đặt hệ thống";
    public override string Subtitle => "Thiết lập cấu hình cửa hàng, máy in hóa đơn và tích hợp phần cứng";
    public override SymbolRegular Icon => SymbolRegular.Settings24;

    // ── Ngôn ngữ ─────────────────────────────────────────────────
    [ObservableProperty]
    private string _selectedLanguage = "vi";

    public ObservableCollection<string> SupportedLanguages { get; } =
        new() { "vi", "en" };

    // ── Nhãn giao diện động ───────────────────────────────────────
    [ObservableProperty]
    private string _settingsTitle = "Cài đặt hệ thống";

    [ObservableProperty]
    private string _settingsSubtitle = "Thiết lập cấu hình cửa hàng, máy in hóa đơn và tích hợp phần cứng";

    [ObservableProperty]
    private string _languageSectionTitle = "Ngôn ngữ giao diện";

    [ObservableProperty]
    private string _languageSectionDesc = "Chọn ngôn ngữ và nhấn Áp dụng. Toàn bộ nhãn trên WinUI thay đổi tức thì – không cần build lại app.";

    [ObservableProperty]
    private string _applyLanguageButtonText = "Áp dụng ngôn ngữ";

    [ObservableProperty]
    private string _storeConfigTitle = "Cấu hình cửa hàng";

    [ObservableProperty]
    private string _storeConfigDesc = "Tải cấu hình SystemConfig của cửa hàng hiện tại từ server.";

    [ObservableProperty]
    private string _loadConfigButtonText = "Tải cấu hình";

    // ── Thông báo trạng thái ──────────────────────────────────────
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public SettingsViewModel(ConfigApiClient configApi, LocalizationService localization,
        SessionService sessionService) : base(sessionService)
    {
        _configApi = configApi;
        _localization = localization;
        _selectedLanguage = localization.LanguageCode;
        _localization.PropertyChanged += (_, _) => UpdateLocalizedTexts();
        UpdateLocalizedTexts();
    }

    private void UpdateLocalizedTexts()
    {
        SettingsTitle = _localization["settings.title"];
        SettingsSubtitle = _localization["settings.subtitle"];
        LanguageSectionTitle = _localization["settings.language"];
        LanguageSectionDesc = _localization["settings.language_desc"];
        ApplyLanguageButtonText = _localization["settings.apply_language"];
        StoreConfigTitle = _localization["settings.store_config"];
        StoreConfigDesc = _localization["settings.store_config_desc"];
        LoadConfigButtonText = _localization["settings.load_config"];
    }

    /// <summary>
    /// Tải từ điển ngôn ngữ mới từ API và áp dụng ngay lập tức lên toàn UI.
    /// </summary>
    [RelayCommand]
    private async Task SwitchLanguageAsync(CancellationToken ct)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Đang tải ngôn ngữ...";
        try
        {
            var response = await _configApi.GetI18nDictionaryAsync(SelectedLanguage, ct);
            if (response?.Data is { } dict)
            {
                _localization.Apply(dict.LanguageCode, dict.Translations);
                StatusMessage = string.Format(_localization["settings.lang_switched"], dict.LanguageCode);
            }
            else
            {
                StatusMessage = "Không thể tải từ điển ngôn ngữ.";
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Tải cấu hình cửa hàng hiện tại từ API.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Đang tải cấu hình...";
        try
        {
            Guid? storeId = Guid.TryParse(CurrentStoreId, out var g) ? g : null;
            var response = await _configApi.GetStoreConfigAsync(storeId, ct);
            if (response?.Data is not null)
                StatusMessage = $"Cấu hình đã tải – {response.Data.Configs.Count} mục.";
            else
                StatusMessage = "Không thể tải cấu hình.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

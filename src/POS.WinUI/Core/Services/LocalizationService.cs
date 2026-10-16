using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace POS.WinUI.Core.Services;

/// <summary>
/// Dịch vụ quản lý ngôn ngữ giao diện. Khi LanguageCode thay đổi,
/// toàn bộ UI binding lắng nghe sẽ cập nhật tức thì – không cần build lại app.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private string _languageCode = "vi";
    private static readonly Dictionary<string, string> VietnameseDefaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nav.workspace"] = "DANH MỤC QUẢN LÝ",
        ["nav.dashboard"] = "Tổng quan",
        ["nav.orders"] = "Đơn hàng & Hóa đơn",
        ["nav.products"] = "Sản phẩm & Danh mục",
        ["nav.inventory"] = "Quản lý Kho & Nhập hàng",
        ["nav.customers"] = "Khách hàng & Hội viên",
        ["nav.promotions"] = "Khuyến mãi & Voucher",
        ["nav.employees"] = "Nhân viên & Phân quyền",
        ["nav.reports"] = "Báo cáo & Thống kê",
        ["nav.settings"] = "Cài đặt hệ thống",
        ["header.pos_screen"] = "Bán hàng POS (F1)",
        ["header.loading_stores"] = "Đang tải chi nhánh...",
        ["header.notifications"] = "Thông báo hệ thống",
        ["settings.title"] = "Cài đặt hệ thống",
        ["settings.subtitle"] = "Thiết lập cấu hình cửa hàng, máy in hóa đơn và tích hợp phần cứng",
        ["settings.language"] = "Ngôn ngữ giao diện",
        ["settings.language_desc"] = "Chọn ngôn ngữ và nhấn Áp dụng. Toàn bộ nhãn trên WinUI thay đổi tức thì – không cần build lại app.",
        ["settings.apply_language"] = "Áp dụng ngôn ngữ",
        ["settings.store_config"] = "Cấu hình cửa hàng",
        ["settings.store_config_desc"] = "Tải cấu hình SystemConfig của cửa hàng hiện tại từ server.",
        ["settings.load_config"] = "Tải cấu hình",
        ["settings.lang_switched"] = "Đã chuyển ngôn ngữ sang '{0}'.",
        ["breadcrumb.home"] = "Trang chủ",
    };

    private IReadOnlyDictionary<string, string> _dictionary = VietnameseDefaults;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static LocalizationService Instance { get; } = new();

    private LocalizationService() { }

    /// <summary>Mã ngôn ngữ hiện tại (vi / en / ...).</summary>
    public string LanguageCode
    {
        get => _languageCode;
        private set
        {
            if (_languageCode == value) return;
            _languageCode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Tải từ điển mới và kích hoạt cập nhật tất cả binding.</summary>
    public void Apply(string languageCode, IReadOnlyDictionary<string, string> translations)
    {
        _dictionary = translations;
        LanguageCode = languageCode;
        // Notify indexer thay đổi để WPF tự refresh toàn bộ {Binding [Key]}
        OnPropertyChanged("Item[]");
    }

    /// <summary>Lấy giá trị dịch; nếu không có trong từ điển thì tìm trong fallback tiếng Việt, cuối cùng trả về key.</summary>
    public string this[string key]
    {
        get
        {
            if (_dictionary.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val) && !val.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return val;
            }

            if (VietnameseDefaults.TryGetValue(key, out var defaultVal))
            {
                return defaultVal;
            }

            return key;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace POS.WinUI.Core.Services;

/// <summary>
/// Dịch vụ quản lý ngôn ngữ giao diện (i18n). Khi LanguageCode thay đổi,
/// toàn bộ UI binding lắng nghe sẽ cập nhật tức thì – không cần tắt hay build lại app.
/// </summary>
public sealed partial class LocalizationService : INotifyPropertyChanged
{
    private string _languageCode = "vi";
    private IReadOnlyDictionary<string, string>? _customDictionary;

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

    /// <summary>Tải từ điển mới và kích hoạt cập nhật tất cả binding trên toàn UI.</summary>
    public void Apply(string languageCode, IReadOnlyDictionary<string, string> translations)
    {
        _customDictionary = translations;
        LanguageCode = languageCode;
        // Notify indexer thay đổi để WPF tự refresh toàn bộ {Binding [Key]}
        OnPropertyChanged("Item[]");
    }

    /// <summary>Lấy giá trị dịch tương ứng với key.</summary>
    public string this[string key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            if (_customDictionary != null && _customDictionary.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val) && !val.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return val;
            }

            if (LanguageCode.Equals("en", StringComparison.OrdinalIgnoreCase) && EnglishDefaults.TryGetValue(key, out var enVal))
            {
                return enVal;
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

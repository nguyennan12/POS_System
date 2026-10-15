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
    private IReadOnlyDictionary<string, string> _dictionary = new Dictionary<string, string>();

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

    /// <summary>Lấy giá trị dịch; trả về key nếu không tìm thấy.</summary>
    public string this[string key] =>
        _dictionary.TryGetValue(key, out var val) ? val : key;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

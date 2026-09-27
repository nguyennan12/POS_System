using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Auth.Components;

/// <summary>
/// Input mật khẩu tự quản lý trạng thái show/hide nội bộ trong màn hình Auth.
/// </summary>
public partial class PasswordInputField : UserControl
{
    private bool _isUpdating;
    private bool _isShown;

    // ── Password DP ───────────────────────────────────────────────
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(
            nameof(Password),
            typeof(string),
            typeof(PasswordInputField),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordDpChanged));

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    // ── PlaceholderText DP ────────────────────────────────────────
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(PasswordInputField),
            new PropertyMetadata("Nhập mật khẩu"));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public PasswordInputField()
    {
        InitializeComponent();
    }

    // ── Sync PasswordBox ← DP (khi ViewModel set Password từ ngoài vào) ──
    private static void OnPasswordDpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (PasswordInputField)d;
        if (ctrl._isUpdating) return;

        var newVal = (string)e.NewValue ?? string.Empty;

        ctrl._isUpdating = true;
        if (ctrl.PwdBox.Password != newVal)
            ctrl.PwdBox.Password = newVal;
        ctrl._isUpdating = false;

        ctrl.UpdatePlaceholder(newVal);
    }

    // ── Sync DP ← PasswordBox (khi user gõ) ──────────────────────
    private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;

        _isUpdating = true;
        Password = PwdBox.Password;
        _isUpdating = false;

        UpdatePlaceholder(PwdBox.Password);
    }

    // ── Toggle show/hide nội bộ ───────────────────────────────────
    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        _isShown = !_isShown;

        if (_isShown)
        {
            TxtPassword.Text = PwdBox.Password;
            PwdBox.Visibility    = Visibility.Collapsed;
            TxtPassword.Visibility = Visibility.Visible;
            EyeIcon.Symbol = SymbolRegular.EyeOff24;
        }
        else
        {
            PwdBox.Password      = TxtPassword.Text;
            TxtPassword.Visibility = Visibility.Collapsed;
            PwdBox.Visibility    = Visibility.Visible;
            EyeIcon.Symbol = SymbolRegular.Eye24;
        }
    }

    private void UpdatePlaceholder(string value)
    {
        Placeholder.Visibility = string.IsNullOrEmpty(value)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}

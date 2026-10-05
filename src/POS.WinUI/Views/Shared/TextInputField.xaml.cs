using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Shared;

/// <summary>
/// Ô nhập văn bản dùng chung (Shared TextInputField) cho toàn bộ ứng dụng:
/// - Tự do tùy biến chiều cao (Height), bo góc (CornerRadius), font size, icon.
/// - Placeholder căn chỉnh hoàn hảo cùng vị trí với text nhập.
/// - Tích hợp nút Clear nhanh (ShowClearButton) và phím tắt Enter (EnterCommand).
/// - Khắc phục hoàn toàn lỗi gạch chân viền xanh của WPF-UI, viền chuyển màu cam thương hiệu khi Focus.
/// </summary>
public partial class TextInputField : UserControl
{
    // ── Icon ──────────────────────────────────────────────────────
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(SymbolRegular), typeof(TextInputField),
            new PropertyMetadata(SymbolRegular.Person24));

    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // ── ShowIcon ──────────────────────────────────────────────────
    public static readonly DependencyProperty ShowIconProperty =
        DependencyProperty.Register(nameof(ShowIcon), typeof(bool), typeof(TextInputField),
            new PropertyMetadata(true));

    public bool ShowIcon
    {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    // ── IconSize ──────────────────────────────────────────────────
    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(TextInputField),
            new PropertyMetadata(16.0));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    // ── PlaceholderText ───────────────────────────────────────────
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(TextInputField),
            new PropertyMetadata(string.Empty));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    // ── Text (TwoWay) ─────────────────────────────────────────────
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TextInputField),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // ── InputFontSize ─────────────────────────────────────────────
    public static readonly DependencyProperty InputFontSizeProperty =
        DependencyProperty.Register(nameof(InputFontSize), typeof(double), typeof(TextInputField),
            new PropertyMetadata(13.0));

    public double InputFontSize
    {
        get => (double)GetValue(InputFontSizeProperty);
        set => SetValue(InputFontSizeProperty, value);
    }

    // ── CornerRadius ──────────────────────────────────────────────
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(TextInputField),
            new PropertyMetadata(new CornerRadius(10)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    // ── ShowClearButton ───────────────────────────────────────────
    public static readonly DependencyProperty ShowClearButtonProperty =
        DependencyProperty.Register(nameof(ShowClearButton), typeof(bool), typeof(TextInputField),
            new PropertyMetadata(false));

    public bool ShowClearButton
    {
        get => (bool)GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    // ── EnterCommand ──────────────────────────────────────────────
    public static readonly DependencyProperty EnterCommandProperty =
        DependencyProperty.Register(nameof(EnterCommand), typeof(ICommand), typeof(TextInputField),
            new PropertyMetadata(null));

    public ICommand? EnterCommand
    {
        get => (ICommand?)GetValue(EnterCommandProperty);
        set => SetValue(EnterCommandProperty, value);
    }

    // ── EnterCommandParameter ─────────────────────────────────────
    public static readonly DependencyProperty EnterCommandParameterProperty =
        DependencyProperty.Register(nameof(EnterCommandParameter), typeof(object), typeof(TextInputField),
            new PropertyMetadata(null));

    public object? EnterCommandParameter
    {
        get => GetValue(EnterCommandParameterProperty);
        set => SetValue(EnterCommandParameterProperty, value);
    }

    // ── ClearCommand ──────────────────────────────────────────────
    public static readonly DependencyProperty ClearCommandProperty =
        DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(TextInputField),
            new PropertyMetadata(null));

    public ICommand? ClearCommand
    {
        get => (ICommand?)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    public event EventHandler? ClearClicked;

    public TextInputField()
    {
        InitializeComponent();
    }

    private void OnContainerMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        InnerTextBox.Focus();
    }

    private void OnClearButtonClick(object sender, RoutedEventArgs e)
    {
        Text = string.Empty;
        ClearCommand?.Execute(null);
        ClearClicked?.Invoke(this, EventArgs.Empty);
        InnerTextBox.Focus();
    }

    private void OnInnerTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var param = EnterCommandParameter ?? Text;
            if (EnterCommand != null && EnterCommand.CanExecute(param))
            {
                EnterCommand.Execute(param);
                e.Handled = true;
            }
        }
    }
}

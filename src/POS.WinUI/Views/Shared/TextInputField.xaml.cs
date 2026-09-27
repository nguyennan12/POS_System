using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Shared;

/// <summary>
/// Input text generic có icon + placeholder — tái sử dụng ở mọi form của ứng dụng.
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

    // ── PlaceholderText ───────────────────────────────────────────
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(TextInputField),
            new PropertyMetadata("Nhập văn bản..."));

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

    public TextInputField()
    {
        InitializeComponent();
    }
}

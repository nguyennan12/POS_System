using System.Windows;
using System.Windows.Controls;

namespace POS.WinUI.Views.Shared.Components;

public partial class AppFooterBar : UserControl
{
    public static readonly DependencyProperty IsPosModeProperty =
        DependencyProperty.Register(
            nameof(IsPosMode),
            typeof(bool),
            typeof(AppFooterBar),
            new PropertyMetadata(false));

    public bool IsPosMode
    {
        get => (bool)GetValue(IsPosModeProperty);
        set => SetValue(IsPosModeProperty, value);
    }

    public AppFooterBar()
    {
        InitializeComponent();
    }
}

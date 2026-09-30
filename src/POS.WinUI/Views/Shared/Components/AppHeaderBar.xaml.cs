using System.Windows;
using System.Windows.Controls;

namespace POS.WinUI.Views.Shared.Components;

public partial class AppHeaderBar : UserControl
{
    public static readonly DependencyProperty IsPosModeProperty =
        DependencyProperty.Register(
            nameof(IsPosMode),
            typeof(bool),
            typeof(AppHeaderBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowSidebarToggleProperty =
        DependencyProperty.Register(
            nameof(ShowSidebarToggle),
            typeof(bool),
            typeof(AppHeaderBar),
            new PropertyMetadata(true));

    public bool IsPosMode
    {
        get => (bool)GetValue(IsPosModeProperty);
        set => SetValue(IsPosModeProperty, value);
    }

    public bool ShowSidebarToggle
    {
        get => (bool)GetValue(ShowSidebarToggleProperty);
        set => SetValue(ShowSidebarToggleProperty, value);
    }

    public AppHeaderBar()
    {
        InitializeComponent();
    }
}

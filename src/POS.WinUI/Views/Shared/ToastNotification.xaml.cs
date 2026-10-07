using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Shared;

public partial class ToastNotification : UserControl
{
    private DispatcherTimer? _autoHideTimer;

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(ToastNotification),
            new PropertyMetadata(null, OnNotificationChanged));

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty ToastTypeProperty =
        DependencyProperty.Register(
            nameof(ToastType),
            typeof(string),
            typeof(ToastNotification),
            new PropertyMetadata("Success", OnNotificationChanged));

    public string ToastType
    {
        get => (string)GetValue(ToastTypeProperty);
        set => SetValue(ToastTypeProperty, value);
    }

    public ToastNotification()
    {
        InitializeComponent();
    }

    private static void OnNotificationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ToastNotification)d;
        ctrl.UpdateToastState();
    }

    private void UpdateToastState()
    {
        _autoHideTimer?.Stop();

        if (!string.IsNullOrWhiteSpace(Message))
        {
            ToastText.Text = Message;
            ApplyStyleByType(ToastType);

            ToastBorder.IsHitTestVisible = true;
            if (Resources["EnterAnimation"] is Storyboard enterAnimation)
            {
                BeginStoryboard(enterAnimation);
            }

            // Tự động đóng Toast sau 3.5 giây
            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _autoHideTimer.Tick += (s, args) =>
            {
                _autoHideTimer?.Stop();
                HideToast();
            };
            _autoHideTimer.Start();
        }
        else
        {
            HideToast();
        }
    }

    private void ApplyStyleByType(string? type)
    {
        switch (type?.ToLowerInvariant())
        {
            case "error":
                IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ToastIcon.Symbol = SymbolRegular.Dismiss24;
                ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA"));
                break;
            case "warning":
                IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                ToastIcon.Symbol = SymbolRegular.Warning24;
                ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                break;
            case "info":
                IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                ToastIcon.Symbol = SymbolRegular.Info24;
                ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE"));
                break;
            case "success":
            default:
                IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                ToastIcon.Symbol = SymbolRegular.Checkmark24;
                ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A7F3D0"));
                break;
        }
    }

    private void OnToastClicked(object sender, MouseButtonEventArgs e)
    {
        HideToast();
    }

    private void HideToast()
    {
        _autoHideTimer?.Stop();
        ToastBorder.IsHitTestVisible = false;
        if (Resources["ExitAnimation"] is Storyboard exitAnimation)
        {
            BeginStoryboard(exitAnimation);
        }
    }
}

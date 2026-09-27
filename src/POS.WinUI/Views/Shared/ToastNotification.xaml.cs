using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POS.WinUI.Views.Shared;

/// <summary>
/// Toast thành công tái sử dụng được.
/// Bind Message từ ViewModel — khi Message không rỗng toast trượt xuống,
/// khi Message = null toast fade-out và biến mất.
/// </summary>
public partial class ToastNotification : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(ToastNotification),
            new PropertyMetadata(null, OnMessageChanged));

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ToastNotification()
    {
        InitializeComponent();
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ToastNotification)d;
        var msg = e.NewValue as string;

        if (!string.IsNullOrEmpty(msg))
        {
            ctrl.ToastText.Text = msg;
            ctrl.ToastBorder.IsHitTestVisible = true;
            ctrl.BeginStoryboard((Storyboard)ctrl.Resources["EnterAnimation"]);
        }
        else
        {
            ctrl.ToastBorder.IsHitTestVisible = false;
            ctrl.BeginStoryboard((Storyboard)ctrl.Resources["ExitAnimation"]);
        }
    }
}

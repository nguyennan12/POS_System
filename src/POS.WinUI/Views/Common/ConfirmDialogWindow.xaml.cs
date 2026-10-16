using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using POS.WinUI.Core.Services;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Common;

public partial class ConfirmDialogWindow : Window
{
    public bool Confirmed { get; private set; } = false;

    public ConfirmDialogWindow(ConfirmDialogOptions options)
    {
        InitializeComponent();

        if (Application.Current?.MainWindow != null)
        {
            Owner = Application.Current.MainWindow;
            Width = Owner.ActualWidth > 0 ? Owner.ActualWidth : 1200;
            Height = Owner.ActualHeight > 0 ? Owner.ActualHeight : 800;
            Left = Owner.Left;
            Top = Owner.Top;
        }

        TxtTitle.Text = options.Title;
        if (string.IsNullOrWhiteSpace(options.Subtitle))
        {
            TxtSubtitle.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtSubtitle.Text = options.Subtitle;
            TxtSubtitle.Visibility = Visibility.Visible;
        }

        TxtMessage.Text = options.Message;

        if (string.IsNullOrWhiteSpace(options.WarningMessage))
        {
            WarningBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtWarningMessage.Text = options.WarningMessage;
            WarningBorder.Visibility = Visibility.Visible;
        }

        TxtCancelBtn.Text = options.CancelText;
        TxtConfirmBtn.Text = options.ConfirmText;

        // Tùy biến màu theo Variant
        switch (options.Variant)
        {
            case ConfirmDialogVariant.Danger:
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                IconSymbol.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                IconSymbol.Symbol = SymbolRegular.Delete24;
                ConfirmIcon.Symbol = SymbolRegular.Delete24;
                break;

            case ConfirmDialogVariant.Warning:
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                IconSymbol.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6));
                IconSymbol.Symbol = SymbolRegular.Warning24;
                ConfirmIcon.Symbol = SymbolRegular.Checkmark24;
                break;

            case ConfirmDialogVariant.Primary:
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(255, 237, 213));
                IconSymbol.Foreground = new SolidColorBrush(Color.FromRgb(234, 88, 12));
                IconSymbol.Symbol = SymbolRegular.Info24;
                ConfirmIcon.Symbol = SymbolRegular.Checkmark24;
                break;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void OnBackdropMouseDown(object sender, MouseButtonEventArgs e)
    {
        Confirmed = false;
        Close();
    }

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // Ngăn chặn nổi bọt lên backdrop
    }
}

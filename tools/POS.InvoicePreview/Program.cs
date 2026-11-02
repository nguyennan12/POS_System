using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Orders;
using POS.WinUI.Views.Orders;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;

namespace POS.InvoicePreview;

// Standalone frontend review host. It starts no App host, login, API client or database.
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("vi-VN");
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("vi-VN")));
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ThemesDictionary { Theme = ApplicationTheme.Light });
        app.Resources.MergedDictionaries.Add(new ControlsDictionary());
        foreach (var file in new[] { "Colors", "Converters", "Styles" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/POS.WinUI;component/Resources/Styles/{file}.xaml")
            });
        using var vm = OrdersViewModel.CreatePreview(new SessionService());
        var window = new Window
        {
            Title = "OraPOS · T52 · Xem thử Hóa đơn & In lại (Frontend)",
            Width = Math.Min(1420, SystemParameters.WorkArea.Width - 32),
            Height = Math.Min(980, SystemParameters.WorkArea.Height - 32),
            MinWidth = 800, MinHeight = 640,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new frmInvoiceView(vm)
        };
        app.Run(window);
    }
}

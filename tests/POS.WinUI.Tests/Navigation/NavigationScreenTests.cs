using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.Core.Services;
using POS.WinUI.Views.Customers;
using POS.WinUI.Views.Promotions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;
using Xunit;

namespace POS.WinUI.Tests.Navigation;

public sealed class SymbolValidationTests
{
    private static readonly string[] SymbolsToCheck =
    [
        "RibbonStar24",
        "Trophy24",
        "Home24",
        "CheckmarkCircle24",
        "People24",
        "History24",
        "ArrowClockwise24",
        "PersonAdd24",
        "ErrorCircle24",
        "Dismiss24",
        "Person24",
        "Mail24",
        "BarcodeScanner24",
        "Phone24",
        "Delete24",
        "Edit24",
        "Checkmark24",
        "Note24",
        "NumberSymbol24",
        "Receipt24",
        "Tag24",
        "Money24",
        "TicketDiagonal24",
        "ReceiptMoney24",
        "MoneyHand24",
        "CalendarLtr24"
    ];

    [Fact]
    public void Verify_symbols()
    {
        var invalid = new List<string>();
        foreach (var s in SymbolsToCheck)
        {
            if (!Enum.TryParse<SymbolRegular>(s, out _))
            {
                invalid.Add(s);
            }
        }
        Assert.True(invalid.Count == 0, $"Invalid: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void Verify_views_instantiation()
    {
        Exception? ex = null;
        var t = new Thread(() =>
        {
            try
            {
                var app = Application.Current ?? new Application();
                if (app.Resources.MergedDictionaries.Count == 0)
                {
                    app.Resources.MergedDictionaries.Add(new ThemesDictionary { Theme = ApplicationTheme.Light });
                    app.Resources.MergedDictionaries.Add(new ControlsDictionary());
                    foreach (var file in new[] { "Colors", "Converters", "Styles" })
                    {
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri($"pack://application:,,,/POS.WinUI;component/Resources/Styles/{file}.xaml")
                        });
                    }
                }

                var cView = new CustomersView();
                Assert.NotNull(cView);

                var pView = new PromotionsView();
                Assert.NotNull(pView);
            }
            catch (Exception e)
            {
                ex = e;
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();

        if (ex != null)
        {
            throw new Exception($"View instantiation failed: {ex}", ex);
        }
    }

    [Fact]
    public void Verify_Navigation_To_ManagementMainView()
    {
        Exception? ex = null;
        var t = new Thread(() =>
        {
            try
            {
                var app = Application.Current ?? new Application();
                if (app.Resources.MergedDictionaries.Count == 0)
                {
                    app.Resources.MergedDictionaries.Add(new ThemesDictionary { Theme = ApplicationTheme.Light });
                    app.Resources.MergedDictionaries.Add(new ControlsDictionary());
                    foreach (var file in new[] { "Colors", "Converters", "Styles" })
                    {
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri($"pack://application:,,,/POS.WinUI;component/Resources/Styles/{file}.xaml")
                        });
                    }
                }

                var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
                services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
                services.AddHttpClient("PosApi", client => { client.BaseAddress = new Uri("http://localhost:5000/"); });
                services.AddTransient(sp => sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("PosApi"));
                POS.WinUI.Core.Extensions.ServiceCollectionExtensions.AddWinUIServices(services);

                var sp = services.BuildServiceProvider();
                var session = sp.GetRequiredService<SessionService>();
                session.SetSession("test-token", "test-refresh", Guid.NewGuid().ToString(), "Chủ Hệ Thống (Owner)", "Owner", Guid.NewGuid().ToString(), "Chi Nhánh Quận 3", true, []);

                var nav = sp.GetRequiredService<INavigationService>();
                nav.NavigateTo<POS.WinUI.Views.Shell.ManagementMainView>();

                var shell = sp.GetRequiredService<POS.WinUI.ViewModels.Shell.MainWindowViewModel>();
                Assert.NotNull(shell.CurrentView);
            }
            catch (Exception e)
            {
                ex = e;
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();

        if (ex != null)
        {
            throw new Exception($"Navigation test failed: {ex}", ex);
        }
    }
}

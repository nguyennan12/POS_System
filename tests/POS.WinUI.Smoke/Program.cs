using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Contracts.V1.Auth;
using POS.Contracts.V1.Common;
using POS.WinUI.Core.Extensions;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Employees;
using POS.WinUI.ViewModels.Orders;
using POS.WinUI.ViewModels.Common;
using POS.WinUI.Views.Employees;
using POS.WinUI.Views.Orders;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;
using Wpf.Ui.Controls;

// Manual QA host for the real view + API, without adding a login bypass to the application.
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new System.Windows.Application();
        app.Resources.MergedDictionaries.Add(new ThemesDictionary { Theme = ApplicationTheme.Light });
        app.Resources.MergedDictionaries.Add(new ControlsDictionary());
        foreach (var file in new[] { "Colors", "Converters", "Styles" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/POS.WinUI;component/Resources/Styles/{file}.xaml") });
        app.Startup += async (_, _) =>
        {
            try
            {
                var http = new HttpClient { BaseAddress = new(Environment.GetEnvironmentVariable("ApiSettings__BaseUrl") ?? "http://localhost:5000/") };
                var password = Environment.GetEnvironmentVariable("POS_SMOKE_PASSWORD") ?? throw new InvalidOperationException("Set POS_SMOKE_PASSWORD for the development seed account.");
                using var response = await http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(Environment.GetEnvironmentVariable("POS_SMOKE_USER") ?? "owner", password));
                response.EnsureSuccessStatusCode();
                var auth = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ApiSettings:BaseUrl"] = http.BaseAddress.ToString() }).Build());
                services.AddSingleton(http); services.AddWinUIServices();
                var provider = services.BuildServiceProvider();
                var session = provider.GetRequiredService<SessionService>();
                session.SetSession(auth.AccessToken, auth.RefreshToken, auth.User.Id.ToString(), auth.User.Name,
                    auth.User.RoleName, auth.User.StoreId?.ToString() ?? "", isChainOwner: auth.User.IsChainOwner, permissions: auth.User.Permissions);
                var invoices = Environment.GetEnvironmentVariable("POS_SMOKE_SCREEN") == "invoices";
                UserControl view = invoices ? provider.GetRequiredService<frmInvoiceView>() : provider.GetRequiredService<EmployeesView>();
                if (!invoices) view.DataContext = provider.GetRequiredService<EmployeesViewModel>();
                if (invoices && Environment.GetEnvironmentVariable("POS_SMOKE_VERIFY") == "true")
                {
                    var invoiceVm = (OrdersViewModel)view.DataContext;
                    await invoiceVm.InitializeAsync();
                    if (invoiceVm.IsPreview || invoiceVm.ListError != null) throw new InvalidOperationException(invoiceVm.ListError ?? "Expected live data.");
                    System.IO.File.WriteAllText("t52-live-ui-result.log", $"Live invoice screen initialized: {invoiceVm.ResultCountText}; API and JWT session verified.");
                    invoiceVm.Dispose(); provider.Dispose(); app.Shutdown(); return;
                }
                var width = double.TryParse(Environment.GetEnvironmentVariable("POS_SMOKE_WIDTH"), out var requestedWidth) ? requestedWidth : 1320;
                var window = new Window { Title = invoices ? "T52 Invoice Viewer - Live API QA" : "T21 Employee Management - Live API QA", Width = width, Height = 850, MinWidth = 760, MinHeight = 680, Content = view };
                window.Closed += (_, _) => { ((ManagementTabViewModelBase)view.DataContext).Dispose(); provider.Dispose(); };
                app.MainWindow = window; window.Show();
            }
            catch (Exception ex) { System.IO.File.WriteAllText(Environment.GetEnvironmentVariable("POS_SMOKE_SCREEN") == "invoices" ? "t52-smoke-error.log" : "t21-smoke-error.log", ex.ToString()); app.Shutdown(1); }
        };
        app.Run();
    }
}

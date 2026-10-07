using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Cashier;
using POS.WinUI.ViewModels.Common;
using POS.WinUI.ViewModels.Customers;
using POS.WinUI.ViewModels.Dashboard;
using POS.WinUI.ViewModels.Employees;
using POS.WinUI.ViewModels.Inventory;
using POS.WinUI.ViewModels.Orders;
using POS.WinUI.ViewModels.Products;
using POS.WinUI.ViewModels.Promotions;
using POS.WinUI.ViewModels.Reports;
using POS.WinUI.ViewModels.Settings;
using POS.WinUI.Views.Cashier;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Shell;

public partial class ManagementMainViewModel
{
    private void InitializeNavigationTabs()
    {
        NavTabs.Clear();

        var tabs = new List<NavigationItemViewModel>
        {
            new() { Id = "Dashboard", Title = "Tổng quan", Icon = SymbolRegular.Grid24, MinRoleLevel = 2 },
            new() { Id = "Orders", Title = "Đơn hàng & Hóa đơn", Icon = SymbolRegular.Receipt24, MinRoleLevel = 2 },
            new() { Id = "Products", Title = "Sản phẩm & Danh mục", Icon = SymbolRegular.Box24, MinRoleLevel = 2 },
            new() { Id = "Inventory", Title = "Quản lý Kho & Nhập hàng", Icon = SymbolRegular.Archive24, MinRoleLevel = 2 },
            new() { Id = "Customers", Title = "Khách hàng & Hội viên", Icon = SymbolRegular.People24, MinRoleLevel = 2 },
            new() { Id = "Promotions", Title = "Khuyến mãi & Voucher", Icon = SymbolRegular.TicketDiagonal24, MinRoleLevel = 2 },
            new() { Id = "Employees", Title = "Nhân viên & Phân quyền", Icon = SymbolRegular.PersonAccounts24, MinRoleLevel = 2 },
            new() { Id = "Reports", Title = "Báo cáo & Thống kê", Icon = SymbolRegular.DataPie24, MinRoleLevel = 2 },
            new() { Id = "Settings", Title = "Cài đặt hệ thống", Icon = SymbolRegular.Settings24, MinRoleLevel = 2 }
        };

        foreach (var tab in tabs)
        {
            if (IsTabAllowed(tab))
            {
                NavTabs.Add(tab);
            }
        }

        if (NavTabs.Count > 0)
        {
            SelectTab(NavTabs[0]);
        }
    }

    private bool IsTabAllowed(NavigationItemViewModel tab)
    {
        if (tab.MinRoleLevel > 0 && _sessionService.RoleLevel < tab.MinRoleLevel)
        {
            return false;
        }

        if (tab.AllowedRoles != null && tab.AllowedRoles.Length > 0)
        {
            if (!_sessionService.IsInRole(string.Join(',', tab.AllowedRoles)))
            {
                return false;
            }
        }

        if (tab.RequiredPermissions != null && tab.RequiredPermissions.Length > 0)
        {
            if (!_sessionService.HasAnyPermission(tab.RequiredPermissions))
            {
                return false;
            }
        }

        return true;
    }

    [RelayCommand]
    private void SelectTab(NavigationItemViewModel? tab)
    {
        if (tab == null) return;

        foreach (var item in NavTabs)
        {
            item.IsSelected = (item.Id == tab.Id);
        }

        SelectedTab = tab;
        ActiveTabTitle = tab.Title;
        ActiveTabIcon = tab.Icon;
        ActiveTabBadgeText = $"Đang ở: {tab.Title}";

        if (!_tabViewModelCache.TryGetValue(tab.Id, out var subVm))
        {
            subVm = tab.Id switch
            {
                "Dashboard" => _serviceProvider.GetRequiredService<DashboardViewModel>(),
                "Orders" => _serviceProvider.GetRequiredService<OrdersViewModel>(),
                "Products" => _serviceProvider.GetRequiredService<ProductsViewModel>(),
                "Inventory" => _serviceProvider.GetRequiredService<InventoryViewModel>(),
                "Customers" => _serviceProvider.GetRequiredService<CustomersViewModel>(),
                "Promotions" => _serviceProvider.GetRequiredService<PromotionsViewModel>(),
                "Employees" => _serviceProvider.GetRequiredService<EmployeesViewModel>(),
                "Reports" => _serviceProvider.GetRequiredService<ReportsViewModel>(),
                "Settings" => _serviceProvider.GetRequiredService<SettingsViewModel>(),
                _ => null
            };

            if (subVm != null)
            {
                _tabViewModelCache[tab.Id] = subVm;
            }
        }

        CurrentTabViewModel = subVm;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void OpenPosCashier()
    {
        _clockTimer.Stop();
        _navigationService.NavigateTo<PosCashierMainView>();
    }

    [RelayCommand]
    private void OpenShiftModal()
    {
        _clockTimer.Stop();
        _navigationService.NavigateTo<PosCashierMainView>();
        if (_serviceProvider.GetService<MainWindowViewModel>()?.CurrentView is PosCashierMainView posView &&
            posView.DataContext is PosCashierMainViewModel posVm)
        {
            posVm.OpenShiftModal();
        }
    }
}

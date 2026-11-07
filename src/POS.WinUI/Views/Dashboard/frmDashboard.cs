using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Dashboard;

namespace POS.WinUI.Views.Dashboard;

/// <summary>
/// Màn hình Executive Dashboard tổng quan (frmDashboard)
/// Tương thích với cấu trúc task T57. Có thể mở dạng Window độc lập hoặc nhúng trong ManagementMainView.
/// </summary>
public partial class frmDashboard : Window
{
    public DashboardViewModel ViewModel { get; }

    public frmDashboard() : this(App.Services?.GetService<DashboardViewModel>()!)
    {
    }

    public frmDashboard(DashboardViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Title = "Tổng quan kinh doanh (Executive Dashboard) - POS System";
        Width = 1280;
        Height = 840;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = System.Windows.Media.Brushes.White;

        // Nhúng DashboardView làm Content
        Content = new DashboardView { DataContext = ViewModel };
    }
}

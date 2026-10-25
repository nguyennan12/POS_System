using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Reports;

namespace POS.WinUI.Views.Reports;

/// <summary>
/// Màn hình Báo cáo & Thống kê kinh doanh (frmReport)
/// Tương thích với cấu trúc task T57. Có thể mở dạng Window độc lập hoặc nhúng trong ManagementMainView.
/// </summary>
public partial class frmReport : Window
{
    public ReportsViewModel ViewModel { get; }

    public frmReport() : this(App.Services?.GetService<ReportsViewModel>()!)
    {
    }

    public frmReport(ReportsViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Title = "Báo cáo & Thống kê kinh doanh (Report Visualizer) - POS System";
        Width = 1280;
        Height = 840;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = System.Windows.Media.Brushes.White;

        // Nhúng ReportsView làm Content
        Content = new ReportsView { DataContext = ViewModel };
    }
}

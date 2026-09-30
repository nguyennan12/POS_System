using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Dashboard;

/// <summary>
/// ViewModel chính cho phân hệ Tổng quan kinh doanh (Dashboard)
/// </summary>
public sealed partial class DashboardViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Dashboard";
    public override string Title => "Tổng quan kinh doanh";
    public override string Subtitle => "Theo dõi doanh thu, số lượng đơn hàng và hiệu suất bán lẻ hôm nay";
    public override SymbolRegular Icon => SymbolRegular.Grid24;

    public DashboardViewModel()
    {
        // Khởi tạo các thông số, command và dữ liệu dashboard tại đây
    }
}

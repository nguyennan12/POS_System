using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Reports;

public sealed partial class ReportsViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Reports";
    public override string Title => "Báo cáo & Thống kê";
    public override string Subtitle => "Báo cáo doanh thu ca, lợi nhuận, mặt hàng bán chạy và xuất file thống kê";
    public override SymbolRegular Icon => SymbolRegular.DataPie24;
}

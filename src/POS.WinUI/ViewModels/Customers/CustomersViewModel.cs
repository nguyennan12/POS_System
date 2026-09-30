using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Customers;

public sealed partial class CustomersViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Customers";
    public override string Title => "Khách hàng & Hội viên";
    public override string Subtitle => "Quản lý thông tin khách hàng, điểm tích lũy và chính sách hạng thành viên";
    public override SymbolRegular Icon => SymbolRegular.People24;
}

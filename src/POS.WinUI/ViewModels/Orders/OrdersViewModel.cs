using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Orders;

public sealed partial class OrdersViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Orders";
    public override string Title => "Đơn hàng & Hóa đơn";
    public override string Subtitle => "Quản lý lịch sử giao dịch bán hàng, tra cứu và xử lý đổi trả";
    public override SymbolRegular Icon => SymbolRegular.Receipt24;
}

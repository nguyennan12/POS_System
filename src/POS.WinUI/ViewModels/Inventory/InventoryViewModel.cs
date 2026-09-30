using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Inventory;

public sealed partial class InventoryViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Inventory";
    public override string Title => "Quản lý Kho & Nhập hàng";
    public override string Subtitle => "Kiểm soát số lượng tồn kho, tạo phiếu nhập và điều chuyển hàng hóa";
    public override SymbolRegular Icon => SymbolRegular.Archive24;
}

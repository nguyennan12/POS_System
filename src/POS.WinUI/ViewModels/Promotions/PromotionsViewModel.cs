using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Promotions;

public sealed partial class PromotionsViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Promotions";
    public override string Title => "Khuyến mãi & Voucher";
    public override string Subtitle => "Quản lý chương trình khuyến mãi tự động, giảm giá hóa đơn và phát hành mã Voucher";
    public override SymbolRegular Icon => SymbolRegular.TicketDiagonal24;
}

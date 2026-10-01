using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Settings;

public sealed partial class SettingsViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Settings";
    public override string Title => "Cài đặt hệ thống";
    public override string Subtitle => "Thiết lập cấu hình cửa hàng, máy in hóa đơn và tích hợp phần cứng";
    public override SymbolRegular Icon => SymbolRegular.Settings24;
}

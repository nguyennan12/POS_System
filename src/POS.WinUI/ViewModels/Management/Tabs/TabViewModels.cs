using POS.WinUI.ViewModels.Management;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Management.Tabs;

public sealed class OrdersTabViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Orders";
    public override string Title => "Đơn hàng & Hóa đơn";
    public override string Subtitle => "Quản lý lịch sử giao dịch bán hàng, tra cứu và xử lý đổi trả";
    public override SymbolRegular Icon => SymbolRegular.Receipt24;
}

public sealed class ProductsTabViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Products";
    public override string Title => "Sản phẩm & Danh mục";
    public override string Subtitle => "Quản lý danh sách hàng hóa, mã vạch SKU, giá bán và phân loại";
    public override SymbolRegular Icon => SymbolRegular.Box24;
}

public sealed class InventoryTabViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Inventory";
    public override string Title => "Quản lý Kho & Nhập hàng";
    public override string Subtitle => "Kiểm soát số lượng tồn kho, tạo phiếu nhập và điều chuyển hàng hóa";
    public override SymbolRegular Icon => SymbolRegular.Archive24;
}

public sealed class CustomersTabViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Customers";
    public override string Title => "Khách hàng & Hội viên";
    public override string Subtitle => "Quản lý thông tin khách hàng, điểm tích lũy và chính sách hạng thành viên";
    public override SymbolRegular Icon => SymbolRegular.People24;
}

public sealed class EmployeesTabViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Employees";
    public override string Title => "Nhân viên & Phân quyền";
    public override string Subtitle => "Quản lý hồ sơ nhân sự, mã PIN đăng nhập nhanh và phân quyền tài khoản";
    public override SymbolRegular Icon => SymbolRegular.PersonAccounts24;
}

public sealed class ReportsTabViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Reports";
    public override string Title => "Báo cáo & Thống kê";
    public override string Subtitle => "Báo cáo doanh thu ca, lợi nhuận, mặt hàng bán chạy và xuất file thống kê";
    public override SymbolRegular Icon => SymbolRegular.DataPie24;
}

public sealed class SettingsTabViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Settings";
    public override string Title => "Cài đặt hệ thống";
    public override string Subtitle => "Thiết lập cấu hình cửa hàng, máy in hóa đơn và tích hợp phần cứng";
    public override SymbolRegular Icon => SymbolRegular.Settings24;
}

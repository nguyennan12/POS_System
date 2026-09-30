using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Employees;

public sealed partial class EmployeesViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Employees";
    public override string Title => "Nhân viên & Phân quyền";
    public override string Subtitle => "Quản lý hồ sơ nhân sự, mã PIN đăng nhập nhanh và phân quyền tài khoản";
    public override SymbolRegular Icon => SymbolRegular.PersonAccounts24;
}

using POS.WinUI.ViewModels.Employees;

namespace POS.WinUI.Views.Employees;

// T21 entry point; the management shell hosts the same MVVM UserControl.
public sealed class frmEmployeeList : EmployeesView
{
    public frmEmployeeList(EmployeesViewModel viewModel) => DataContext = viewModel;
}

using System.Windows;
using System.Windows.Controls;
using POS.WinUI.ViewModels.Employees;

namespace POS.WinUI.Views.Employees;

public partial class EmployeeEditForm : UserControl
{
    public EmployeeEditForm() => InitializeComponent();
    private void PasswordChanged(object sender, RoutedEventArgs e)
    { if (DataContext is EmployeeEditViewModel vm) vm.Password = ((PasswordBox)sender).Password; }
    private void PinChanged(object sender, RoutedEventArgs e)
    { if (DataContext is EmployeeEditViewModel vm) vm.Pin = ((PasswordBox)sender).Password; }
}

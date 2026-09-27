using System.Windows;
using System.Windows.Controls;
using POS.WinUI.ViewModels.Auth;

namespace POS.WinUI.Views.Auth;

public partial class LoginView : UserControl
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pwdBox)
        {
            if (vm.Password != pwdBox.Password)
            {
                vm.Password = pwdBox.Password;
            }
        }
    }
}

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
}

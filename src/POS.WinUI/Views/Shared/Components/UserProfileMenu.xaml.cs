using System.Windows;
using System.Windows.Controls;

namespace POS.WinUI.Views.Shared.Components;

public partial class UserProfileMenu : UserControl
{
    public UserProfileMenu()
    {
        InitializeComponent();
    }

    private void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        ProfilePopup.IsOpen = false;
    }
}

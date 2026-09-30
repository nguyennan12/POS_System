using System.Windows.Controls;
using POS.WinUI.ViewModels.Management;

namespace POS.WinUI.Views.Management;

public partial class ManagementMainView : UserControl
{
    public ManagementMainView(ManagementMainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => Focus();
    }
}

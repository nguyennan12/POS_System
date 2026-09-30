using System.Windows.Controls;
using POS.WinUI.ViewModels.Shell;

namespace POS.WinUI.Views.Shell;

public partial class ManagementMainView : UserControl
{
    public ManagementMainView(ManagementMainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => Focus();
    }
}

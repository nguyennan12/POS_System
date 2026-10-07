using System.Windows.Controls;
using POS.WinUI.ViewModels.CustomerFacing;

namespace POS.WinUI.Views.CustomerFacing;

public partial class CfdStandbyView : UserControl
{
    public CfdStandbyView(CfdStandbyViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

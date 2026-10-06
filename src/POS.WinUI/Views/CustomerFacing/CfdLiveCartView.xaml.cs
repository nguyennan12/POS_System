using System.Windows.Controls;
using POS.WinUI.ViewModels.CustomerFacing;

namespace POS.WinUI.Views.CustomerFacing;

public partial class CfdLiveCartView : UserControl
{
    public CfdLiveCartView(CfdLiveCartViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

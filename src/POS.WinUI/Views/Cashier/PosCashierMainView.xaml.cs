using System.Windows.Controls;
using POS.WinUI.ViewModels.Cashier;

namespace POS.WinUI.Views.Cashier;

public partial class PosCashierMainView : UserControl
{
    public PosCashierMainView(PosCashierMainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => Focus();
    }
}

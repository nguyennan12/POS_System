using POS.WinUI.ViewModels.Shell;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Shell;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

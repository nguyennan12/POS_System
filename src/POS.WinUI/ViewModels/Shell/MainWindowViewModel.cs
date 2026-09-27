using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.ViewModels.Shell;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "OraPOS - Quầy Thu Ngân";

    [ObservableProperty]
    private object? _currentView;
}

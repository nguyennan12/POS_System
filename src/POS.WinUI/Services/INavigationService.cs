using System.Windows.Controls;

namespace POS.WinUI.Services;

/// <summary>
/// Điều hướng giữa các màn hình trong MainWindow.
/// </summary>
public interface INavigationService
{

    void NavigateTo<TView>() where TView : UserControl;
}

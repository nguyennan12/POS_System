using System.Windows.Controls;

namespace POS.WinUI.Core.Services;

///  
/// Điều hướng giữa các màn hình trong MainWindow.
/// </summary>
public interface INavigationService
{

  void NavigateTo<TView>() where TView : UserControl;
}

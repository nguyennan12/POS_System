using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Shell;

namespace POS.WinUI.Core.Services;

///  
/// Điều hướng màn hình bằng cách resolve View từ DI và set vào MainWindowViewModel.CurrentView.
/// </summary>
public sealed class NavigationService : INavigationService
{
  private readonly IServiceProvider _serviceProvider;
  private readonly MainWindowViewModel _shell;

  public NavigationService(IServiceProvider serviceProvider, MainWindowViewModel shell)
  {
    _serviceProvider = serviceProvider;
    _shell = shell;
  }

  /// <inheritdoc />
  public void NavigateTo<TView>() where TView : UserControl
  {
    var view = _serviceProvider.GetRequiredService<TView>();
    _shell.CurrentView = view;
  }
}

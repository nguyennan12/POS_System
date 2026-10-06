using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.Views.CustomerFacing;

namespace POS.WinUI.Core.Services;

public interface ICfdDisplayService
{
    bool IsOpen { get; }
    void OpenCfd();
    void CloseCfd();
    void ToggleCfd();
    void ShowStandby();
}

public sealed class CfdDisplayService : ICfdDisplayService
{
    private readonly IServiceProvider _serviceProvider;
    private CustomerFacingWindow? _cfdWindow;

    public bool IsOpen => _cfdWindow != null && _cfdWindow.IsVisible;

    public CfdDisplayService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void OpenCfd()
    {
        if (_cfdWindow == null || !_cfdWindow.IsLoaded)
        {
            _cfdWindow = new CustomerFacingWindow();
            _cfdWindow.Closed += (s, e) => _cfdWindow = null;
        }

        ShowStandby();
        _cfdWindow.ShowOnSecondaryScreen();
    }

    public void CloseCfd()
    {
        if (_cfdWindow != null)
        {
            _cfdWindow.Close();
            _cfdWindow = null;
        }
    }

    public void ToggleCfd()
    {
        if (IsOpen)
        {
            CloseCfd();
        }
        else
        {
            OpenCfd();
        }
    }

    public void ShowStandby()
    {
        if (_cfdWindow == null)
        {
            OpenCfd();
            return;
        }

        var standbyView = _serviceProvider.GetRequiredService<CfdStandbyView>();
        _cfdWindow.SetContent(standbyView);
    }
}

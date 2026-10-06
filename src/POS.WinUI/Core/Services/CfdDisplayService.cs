using System;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.Core.Models.Cfd;
using POS.WinUI.ViewModels.CustomerFacing;
using POS.WinUI.Views.CustomerFacing;

namespace POS.WinUI.Core.Services;

public interface ICfdDisplayService
{
    bool IsOpen { get; }
    void OpenCfd();
    void CloseCfd();
    void ToggleCfd();
    void ShowStandby();
    void ShowLiveCart();
}

public sealed class CfdDisplayService : ICfdDisplayService, IRecipient<CfdCartStateMessage>, IRecipient<CfdShowStandbyMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private CustomerFacingWindow? _cfdWindow;
    private bool _isShowingLiveCart = false;

    private CfdStandbyView? _standbyView;
    private CfdLiveCartView? _liveCartView;

    public bool IsOpen => _cfdWindow != null && _cfdWindow.IsVisible;

    public CfdDisplayService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Tự động lắng nghe các sự kiện đồng bộ từ ICfdSyncService
        WeakReferenceMessenger.Default.Register<CfdCartStateMessage>(this);
        WeakReferenceMessenger.Default.Register<CfdShowStandbyMessage>(this);
    }

    private void EnsureViewsInitialized()
    {
        // Khởi tạo trước cả 2 View & ViewModel để đăng ký nhận Messenger sẵn sàng ngay từ đầu
        _standbyView ??= _serviceProvider.GetRequiredService<CfdStandbyView>();
        _liveCartView ??= _serviceProvider.GetRequiredService<CfdLiveCartView>();
    }

    public void OpenCfd()
    {
        EnsureViewsInitialized();

        if (_cfdWindow == null || !_cfdWindow.IsLoaded)
        {
            _cfdWindow = new CustomerFacingWindow();
            _cfdWindow.Closed += (s, e) =>
            {
                _cfdWindow = null;
                _isShowingLiveCart = false;
            };
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
            _isShowingLiveCart = false;
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
            return;
        }

        EnsureViewsInitialized();
        _isShowingLiveCart = false;
        _cfdWindow.SetContent(_standbyView!);
    }

    public void ShowLiveCart()
    {
        if (_cfdWindow == null)
        {
            return;
        }

        EnsureViewsInitialized();
        if (!_isShowingLiveCart)
        {
            _isShowingLiveCart = true;
            _cfdWindow.SetContent(_liveCartView!);
        }
    }

    public void Receive(CfdCartStateMessage message)
    {
        void Update()
        {
            if (_cfdWindow != null && _cfdWindow.IsVisible)
            {
                if (message.State.TotalItems > 0)
                {
                    ShowLiveCart();
                }
                else
                {
                    ShowStandby();
                }
            }
        }

        if (Application.Current?.Dispatcher?.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(Update);
        }
        else
        {
            Update();
        }
    }

    public void Receive(CfdShowStandbyMessage message)
    {
        void Update()
        {
            if (_cfdWindow != null && _cfdWindow.IsVisible)
            {
                ShowStandby();
            }
        }

        if (Application.Current?.Dispatcher?.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(Update);
        }
        else
        {
            Update();
        }
    }
}

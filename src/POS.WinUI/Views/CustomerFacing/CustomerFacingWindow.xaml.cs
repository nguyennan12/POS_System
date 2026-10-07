using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Views.CustomerFacing;

public partial class CustomerFacingWindow : Window
{
    private bool _isFullScreen = false;
    private WindowStyle _previousStyle = WindowStyle.SingleBorderWindow;
    private WindowState _previousState = WindowState.Normal;

    public ICommand ToggleFullScreenCommand { get; }
    public ICommand CloseWindowCommand { get; }

    public CustomerFacingWindow()
    {
        InitializeComponent();

        ToggleFullScreenCommand = new RelayCommand(ToggleFullScreen);
        CloseWindowCommand = new RelayCommand(Close);
    }

    public void SetContent(object content)
    {
        CfdContentControl.Content = content;
    }

    public void ShowOnSecondaryScreen()
    {
        var screens = ScreenHelper.GetAllScreens();

        if (screens.Count > 1)
        {
            // Chọn màn hình phụ thứ 2 (không phải Primary Screen)
            var secondaryScreen = screens.FirstOrDefault(s => !s.IsPrimary);
            if (secondaryScreen.Width == 0) secondaryScreen = screens[1];

            // Đặt vị trí cửa sổ vào vùng hiển thị của màn hình thứ 2
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = secondaryScreen.Left;
            Top = secondaryScreen.Top;
            Width = secondaryScreen.Width;
            Height = secondaryScreen.Height;

            // Fullscreen borderless trên màn hình phụ
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
        }
        else
        {
            // Nếu chỉ có 1 màn hình: Mở cửa sổ 16:9 ở giữa màn hình chính để preview
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 1280;
            Height = 720;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            _isFullScreen = false;
        }

        Show();
        Activate();
    }

    public void ToggleFullScreen()
    {
        if (_isFullScreen)
        {
            WindowStyle = _previousStyle;
            WindowState = _previousState;
            _isFullScreen = false;
        }
        else
        {
            _previousStyle = WindowStyle;
            _previousState = WindowState;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
        }
    }
}

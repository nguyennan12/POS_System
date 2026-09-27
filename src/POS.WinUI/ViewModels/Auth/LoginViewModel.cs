using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Services;

namespace POS.WinUI.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly SessionService _sessionService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private bool _isPinMode = false;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [RelayCommand]
    private void SwitchLoginMode()
    {
        IsPinMode = !IsPinMode;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // TODO: Gọi AuthApiClient
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

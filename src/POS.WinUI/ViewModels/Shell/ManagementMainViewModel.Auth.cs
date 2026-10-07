using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Views.Auth;

namespace POS.WinUI.ViewModels.Shell;

public partial class ManagementMainViewModel
{
    private void LoadUserInfo()
    {
        if (!string.IsNullOrWhiteSpace(_sessionService.EmployeeName))
        {
            EmployeeName = _sessionService.EmployeeName;
            AvatarInitials = GetInitials(EmployeeName);
        }

        if (!string.IsNullOrWhiteSpace(_sessionService.Role))
        {
            RoleName = _sessionService.Role switch
            {
                "StoreManager" => "Quản lý cửa hàng",
                "Owner" => "Chủ cửa hàng / Quản lý",
                "Cashier" => "Thu ngân",
                _ => _sessionService.Role
            };
        }

        IsManagerRole = _sessionService.IsManager;
        IsOwnerRole = _sessionService.IsOwner;
        if (!_sessionService.IsManager && _sessionService.IsCashier)
        {
            _clockTimer.Stop();
            _navigationService.NavigateTo<Views.Cashier.PosCashierMainView>();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_sessionService.StoreName))
        {
            StoreName = _sessionService.StoreName;
        }
        else if (!string.IsNullOrWhiteSpace(_sessionService.StoreId))
        {
            StoreName = "Chi nhánh mặc định";
        }
    }

    private static string GetInitials(string fullName)
    {
        var clean = Regex.Replace(fullName, @"[^\p{L}\s]", "").Trim();
        var parts = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "POS";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    [RelayCommand]
    private void Refresh()
    {
        CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        _ = LoadCurrentShiftAsync();
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == "VI" ? "EN" : "VI";
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _clockTimer.Stop();
        var refreshToken = _sessionService.RefreshToken;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _authApiClient.LogoutAsync(refreshToken);
            }
            catch
            {
                // Bỏ qua lỗi mạng khi logout để người dùng luôn có thể đăng xuất cục bộ
            }
        }
        _sessionService.Clear();
        _navigationService.NavigateTo<LoginView>();
    }
}

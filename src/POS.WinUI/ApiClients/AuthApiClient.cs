using System.Net.Http;
using POS.Contracts.V1.Auth;
using POS.Contracts.V1.Common;
using POS.WinUI.Constants;
using POS.WinUI.Services;

namespace POS.WinUI.ApiClients;

public sealed class AuthApiClient : BaseApiClient
{
    public AuthApiClient(HttpClient http, SessionService session)
        : base(http, session)
    {
    }

    public async Task<ApiResponse<AuthResponse>?> LoginWithPasswordAsync(string username, string password, CancellationToken ct = default)
    {
        var request = new LoginRequest(username, password);
        return await PostAsync<ApiResponse<AuthResponse>>(ApiRoutes.Auth.Login, request, ct);
    }

    public async Task<ApiResponse<AuthResponse>?> LoginWithPinAsync(Guid storeId, string pin, string? deviceId = null, CancellationToken ct = default)
    {
        var request = new PinLoginRequest(pin, storeId);
        if (!string.IsNullOrEmpty(deviceId))
        {
            _http.DefaultRequestHeaders.Remove("X-Device-Id");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Device-Id", deviceId);
        }
        else if (!_http.DefaultRequestHeaders.Contains("X-Device-Id"))
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Device-Id", Environment.MachineName);
        }
        return await PostAsync<ApiResponse<AuthResponse>>(ApiRoutes.Auth.PinLogin, request, ct);
    }

    public async Task<ApiResponse<AuthResponse>?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var request = new RefreshTokenRequest(refreshToken);
        return await PostAsync<ApiResponse<AuthResponse>>(ApiRoutes.Auth.Refresh, request, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var request = new LogoutRequest(refreshToken);
        await PostAsync<object>(ApiRoutes.Auth.Logout, request, ct);
    }
}

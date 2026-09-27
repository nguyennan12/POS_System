using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.WinUI.Services;

namespace POS.WinUI.ApiClients;

public sealed class HealthApiClient : BaseApiClient
{
    public HealthApiClient(HttpClient http, SessionService session)
        : base(http, session)
    {
    }

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> CheckDatabaseHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("health/db", ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(ct);
            }
            return "Unhealthy";
        }
        catch (Exception ex)
        {
            return $"Lỗi kết nối: {ex.Message}";
        }
    }
}

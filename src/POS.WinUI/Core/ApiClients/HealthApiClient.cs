using System.Net.Http;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;


[Obsolete("Use NetworkStatusService.CheckHealthAsync() instead. This class will be removed in a future cleanup.", error: false)]
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
            var response = await _http.GetAsync(ApiRoutes.Health, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

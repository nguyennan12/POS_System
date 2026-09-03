namespace POS.WinUI.ApiClients;

using POS.WinUI.Services;

/// Gọi REST API cho resource 'stores'
public sealed class StoreApiClient : BaseApiClient
{
  public StoreApiClient(HttpClient http, SessionService session)
      : base(http, session) { }

  public Task<object?> CreateAsync(object request, CancellationToken ct = default)
      => PostAsync<object>("api/v1/stores", request, ct);
}

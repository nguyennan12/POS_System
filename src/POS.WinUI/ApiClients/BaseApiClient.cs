using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using POS.WinUI.Services;
namespace POS.WinUI.ApiClients;

/// Base class cho tất cả ApiClient.
/// Quản lý HttpClient, tự động gắn JWT Bearer token từ SessionService,
public abstract class BaseApiClient
{
  protected readonly HttpClient _http;
  protected readonly SessionService _session;

  private static readonly JsonSerializerOptions _jsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  protected BaseApiClient(HttpClient http, SessionService session)
  {
    _http = http;
    _session = session;
  }

  /// Gắn Authorization header từ token hiện tại
  protected void AttachToken()
  {
    var token = _session.AccessToken;
    if (!string.IsNullOrEmpty(token))
      _http.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue("Bearer", token);
  }

  protected async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
  {
    AttachToken();
    var response = await _http.GetAsync(url, ct);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
  }

  protected async Task<T?> PostAsync<T>(string url, object body, CancellationToken ct = default)
  {
    AttachToken();
    var response = await _http.PostAsJsonAsync(url, body, ct);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
  }

  protected async Task<T?> PutAsync<T>(string url, object body, CancellationToken ct = default)
  {
    AttachToken();
    var response = await _http.PutAsJsonAsync(url, body, ct);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
  }

  protected async Task DeleteHttpAsync(string url, CancellationToken ct = default)
  {
    AttachToken();
    var response = await _http.DeleteAsync(url, ct);
    response.EnsureSuccessStatusCode();
  }
}

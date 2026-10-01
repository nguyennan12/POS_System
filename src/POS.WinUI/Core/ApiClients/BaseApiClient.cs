using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

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
    await HandleFailureResponseAsync(response, ct);
    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
  }

  protected async Task<T?> PostAsync<T>(string url, object body, CancellationToken ct = default)
  {
    AttachToken();
    var response = await _http.PostAsJsonAsync(url, body, ct);
    await HandleFailureResponseAsync(response, ct);
    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
  }

  protected async Task<T?> PutAsync<T>(string url, object body, CancellationToken ct = default)
  {
    AttachToken();
    var response = await _http.PutAsJsonAsync(url, body, ct);
    await HandleFailureResponseAsync(response, ct);
    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
  }

  protected async Task DeleteHttpAsync(string url, CancellationToken ct = default)
  {
    AttachToken();
    var response = await _http.DeleteAsync(url, ct);
    await HandleFailureResponseAsync(response, ct);
  }

  private static async Task HandleFailureResponseAsync(HttpResponseMessage response, CancellationToken ct)
  {
    if (response.IsSuccessStatusCode) return;

    string? customMessage = null;
    try
    {
      var rawContent = await response.Content.ReadAsStringAsync(ct);
      if (!string.IsNullOrWhiteSpace(rawContent))
      {
        using var doc = JsonDocument.Parse(rawContent);
        var root = doc.RootElement;
        var validationMessages = new List<string>();

        // 1. Kiểm tra validationErrors trong error object: "error": { "validationErrors": { "Field": ["Msg"] } }
        if (root.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == JsonValueKind.Object)
        {
          if (errorProp.TryGetProperty("validationErrors", out var valErrorsProp) && valErrorsProp.ValueKind == JsonValueKind.Object)
          {
            foreach (var prop in valErrorsProp.EnumerateObject())
            {
              if (prop.Value.ValueKind == JsonValueKind.Array)
              {
                foreach (var item in prop.Value.EnumerateArray())
                {
                  var str = item.GetString();
                  if (!string.IsNullOrWhiteSpace(str)) validationMessages.Add(str);
                }
              }
              else if (prop.Value.ValueKind == JsonValueKind.String)
              {
                var str = prop.Value.GetString();
                if (!string.IsNullOrWhiteSpace(str)) validationMessages.Add(str);
              }
            }
          }

          if (validationMessages.Count > 0)
          {
            customMessage = string.Join("\n", validationMessages);
          }
          else if (errorProp.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
          {
            var msg = msgProp.GetString();
            if (!string.IsNullOrWhiteSpace(msg) && !msg.Equals("A validation problem occurred.", StringComparison.OrdinalIgnoreCase))
            {
              customMessage = msg;
            }
          }
        }

        // 2. Kiểm tra format ProblemDetails: "errors": { "Field": ["Msg"] }
        if (string.IsNullOrWhiteSpace(customMessage) && root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
        {
          foreach (var prop in errorsProp.EnumerateObject())
          {
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
              foreach (var item in prop.Value.EnumerateArray())
              {
                var str = item.GetString();
                if (!string.IsNullOrWhiteSpace(str)) validationMessages.Add(str);
              }
            }
            else if (prop.Value.ValueKind == JsonValueKind.String)
            {
              var str = prop.Value.GetString();
              if (!string.IsNullOrWhiteSpace(str)) validationMessages.Add(str);
            }
          }

          if (validationMessages.Count > 0)
          {
            customMessage = string.Join("\n", validationMessages);
          }
        }

        // 3. Fallback sang các trường message / detail / title nếu chưa lấy được
        if (string.IsNullOrWhiteSpace(customMessage))
        {
          if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
          {
            customMessage = msgProp.GetString();
          }
          else if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
          {
            customMessage = detailProp.GetString();
          }
          else if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
          {
            customMessage = titleProp.GetString();
          }
        }
      }
    }
    catch
    {
      // Fall back to standard status code exception
    }

    if (!string.IsNullOrWhiteSpace(customMessage))
    {
      throw new InvalidOperationException(customMessage);
    }

    response.EnsureSuccessStatusCode();
  }
}

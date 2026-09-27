namespace POS.WinUI.ApiClients;

using System.Collections.Generic;
using System.Net.Http;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Stores;
using POS.WinUI.Services;

/// Gọi REST API cho resource 'stores'
public sealed class StoreApiClient : BaseApiClient
{
  public StoreApiClient(HttpClient http, SessionService session)
      : base(http, session) { }

  public Task<ApiResponse<List<StoreResponse>>?> GetPublicStoresAsync(CancellationToken ct = default)
      => GetAsync<ApiResponse<List<StoreResponse>>>("api/v1/stores/public", ct);

  public Task<ApiResponse<List<StoreResponse>>?> GetAllAsync(CancellationToken ct = default)
      => GetAsync<ApiResponse<List<StoreResponse>>>("api/v1/stores", ct);

  public Task<ApiResponse<StoreDetailResponse>?> CreateAsync(CreateStoreRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<StoreDetailResponse>>("api/v1/stores", request, ct);
}

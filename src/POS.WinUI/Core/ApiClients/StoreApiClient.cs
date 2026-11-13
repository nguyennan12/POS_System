using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Stores;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Gọi REST API cho resource 'stores'
/// </summary>
public sealed class StoreApiClient : BaseApiClient
{
    public StoreApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    public Task<ApiResponse<List<StoreResponse>>?> GetPublicStoresAsync(CancellationToken ct = default)
        => GetAsync<ApiResponse<List<StoreResponse>>>(ApiRoutes.Stores.Public, ct);

    public Task<ApiResponse<List<StoreResponse>>?> GetAllAsync(CancellationToken ct = default)
        => GetAsync<ApiResponse<List<StoreResponse>>>(ApiRoutes.Stores.All, ct);

    public Task<ApiResponse<StoreDetailResponse>?> CreateAsync(CreateStoreRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<StoreDetailResponse>>(ApiRoutes.Stores.All, request, ct);

    public Task<ApiResponse<StoreDetailResponse>?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => GetAsync<ApiResponse<StoreDetailResponse>>(ApiRoutes.Stores.GetById(id), ct);

    public Task<ApiResponse<StoreDetailResponse>?> UpdateAsync(Guid id, UpdateStoreRequest request, CancellationToken ct = default)
        => PutAsync<ApiResponse<StoreDetailResponse>>(ApiRoutes.Stores.GetById(id), request, ct);
}

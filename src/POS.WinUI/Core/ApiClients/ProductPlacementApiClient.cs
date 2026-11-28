using System.Net.Http;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

public sealed class ProductPlacementApiClient(HttpClient http, SessionService session) : BaseApiClient(http, session)
{
    public Task<ApiResponse<ProductLocationsResponse>?> GetBySkuAsync(
        Guid skuId,
        Guid? storeId = null,
        CancellationToken ct = default)
    {
        var url = ApiRoutes.ProductPlacements.BySku(skuId);
        if (storeId.HasValue) url += $"?storeId={storeId.Value}";
        return GetAsync<ApiResponse<ProductLocationsResponse>>(url, ct);
    }

    public Task<ApiResponse<ShelfContentsResponse>?> GetShelfContentsAsync(
        Guid shelfId,
        Guid? storeId = null,
        CancellationToken ct = default)
    {
        var url = ApiRoutes.ProductPlacements.ShelfContents(shelfId);
        if (storeId.HasValue) url += $"?storeId={storeId.Value}";
        return GetAsync<ApiResponse<ShelfContentsResponse>>(url, ct);
    }

    public Task<ApiResponse<PagedResponse<PlacementHistoryResponse>>?> GetHistoryAsync(
        Guid skuId,
        Guid? storeId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var url = $"{ApiRoutes.ProductPlacements.History(skuId)}?pageNumber={pageNumber}&pageSize={pageSize}";
        if (storeId.HasValue) url += $"&storeId={storeId.Value}";
        return GetAsync<ApiResponse<PagedResponse<PlacementHistoryResponse>>>(url, ct);
    }

    public Task<ApiResponse<ProductPlacementResponse>?> AssignAsync(
        AssignProductPlacementRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<ProductPlacementResponse>>(ApiRoutes.ProductPlacements.Base, request, ct);

    public Task<ApiResponse<ProductPlacementResponse>?> MoveAsync(
        Guid id,
        MoveProductPlacementRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<ProductPlacementResponse>>(ApiRoutes.ProductPlacements.Move(id), request, ct);

    public Task<ApiResponse<ProductPlacementResponse>?> SetPrimaryAsync(
        Guid id,
        SetPrimaryPlacementRequest request,
        CancellationToken ct = default)
        => PutAsync<ApiResponse<ProductPlacementResponse>>(ApiRoutes.ProductPlacements.Primary(id), request, ct);

    public Task<ApiResponse<LayoutMutationResponse>?> RemoveAsync(
        Guid id,
        Guid layoutId,
        int expectedRevision,
        string? note = null,
        CancellationToken ct = default)
    {
        var url = $"{ApiRoutes.ProductPlacements.Delete(id)}?layoutId={layoutId}&expectedRevision={expectedRevision}";
        if (!string.IsNullOrWhiteSpace(note)) url += $"&note={Uri.EscapeDataString(note)}";
        return DeleteAsync<ApiResponse<LayoutMutationResponse>>(url, ct);
    }
}

using System.Net.Http;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

public sealed class StoreLayoutApiClient(HttpClient http, SessionService session) : BaseApiClient(http, session)
{
    public Task<ApiResponse<PagedResponse<StoreLayoutSummaryResponse>>?> GetLayoutsAsync(
        Guid? storeId = null,
        StoreLayoutStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var url = $"{ApiRoutes.StoreLayouts.Base}?pageNumber={pageNumber}&pageSize={pageSize}";
        if (storeId.HasValue) url += $"&storeId={storeId.Value}";
        if (status.HasValue) url += $"&status={status.Value}";
        return GetAsync<ApiResponse<PagedResponse<StoreLayoutSummaryResponse>>>(url, ct);
    }

    public Task<ApiResponse<StoreLayoutDetailResponse?>?> GetActiveAsync(
        Guid? storeId = null,
        CancellationToken ct = default)
    {
        var url = ApiRoutes.StoreLayouts.Active;
        if (storeId.HasValue) url += $"?storeId={storeId.Value}";
        return GetAsync<ApiResponse<StoreLayoutDetailResponse?>>(url, ct);
    }

    public Task<ApiResponse<StoreLayoutDetailResponse>?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
        => GetAsync<ApiResponse<StoreLayoutDetailResponse>>(ApiRoutes.StoreLayouts.ById(id), ct);

    public Task<ApiResponse<StoreLayoutDetailResponse>?> CreateAsync(
        CreateStoreLayoutRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<StoreLayoutDetailResponse>>(ApiRoutes.StoreLayouts.Base, request, ct);

    public Task<ApiResponse<StoreLayoutDetailResponse>?> UpdateAsync(
        Guid id,
        UpdateStoreLayoutRequest request,
        CancellationToken ct = default)
        => PutAsync<ApiResponse<StoreLayoutDetailResponse>>(ApiRoutes.StoreLayouts.ById(id), request, ct);

    public Task<ApiResponse<CloneStoreLayoutResponse>?> CloneAsync(
        Guid id,
        CloneStoreLayoutRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<CloneStoreLayoutResponse>>(ApiRoutes.StoreLayouts.Clone(id), request, ct);

    public Task<ApiResponse<StoreLayoutDetailResponse>?> PublishAsync(
        Guid id,
        PublishStoreLayoutRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<StoreLayoutDetailResponse>>(ApiRoutes.StoreLayouts.Publish(id), request, ct);

    public Task<ApiResponse<StoreLayoutDetailResponse>?> ArchiveAsync(
        Guid id,
        ArchiveStoreLayoutRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<StoreLayoutDetailResponse>>(ApiRoutes.StoreLayouts.Archive(id), request, ct);

    public Task<ApiResponse<StoreLayoutDetailResponse>?> UpdateCellsAsync(
        Guid id,
        UpdateLayoutCellsRequest request,
        CancellationToken ct = default)
        => PutAsync<ApiResponse<StoreLayoutDetailResponse>>(ApiRoutes.StoreLayouts.Cells(id), request, ct);

    public Task<ApiResponse<StoreZoneResponse>?> CreateZoneAsync(
        Guid id,
        CreateZoneRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<StoreZoneResponse>>(ApiRoutes.StoreLayouts.Zones(id), request, ct);

    public Task<ApiResponse<StoreZoneResponse>?> UpdateZoneAsync(
        Guid id,
        Guid zoneId,
        UpdateZoneRequest request,
        CancellationToken ct = default)
        => PutAsync<ApiResponse<StoreZoneResponse>>(ApiRoutes.StoreLayouts.ZoneById(id, zoneId), request, ct);

    public Task<ApiResponse<LayoutMutationResponse>?> DeleteZoneAsync(
        Guid id,
        Guid zoneId,
        int expectedRevision,
        CancellationToken ct = default)
        => DeleteAsync<ApiResponse<LayoutMutationResponse>>($"{ApiRoutes.StoreLayouts.ZoneById(id, zoneId)}?expectedRevision={expectedRevision}", ct);

    public Task<ApiResponse<ShelfResponse>?> CreateShelfAsync(
        Guid id,
        CreateShelfRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<ShelfResponse>>(ApiRoutes.StoreLayouts.Shelves(id), request, ct);

    public Task<ApiResponse<ShelfResponse>?> UpdateShelfAsync(
        Guid id,
        Guid shelfId,
        UpdateShelfRequest request,
        CancellationToken ct = default)
        => PutAsync<ApiResponse<ShelfResponse>>(ApiRoutes.StoreLayouts.ShelfById(id, shelfId), request, ct);

    public Task<ApiResponse<LayoutMutationResponse>?> DeleteShelfAsync(
        Guid id,
        Guid shelfId,
        int expectedRevision,
        CancellationToken ct = default)
        => DeleteAsync<ApiResponse<LayoutMutationResponse>>($"{ApiRoutes.StoreLayouts.ShelfById(id, shelfId)}?expectedRevision={expectedRevision}", ct);

    public Task<ApiResponse<ShelfLevelResponse>?> AddShelfLevelAsync(
        Guid id,
        Guid shelfId,
        AddShelfLevelRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<ShelfLevelResponse>>(ApiRoutes.StoreLayouts.ShelfLevels(id, shelfId), request, ct);

    public Task<ApiResponse<ShelfLevelResponse>?> UpdateShelfLevelAsync(
        Guid id,
        Guid shelfId,
        Guid levelId,
        UpdateShelfLevelRequest request,
        CancellationToken ct = default)
        => PutAsync<ApiResponse<ShelfLevelResponse>>(ApiRoutes.StoreLayouts.ShelfLevelById(id, shelfId, levelId), request, ct);

    public Task<ApiResponse<LayoutMutationResponse>?> DeleteShelfLevelAsync(
        Guid id,
        Guid shelfId,
        Guid levelId,
        int expectedRevision,
        CancellationToken ct = default)
        => DeleteAsync<ApiResponse<LayoutMutationResponse>>($"{ApiRoutes.StoreLayouts.ShelfLevelById(id, shelfId, levelId)}?expectedRevision={expectedRevision}", ct);

    public Task<ApiResponse<PickingRoutePreviewResponse>?> PreviewRouteAsync(
        PreviewPickingRouteRequest request,
        Guid? layoutId = null,
        CancellationToken ct = default)
    {
        var route = layoutId.HasValue
            ? $"{ApiRoutes.StoreLayouts.ById(layoutId.Value)}/route-preview"
            : ApiRoutes.StoreLayouts.RoutePreview;
        return PostAsync<ApiResponse<PickingRoutePreviewResponse>>(route, request, ct);
    }
}

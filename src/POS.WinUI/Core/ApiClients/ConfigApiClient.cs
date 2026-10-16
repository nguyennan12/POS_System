using System.Collections.Generic;
using System.Net.Http;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Config;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>Gọi REST API cho resource 'config' (SystemConfig và i18n).</summary>
public sealed class ConfigApiClient : BaseApiClient
{
    public ConfigApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    public Task<ApiResponse<StoreConfigResponse>?> GetStoreConfigAsync(
        Guid? storeId, CancellationToken ct = default)
        => GetAsync<ApiResponse<StoreConfigResponse>>(
            storeId.HasValue
                ? $"{ApiRoutes.Config.Store}?storeId={storeId}"
                : ApiRoutes.Config.Store, ct);

    public Task<ApiResponse<StoreConfigResponse>?> UpdateStoreConfigAsync(
        Guid? storeId, UpdateStoreConfigRequest request, CancellationToken ct = default)
        => PutAsync<ApiResponse<StoreConfigResponse>>(
            storeId.HasValue
                ? $"{ApiRoutes.Config.Store}?storeId={storeId}"
                : ApiRoutes.Config.Store, request, ct);

    public Task<ApiResponse<I18nDictionaryResponse>?> GetI18nDictionaryAsync(
        string languageCode, CancellationToken ct = default)
        => GetAsync<ApiResponse<I18nDictionaryResponse>>(
            ApiRoutes.Config.I18n(languageCode), ct);

    public Task<ApiResponse<I18nDictionaryResponse>?> UpdateI18nDictionaryAsync(
        string languageCode, UpdateI18nDictionaryRequest request, CancellationToken ct = default)
        => PutAsync<ApiResponse<I18nDictionaryResponse>>(
            ApiRoutes.Config.I18n(languageCode), request, ct);
}

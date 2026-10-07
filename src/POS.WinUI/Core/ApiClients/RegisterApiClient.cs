using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Registers;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

public sealed class RegisterApiClient : BaseApiClient
{
    public RegisterApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    public Task<ApiResponse<IReadOnlyList<RegisterResponse>>?> GetRegistersByStoreAsync(Guid storeId, CancellationToken ct = default)
        => GetAsync<ApiResponse<IReadOnlyList<RegisterResponse>>>(ApiRoutes.Stores.Registers(storeId), ct);

    public Task<ApiResponse<RegisterResponse>?> CreateRegisterAsync(Guid storeId, CreateRegisterRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<RegisterResponse>>(ApiRoutes.Stores.Registers(storeId), request, ct);
}

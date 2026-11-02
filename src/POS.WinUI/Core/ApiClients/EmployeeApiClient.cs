using System.Net.Http;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

public sealed class EmployeeApiClient(HttpClient http, SessionService session) : BaseApiClient(http, session)
{
    private const string Route = "api/v1/employees";

    public async Task<PagedResponse<EmployeeResponse>> GetAllAsync(EmployeeFilterRequest filter, CancellationToken ct = default)
    {
        var query = $"?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";
        if (filter.StoreId.HasValue) query += $"&storeId={filter.StoreId}";
        if (filter.RoleId.HasValue) query += $"&roleId={filter.RoleId}";
        if (filter.IsActive.HasValue) query += $"&isActive={filter.IsActive.Value.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrWhiteSpace(filter.Search)) query += $"&search={Uri.EscapeDataString(filter.Search.Trim())}";
        return RequireData(await GetAsync<ApiResponse<PagedResponse<EmployeeResponse>>>(Route + query, ct));
    }

    public async Task<EmployeeOptionsResponse> GetOptionsAsync(CancellationToken ct = default) =>
        RequireData(await GetAsync<ApiResponse<EmployeeOptionsResponse>>(Route + "/options", ct));
    public async Task<EmployeeDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        RequireData(await GetAsync<ApiResponse<EmployeeDetailResponse>>($"{Route}/{id}", ct));
    public async Task<EmployeeDetailResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default) =>
        RequireData(await PostAsync<ApiResponse<EmployeeDetailResponse>>(Route, request, ct));
    public async Task<EmployeeDetailResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default) =>
        RequireData(await PutAsync<ApiResponse<EmployeeDetailResponse>>($"{Route}/{id}", request, ct));
    public async Task<EmployeeDetailResponse> LockAsync(Guid id, LockEmployeeRequest request, CancellationToken ct = default) =>
        RequireData(await PutAsync<ApiResponse<EmployeeDetailResponse>>($"{Route}/{id}/lock", request, ct));
    public async Task<EmployeeDetailResponse> ResetPinAsync(Guid id, ResetPinRequest request, CancellationToken ct = default) =>
        RequireData(await PostAsync<ApiResponse<EmployeeDetailResponse>>($"{Route}/{id}/reset-pin", request, ct));
    public async Task<EmployeeDetailResponse> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default) =>
        RequireData(await PostAsync<ApiResponse<EmployeeDetailResponse>>($"{Route}/{id}/reset-password", request, ct));

    private static T RequireData<T>(ApiResponse<T>? response) => response is { Success: true, Data: not null }
        ? response.Data : throw new InvalidOperationException(response?.Error?.Message ?? "Employee API returned no data.");
}

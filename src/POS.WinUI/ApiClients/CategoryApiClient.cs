using System.Net.Http;
using System.Text;
using POS.Contracts.V1.Categories;
using POS.Contracts.V1.Common;
using POS.WinUI.Constants;
using POS.WinUI.Services;

namespace POS.WinUI.ApiClients;

public sealed class CategoryApiClient : BaseApiClient
{
    public CategoryApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    public Task<ApiResponse<List<CategoryResponse>>?> GetAllAsync(CancellationToken ct = default) =>
        GetAsync<ApiResponse<List<CategoryResponse>>>(ApiRoutes.Categories.All, ct);
}

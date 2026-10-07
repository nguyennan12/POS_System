using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Categories;
using POS.Contracts.V1.Common;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

///  
/// Client gọi REST API cho danh mục sản phẩm (Categories)
/// </summary>
public sealed class CategoryApiClient : BaseApiClient
{
  public CategoryApiClient(HttpClient http, SessionService session)
      : base(http, session) { }


  /// Lấy cây danh mục sản phẩm đang hiển thị
  /// </summary>
  public Task<ApiResponse<List<CategoryResponse>>?> GetCategoriesTreeAsync(CancellationToken ct = default)
      => GetAsync<ApiResponse<List<CategoryResponse>>>(ApiRoutes.Categories.Tree, ct);
}

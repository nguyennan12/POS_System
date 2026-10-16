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


  /// <summary>
  /// Lấy cây danh mục sản phẩm đang hiển thị
  /// </summary>
  public Task<ApiResponse<List<CategoryResponse>>?> GetCategoriesTreeAsync(CancellationToken ct)
      => GetCategoriesTreeAsync(null, ct);

  /// <summary>
  /// Lấy cây danh mục sản phẩm theo cửa hàng mục tiêu (hoặc cửa hàng hiện tại)
  /// </summary>
  public Task<ApiResponse<List<CategoryResponse>>?> GetCategoriesTreeAsync(Guid? storeId = null, CancellationToken ct = default)
  {
      var effectiveStoreId = storeId ?? (Guid.TryParse(_session.StoreId, out var sid) ? sid : null);
      var url = effectiveStoreId.HasValue
          ? $"{ApiRoutes.Categories.Tree}?storeId={effectiveStoreId.Value}"
          : ApiRoutes.Categories.Tree;
      return GetAsync<ApiResponse<List<CategoryResponse>>>(url, ct);
  }

  /// <summary>
  /// Tạo danh mục mới
  /// </summary>
  public Task<ApiResponse<CategoryResponse>?> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<CategoryResponse>>(ApiRoutes.Categories.Tree, request, ct);

  /// <summary>
  /// Cập nhật danh mục
  /// </summary>
  public Task<ApiResponse<CategoryResponse>?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
      => PutAsync<ApiResponse<CategoryResponse>>(ApiRoutes.Categories.GetById(id), request, ct);

  /// <summary>
  /// Xóa danh mục
  /// </summary>
  public Task<ApiResponse<bool>?> DeleteCategoryAsync(Guid id, CancellationToken ct = default)
      => DeleteAsync<ApiResponse<bool>>(ApiRoutes.Categories.GetById(id), ct);
}

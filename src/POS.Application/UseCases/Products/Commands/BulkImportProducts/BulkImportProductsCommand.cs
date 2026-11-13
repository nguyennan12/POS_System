using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.BulkImportProducts;

/// <summary>
/// Command nhập danh sách sản phẩm/SKU hàng loạt từ file Excel.
/// Implement IRequirePermission để pipeline tự động enforce quyền "products:import" [FIX-9].
/// </summary>
public record BulkImportProductsCommand(
    /// <summary>Nội dung nhị phân của file Excel.</summary>
    byte[] FileContent,
    /// <summary>Tên file gốc — dùng để validate extension (.xlsx/.xls) ở Application layer.</summary>
    string? FileName = null,
    /// <summary>ID cửa hàng mục tiêu (tuỳ chọn cho Owner).</summary>
    Guid? StoreId = null,
    /// <summary>Số lượng tồn ban đầu mặc định nếu file không có cột tồn kho (ví dụ: san_pham.xlsx).</summary>
    decimal? DefaultInitialStock = null,
    /// <summary>Định mức tồn tối thiểu mặc định.</summary>
    decimal? DefaultMinStock = null,
    /// <summary>Tự động tạo danh mục nếu chưa có trong cửa hàng.</summary>
    bool AutoCreateCategories = true
) : ICommand<BulkImportResultDto>, IRequirePermission
{
    public string RequiredPermission => "products:import";
}

public record BulkImportResultDto(
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> Errors
);

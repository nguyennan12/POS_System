using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.BulkImportProducts;

/// <summary>
/// Command nhập danh sách sản phẩm/SKU hàng loạt từ file Excel.
/// Implement IRequirePermission để pipeline tự động enforce quyền "products:create" [FIX-9].
/// </summary>
public record BulkImportProductsCommand(
    /// <summary>Nội dung nhị phân của file Excel.</summary>
    byte[] FileContent,
    /// <summary>Tên file gốc — dùng để validate extension (.xlsx/.xls) ở Application layer.</summary>
    string? FileName = null
) : ICommand<BulkImportResultDto>, IRequirePermission
{
    public string RequiredPermission => "products:create";
}

public record BulkImportResultDto(
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> Errors
);

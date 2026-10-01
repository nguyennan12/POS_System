using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.BulkImportProducts;

public record BulkImportProductsCommand(
    byte[] FileContent
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

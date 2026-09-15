using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Queries.GetSuppliers;

public record GetSuppliersQuery(
    string? Search,
    bool? IsActive,
    int PageNumber,
    int PageSize
) : IQuery<PagedSupplierList>, IRequirePermission
{
    public string RequiredPermission => "suppliers:read";
}

public record PagedSupplierList(
    List<SupplierDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

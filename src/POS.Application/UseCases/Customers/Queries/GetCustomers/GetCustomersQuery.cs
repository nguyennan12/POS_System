using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Queries.GetCustomers;

public record GetCustomersQuery(
    string? Phone = null,
    string? Name = null,
    string? Barcode = null,
    Guid? MemberTierId = null,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedCustomerList>, IRequirePermission
{
    public string RequiredPermission => "customers:read";
}

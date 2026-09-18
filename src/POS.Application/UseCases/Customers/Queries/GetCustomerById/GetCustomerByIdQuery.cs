using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id) : IQuery<CustomerDto>, IRequirePermission
{
    public string RequiredPermission => "customers:read";
}

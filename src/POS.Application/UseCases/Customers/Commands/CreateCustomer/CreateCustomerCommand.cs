using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(
    string Name,
    string Phone,
    string? Email = null,
    DateOnly? Dob = null,
    string? Barcode = null
) : ICommand<CustomerDto>, IRequirePermission
{
    public string RequiredPermission => "customers:create";
}

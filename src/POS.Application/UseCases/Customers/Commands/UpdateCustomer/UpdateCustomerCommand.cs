using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Phone,
    string? Email = null,
    DateOnly? Dob = null,
    string? Barcode = null,
    bool IsActive = true
) : ICommand<CustomerDto>, IRequirePermission
{
    public string RequiredPermission => "customers:update";
}

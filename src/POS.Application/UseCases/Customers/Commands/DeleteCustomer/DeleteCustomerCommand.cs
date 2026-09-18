using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id) : ICommand<bool>, IRequirePermission
{
    public string RequiredPermission => "customers:delete";
}

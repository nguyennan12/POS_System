using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Commands.CreateSupplier;

public record CreateSupplierCommand(
    string Name,
    string? TaxCode,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? CreditTerms
) : ICommand<SupplierDto>, IRequirePermission
{
    public string RequiredPermission => "suppliers:create";
}

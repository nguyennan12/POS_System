using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Commands.CreateSupplierPayment;

public record CreateSupplierPaymentCommand(
    Guid SupplierId,
    decimal Amount,
    string Method,
    Guid? VoucherId,
    string? Note
) : ICommand<SupplierPaymentDto>, IRequirePermission
{
    public string RequiredPermission => "suppliers:update";
}

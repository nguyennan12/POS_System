using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Commands.UpdateSupplier;

public record UpdateSupplierCommand(
    Guid SupplierId,
    string Name,
    string? TaxCode,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? CreditTerms,
    bool IsActive
) : ICommand<SupplierDto>;

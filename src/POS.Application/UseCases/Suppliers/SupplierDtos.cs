namespace POS.Application.UseCases.Suppliers;

/// <summary>Shared DTO for Supplier — reused across Commands and Queries.</summary>
public record SupplierDto(
    Guid Id,
    string Name,
    string? TaxCode,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? CreditTerms,
    bool IsActive,
    DateTimeOffset CreatedAt
);

/// <summary>Shared DTO for SupplierPayment — reused across Commands and Queries.</summary>
public record SupplierPaymentDto(
    Guid Id,
    Guid SupplierId,
    Guid? VoucherId,
    decimal Amount,
    string Method,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset PaidAt
);

/// <summary>Extension to map domain entities to shared DTOs.</summary>
public static class SupplierDtoExtensions
{
    public static SupplierDto ToDto(this POS.Domain.Inventory.Suppliers.Supplier s) =>
        new(s.Id, s.Name, s.TaxCode, s.ContactName, s.Phone,
            s.Email, s.Address, s.CreditTerms, s.IsActive, s.CreatedAt);

    public static SupplierPaymentDto ToDto(this POS.Domain.Inventory.Suppliers.SupplierPayment p) =>
        new(p.Id, p.SupplierId, p.VoucherId, p.Amount,
            p.Method.ToString(), p.Note, p.CreatedBy, p.PaidAt);
}

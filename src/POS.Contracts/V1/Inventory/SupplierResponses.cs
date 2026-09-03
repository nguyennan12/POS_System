namespace POS.Contracts.V1.Inventory;

public record SupplierResponse(
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

public record SupplierPaymentResponse(
    Guid Id,
    Guid SupplierId,
    Guid? VoucherId,
    decimal Amount,
    string Method,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset PaidAt
);

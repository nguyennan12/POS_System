using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Inventory;

public record SupplierFilterRequest(
    string? Search = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record CreateSupplierRequest(
    string Name,
    string? TaxCode = null,
    string? ContactName = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? CreditTerms = null
);

public record UpdateSupplierRequest(
    string Name,
    string? TaxCode = null,
    string? ContactName = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? CreditTerms = null,
    bool IsActive = true
);

public record CreateSupplierPaymentRequest(
    decimal Amount,
    string Method,
    Guid? VoucherId = null,
    string? Note = null
);

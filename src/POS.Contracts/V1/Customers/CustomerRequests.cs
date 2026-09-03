using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Customers;

public record CustomerFilterRequest(
    string? Phone = null,
    string? Name = null,
    string? Barcode = null,
    Guid? MemberTierId = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record CreateCustomerRequest(
    string Name,
    string Phone,
    string? Email = null,
    DateOnly? Dob = null,
    string? Barcode = null
);

public record UpdateCustomerRequest(
    string Name,
    string Phone,
    string? Email = null,
    DateOnly? Dob = null,
    string? Barcode = null,
    bool IsActive = true
);

public record LoyaltyTransactionFilterRequest(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Type = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record AdjustPointsRequest(
    decimal Points,
    string Note
);

public record UpdateMemberTierRequest(
    decimal MinSpending,
    decimal PointRate,
    decimal DiscountRate,
    string? DisplayColor = null
);

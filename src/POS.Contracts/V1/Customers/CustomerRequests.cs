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

/// <summary>
/// Supplies a signed point adjustment, using Reason when Note is blank.
/// </summary>
public record AdjustPointsRequest(
    decimal Points,
    string? Note = null,
    string? Reason = null
)
{
    /// <summary>
    /// Gets the nonblank note, falling back to the reason or an empty string.
    /// </summary>
    public string EffectiveNote => !string.IsNullOrWhiteSpace(Note) ? Note : (Reason ?? string.Empty);
}

/// <summary>
/// Supplies a positive point amount to accrue with an optional order reference and note.
/// </summary>
public record AccruePointsRequest(
    decimal Points,
    Guid? OrderId = null,
    string? Note = null
);

/// <summary>
/// Supplies a positive point amount to redeem with an optional order reference and note.
/// </summary>
public record RedeemPointsRequest(
    decimal Points,
    Guid? OrderId = null,
    string? Note = null
);

public record UpdateMemberTierRequest(
    decimal MinSpending,
    decimal PointRate,
    decimal DiscountRate,
    string? DisplayColor = null
);

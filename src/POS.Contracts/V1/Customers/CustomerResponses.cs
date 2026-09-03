namespace POS.Contracts.V1.Customers;

public record MemberTierResponse(
    Guid Id,
    string Name,
    decimal MinSpending,
    decimal PointRate,
    decimal DiscountRate,
    string? DisplayColor
);

public record CustomerSummaryResponse(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string? Barcode,
    Guid MemberTierId,
    string MemberTierName,
    decimal TotalSpending,
    decimal PointsBalance,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record CustomerDetailResponse(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    DateOnly? Dob,
    string? Barcode,
    Guid MemberTierId,
    string MemberTierName,
    decimal TotalSpending,
    decimal PointsBalance,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record LoyaltyAccountResponse(
    Guid CustomerId,
    decimal PointsBalance,
    string TierName,
    decimal PointRate,
    decimal DiscountRate,
    DateTimeOffset LastUpdated
);

public record PointTransactionResponse(
    Guid Id,
    Guid CustomerId,
    decimal Points,
    string Type,
    Guid? OrderId,
    string? Note,
    DateTimeOffset CreatedAt
);

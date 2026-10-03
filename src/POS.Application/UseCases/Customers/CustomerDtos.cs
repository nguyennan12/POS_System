namespace POS.Application.UseCases.Customers;

public record CustomerDto(
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

public record MemberTierDto(
    Guid Id,
    string Name,
    decimal MinSpending,
    decimal PointRate,
    decimal DiscountRate,
    string? DisplayColor,
    decimal PointRedemptionRate = 1000m
);

public record PagedCustomerList(
    List<CustomerDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record LoyaltyAccountDto(
    Guid CustomerId,
    decimal PointsBalance,
    string TierName,
    decimal PointRate,
    decimal DiscountRate,
    DateTimeOffset LastUpdated,
    decimal PointRedemptionRate = 1000m,
    decimal AvailableBalanceInCurrency = 0m
);

public record PointTransactionDto(
    Guid Id,
    Guid CustomerId,
    decimal Points,
    string Type,
    Guid? OrderId,
    string? Note,
    DateTimeOffset CreatedAt
);

public record PagedPointTransactionList(
    List<PointTransactionDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

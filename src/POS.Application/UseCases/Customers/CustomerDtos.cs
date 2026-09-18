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
    string? DisplayColor
);

public record PagedCustomerList(
    List<CustomerDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

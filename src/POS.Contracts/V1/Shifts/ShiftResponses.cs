namespace POS.Contracts.V1.Shifts;

public record ShiftResponse(
    Guid Id,
    Guid StoreId,
    Guid EmployeeId,
    string EmployeeName,
    decimal OpeningCash,
    decimal? ClosingCash,
    decimal? ActualCash,
    string Status,
    string? Note,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt
);

public record ShiftSummaryResponse(
    Guid ShiftId,
    decimal OpeningCash,
    decimal TotalCashSales,
    decimal TotalCardSales,
    decimal TotalQrSales,
    decimal TotalRefunds,
    decimal ExpectedCash,
    decimal? ActualCash,
    decimal? Difference,
    int OrderCount,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt
);

using POS.Application.UseCases.Shifts.Commands.CloseShift;
using POS.Application.UseCases.Shifts.Commands.OpenShift;
using POS.Contracts.V1.Shifts;

namespace POS.Api.Mappings;

public static class ShiftMapping
{
    public static ShiftResponse ToResponse(this ShiftDto dto) =>
        new(
            dto.Id,
            dto.StoreId,
            dto.EmployeeId,
            dto.EmployeeName,
            dto.OpeningCash,
            dto.ClosingCash,
            dto.ActualCash,
            dto.Status,
            dto.Note,
            dto.OpenedAt,
            dto.ClosedAt);

    public static ShiftSummaryResponse ToResponse(this ShiftSummaryDto dto) =>
        new(
            dto.ShiftId,
            dto.OpeningCash,
            dto.TotalCashSales,
            dto.TotalCardSales,
            dto.TotalQrSales,
            dto.TotalRefunds,
            dto.ExpectedCash,
            dto.ActualCash,
            dto.Difference,
            dto.OrderCount,
            dto.OpenedAt,
            dto.ClosedAt);
}

using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Shifts.Commands.CloseShift;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Shifts.Queries.GetCurrentShift;

public class GetCurrentShiftQueryHandler(
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetCurrentShiftQuery, ShiftSummaryDto>
{
    public async Task<Result<ShiftSummaryDto>> Handle(
        GetCurrentShiftQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return new Error(ErrorType.Unauthorized, "Auth.Required", "Yêu cầu đăng nhập.");

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null)
            return new Error(ErrorType.NotFound, "Employee.NotFound", "Không tìm thấy nhân viên.");

        if (!employee.IsActive)
            return new Error(ErrorType.Forbidden, "Employee.Inactive", "Nhân viên đã bị khóa hoặc ngừng hoạt động.");

        if (!employee.IsChainOwner && employee.StoreId != query.StoreId)
            return new Error(ErrorType.Forbidden, "Employee.InvalidStore", "Nhân viên không thuộc cửa hàng này.");

        var shift = await shiftRepository.GetOpenShiftAsync(query.StoreId, cancellationToken);
        if (shift is null)
            return ShiftErrors.NotFound;

        var sales = await shiftRepository.GetShiftSalesAsync(shift.Id, cancellationToken);
        var expectedCash = shift.OpeningCash + sales.CashSales - sales.Refunds;

        return new ShiftSummaryDto(
            shift.Id,
            shift.OpeningCash,
            sales.CashSales,
            sales.CardSales,
            sales.QrSales,
            sales.Refunds,
            expectedCash,
            shift.ActualCash,
            shift.ActualCash.HasValue ? shift.ActualCash.Value - expectedCash : null,
            sales.OrderCount,
            new DateTimeOffset(shift.OpenedAt, TimeSpan.Zero),
            shift.ClosedAt.HasValue
                ? new DateTimeOffset(shift.ClosedAt.Value, TimeSpan.Zero)
                : null);
    }
}

using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Shifts.Commands.CloseShift;

public class CloseShiftCommandHandler(
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<CloseShiftCommand, ShiftSummaryDto>
{
    public async Task<Result<ShiftSummaryDto>> Handle(
        CloseShiftCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return new Error(ErrorType.Unauthorized, "Auth.Required", "Yêu cầu đăng nhập.");

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null)
            return new Error(ErrorType.NotFound, "Employee.NotFound", "Không tìm thấy nhân viên.");

        if (!employee.IsActive)
            return new Error(ErrorType.Forbidden, "Employee.Inactive", "Nhân viên đã bị khóa hoặc ngừng hoạt động.");

        var shift = await shiftRepository.GetByIdAsync(command.ShiftId, cancellationToken);
        if (shift is null)
            return ShiftErrors.NotFound;

        if (!employee.IsChainOwner && employee.StoreId != shift.StoreId)
            return new Error(ErrorType.Forbidden, "Employee.InvalidStore", "Nhân viên không thuộc cửa hàng này.");

        // Aggregate sales data from orders/payments in this shift
        var sales = await shiftRepository.GetShiftSalesAsync(shift.Id, cancellationToken);

        // ExpectedCash = what the register should contain
        var expectedCash = shift.OpeningCash + sales.CashSales - sales.Refunds;

        var closeResult = shift.Close(command.ActualCash, expectedCash, command.Note);
        if (closeResult.IsFailure)
            return closeResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var difference = command.ActualCash - expectedCash;

        return new ShiftSummaryDto(
            shift.Id,
            shift.OpeningCash,
            sales.CashSales,
            sales.CardSales,
            sales.QrSales,
            sales.Refunds,
            expectedCash,
            command.ActualCash,
            difference,
            sales.OrderCount,
            new DateTimeOffset(shift.OpenedAt, TimeSpan.Zero),
            shift.ClosedAt.HasValue
                ? new DateTimeOffset(shift.ClosedAt.Value, TimeSpan.Zero)
                : null);
    }
}

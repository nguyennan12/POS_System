using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Shifts.Commands.OpenShift;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Shifts.Queries.GetShiftById;

public class GetShiftByIdQueryHandler(
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetShiftByIdQuery, ShiftDto>
{
    public async Task<Result<ShiftDto>> Handle(
        GetShiftByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return new Error(ErrorType.Unauthorized, "Auth.Required", "Yêu cầu đăng nhập.");

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null)
            return new Error(ErrorType.NotFound, "Employee.NotFound", "Không tìm thấy nhân viên.");

        if (!employee.IsActive)
            return new Error(ErrorType.Forbidden, "Employee.Inactive", "Nhân viên đã bị khóa hoặc ngừng hoạt động.");

        var shift = await shiftRepository.GetByIdAsync(query.ShiftId, cancellationToken);
        if (shift is null)
            return ShiftErrors.NotFound;

        if (!employee.IsChainOwner && employee.StoreId != shift.StoreId)
            return new Error(ErrorType.Forbidden, "Employee.InvalidStore", "Nhân viên không thuộc cửa hàng này.");

        return new ShiftDto(
            shift.Id,
            shift.StoreId,
            shift.EmployeeId,
            shift.Employee.Name,
            shift.OpeningCash,
            shift.ClosingCash,
            shift.ActualCash,
            shift.Status.ToString(),
            shift.Note,
            new DateTimeOffset(shift.OpenedAt, TimeSpan.Zero),
            shift.ClosedAt.HasValue
                ? new DateTimeOffset(shift.ClosedAt.Value, TimeSpan.Zero)
                : null);
    }
}

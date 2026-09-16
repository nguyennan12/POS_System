using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Shifts.Commands.OpenShift;

public class OpenShiftCommandHandler(
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<OpenShiftCommand, ShiftDto>
{
    public async Task<Result<ShiftDto>> Handle(
        OpenShiftCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return new Error(ErrorType.Unauthorized, "Auth.Required", "Yêu cầu đăng nhập.");

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null)
            return new Error(ErrorType.NotFound, "Employee.NotFound", "Không tìm thấy nhân viên.");

        // Use serializable transaction to prevent race condition (two shifts opening simultaneously)
        return await unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            var existingShift = await shiftRepository.GetOpenShiftAsync(command.StoreId, ct);
            if (existingShift is not null)
                return Result<ShiftDto>.Failure(ShiftErrors.AlreadyOpen);

            var shift = Shift.Open(command.StoreId, employee.Id, command.OpeningCash, command.Note);
            await shiftRepository.AddAsync(shift, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<ShiftDto>.Success(new ShiftDto(
                shift.Id,
                shift.StoreId,
                shift.EmployeeId,
                employee.Name,
                shift.OpeningCash,
                shift.ClosingCash,
                shift.ActualCash,
                shift.Status.ToString(),
                shift.Note,
                new DateTimeOffset(shift.OpenedAt, TimeSpan.Zero),
                null));
        }, cancellationToken);
    }
}

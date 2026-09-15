using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Shifts.Commands.OpenShift;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Shifts.Queries.GetShiftById;

public class GetShiftByIdQueryHandler(IShiftRepository shiftRepository)
    : IQueryHandler<GetShiftByIdQuery, ShiftDto>
{
    public async Task<Result<ShiftDto>> Handle(
        GetShiftByIdQuery query,
        CancellationToken cancellationToken)
    {
        var shift = await shiftRepository.GetByIdAsync(query.ShiftId, cancellationToken);
        if (shift is null)
            return ShiftErrors.NotFound;

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

using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Shifts.Commands.CloseShift;

namespace POS.Application.UseCases.Shifts.Queries.GetCurrentShift;

public record GetCurrentShiftQuery(Guid StoreId) : IQuery<ShiftSummaryDto>;

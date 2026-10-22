using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Common;
using POS.Domain.Employees.Enums;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments.Queries.AuthorizePaymentSubscription;

public sealed class AuthorizePaymentSubscriptionQueryHandler(IPaymentRepository payments,
    IEmployeeRepository employees, IShiftRepository shifts, ICurrentUser currentUser,
    IPaymentSubscriptionAccessPolicy access, TimeProvider clock)
    : IQueryHandler<AuthorizePaymentSubscriptionQuery, PaymentStatusDto>
{
    public async Task<Result<PaymentStatusDto>> Handle(AuthorizePaymentSubscriptionQuery query, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.EmployeeId is not Guid employeeId)
            return PaymentErrors.Unauthorized;
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null || !employee.IsActive || employee.IsLocked(clock.GetUtcNow().UtcDateTime))
            return PaymentErrors.Unauthorized;

        // This repository read is AsNoTracking, including the second snapshot after joining the group.
        var payment = await payments.GetByIdWithOrderAsync(query.PaymentId, ct);
        if (payment is null) return PaymentErrors.PaymentNotFound;
        if (employee.StoreId != payment.Order.StoreId) return PaymentErrors.InvalidStore;

        var shift = await shifts.GetByIdAsync(payment.Order.ShiftId, ct);
        if (shift is null || !await access.CanAccessShiftAsync(employee, payment.Order, shift, ct))
            return PaymentErrors.InvalidShift;
        if (payment.Status == PaymentStatus.Pending && shift.Status != ShiftStatus.Open)
            return PaymentErrors.ShiftClosed;

        // A subscription reads persisted state; it never expires or settles a payment.
        return PaymentStatusDto.From(payment);
    }
}

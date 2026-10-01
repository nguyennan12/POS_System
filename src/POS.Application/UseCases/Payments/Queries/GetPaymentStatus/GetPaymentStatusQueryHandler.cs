using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments.Errors;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;

namespace POS.Application.UseCases.Payments.Queries.GetPaymentStatus;

public class GetPaymentStatusQueryHandler(
    IPaymentRepository paymentRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IQueryHandler<GetPaymentStatusQuery, PaymentStatusResponse>
{
    public async Task<Result<PaymentStatusResponse>> Handle(
        GetPaymentStatusQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return PaymentErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return PaymentErrors.Unauthorized;

        var payment = await paymentRepository.GetByIdWithOrderAsync(query.Id, cancellationToken);
        if (payment is null)
            return PaymentErrors.PaymentNotFound;

        if (!employee.IsChainOwner && employee.StoreId != payment.Order.StoreId)
        {
            return PaymentErrors.InvalidStore;
        }

        return new PaymentStatusResponse(
            payment.Id, payment.OrderId, payment.Method.ToString(), payment.Amount,
            payment.Status.ToString(), payment.TransactionRef, payment.PaidAt);
    }
}

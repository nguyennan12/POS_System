using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments.Errors;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Commands.ExpirePendingPayment;
using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Payments.Queries.GetPaymentStatus;

public class GetPaymentStatusQueryHandler(
    IPaymentRepository paymentRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser,
    ISender sender) : IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    public async Task<Result<PaymentStatusDto>> Handle(
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

        if (payment.Status == POS.Domain.Orders.Enums.PaymentStatus.Pending && QrSessionDto.Read(payment) is not null)
            return await sender.Send(new ExpirePendingPaymentCommand(payment.Id), cancellationToken);
        return PaymentStatusDto.From(payment);
    }
}

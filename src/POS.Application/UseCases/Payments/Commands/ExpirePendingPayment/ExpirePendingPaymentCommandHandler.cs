using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Common;
using POS.Domain.Auditing;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments.Commands.ExpirePendingPayment;

public sealed class ExpirePendingPaymentCommandHandler(IPaymentRepository payments, IStoreAccessPolicy access,
    IUnitOfWork uow, TimeProvider clock, IAuditLogRepository audit) : ICommandHandler<ExpirePendingPaymentCommand, PaymentStatusDto>
{
    public Task<Result<PaymentStatusDto>> Handle(ExpirePendingPaymentCommand command, CancellationToken cancellationToken) =>
        PaymentTransactions.ExecuteAsync<PaymentStatusDto>(uow, async ct =>
        {
            var payment = await payments.GetTrackedAsync(command.Id, ct);
            if (payment is null) return PaymentErrors.PaymentNotFound;
            if (!access.CanAccessStore(payment.Order.StoreId)) return PaymentErrors.InvalidStore;
            if (payment.Status == PaymentStatus.Pending && QrSessionDto.Read(payment) is { } session
                && session.ExpiresAt <= clock.GetUtcNow())
            {
                payment.ExpireQr();
                await audit.AddAsync(AuditLog.SystemPayment(payment.Id, payment.Order.StoreId, "QrPaymentTimeout"), ct);
                await uow.SaveChangesAsync(ct);
            }
            return PaymentStatusDto.From(payment);
        }, cancellationToken);
}

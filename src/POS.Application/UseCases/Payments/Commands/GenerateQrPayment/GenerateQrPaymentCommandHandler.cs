using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Payments;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Common;
using POS.Domain.Auditing;
using POS.Domain.Employees.Enums;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments.Commands.GenerateQrPayment;

public sealed class GenerateQrPaymentCommandHandler(IOrderRepository orders, IPaymentRepository payments,
    IShiftRepository shifts, IStoreAccessPolicy access, IUnitOfWork uow, IEnumerable<IPaymentGateway> gateways,
    QrPaymentOptions options, TimeProvider clock, IAuditLogRepository audit, ICurrentUser currentUser)
    : ICommandHandler<GenerateQrPaymentCommand, QrPaymentDto>
{
    public async Task<Result<QrPaymentDto>> Handle(GenerateQrPaymentCommand command, CancellationToken cancellationToken)
    {
        if (command.Method is not ("MoMo" or "VietQR")) return PaymentErrors.InvalidMethod;
        var method = Enum.Parse<PaymentMethod>(command.Method);
        var gateway = gateways.SingleOrDefault(x => x.Method == method);
        if (gateway is null) return PaymentErrors.GatewayUnavailable;

        // Reserve once before network I/O. Retrying an uncertain provider request reuses both merchant IDs.
        var reservation = await PaymentTransactions.ExecuteAsync<Payment>(uow, async ct =>
        {
            var order = await orders.GetByIdWithDetailsAsync(command.OrderId, ct);
            if (order is null) return PaymentErrors.InvalidOrder;
            if (!access.CanAccessStore(order.StoreId)) return PaymentErrors.InvalidStore;
            if (order.Status != OrderStatus.Confirmed) return PaymentErrors.InvalidOrder;
            if (order.CurrencyCode != "VND") return PaymentErrors.UnsupportedCurrency;
            var shift = await shifts.GetByIdAsync(order.ShiftId, ct);
            if (shift is null || shift.Status != ShiftStatus.Open || shift.StoreId != order.StoreId)
                return PaymentErrors.ShiftClosed;
            var now = clock.GetUtcNow();
            var expired = false;
            foreach (var pending in order.Payments.Where(x => x.Method == method && x.Status == PaymentStatus.Pending
                         && x.MerchantReference != null))
            {
                var stored = QrSessionDto.Read(pending);
                if (stored is null) return PaymentErrors.GatewayUnavailable;
                if (stored.ExpiresAt > now) return pending;
                pending.ExpireQr();
                expired = true;
                await audit.AddAsync(AuditLog.SystemPayment(pending.Id, order.StoreId, "QrPaymentTimeout"), ct);
            }
            // Release the filtered unique index before inserting the replacement, in this same transaction.
            if (expired) await uow.SaveChangesAsync(ct);
            var amount = order.GrandTotal - order.GetPaymentTotals().TotalApplied;
            if (amount <= 0 || amount != decimal.Truncate(amount) || amount > 9999999999999999m)
                return PaymentErrors.InvalidAmount;
            var reference = "POS" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
            var session = new QrSessionDto("", null, now.AddMinutes(options.LifetimeMinutes));
            var payment = Payment.CreatePendingQr(order.Id, method, amount, reference, session.ToJson());
            await orders.AddPaymentsAsync([payment], ct);
            await audit.AddAsync(AuditLog.QrPaymentRequested(payment.Id, order.StoreId, currentUser.EmployeeId), ct);
            await uow.SaveChangesAsync(ct);
            return payment;
        }, cancellationToken);
        if (reservation.IsFailure) return reservation.Error;
        var reserved = reservation.Value!;
        var metadata = QrSessionDto.Read(reserved)!;
        if (!string.IsNullOrEmpty(metadata.QrCodeData)) return metadata.ToDto(reserved);

        var generated = await gateway.GenerateAsync(reserved, metadata, cancellationToken);
        if (generated.IsFailure) return generated.Error; // Keep Pending after network uncertainty.

        return await PaymentTransactions.ExecuteAsync<QrPaymentDto>(uow, async ct =>
        {
            var payment = await payments.GetTrackedAsync(reserved.Id, ct);
            if (payment is null) return PaymentErrors.PaymentNotFound;
            var stored = QrSessionDto.Read(payment);
            if (stored is not null && !string.IsNullOrEmpty(stored.QrCodeData)) return stored.ToDto(payment);
            if (payment.Status != PaymentStatus.Pending || metadata.ExpiresAt <= clock.GetUtcNow())
                return PaymentErrors.GatewayUnavailable;
            var set = payment.SetQrMetadata(generated.Value!.ToJson());
            if (set.IsFailure) return set.Error;
            await uow.SaveChangesAsync(ct);
            return generated.Value.ToDto(payment);
        }, cancellationToken);
    }
}

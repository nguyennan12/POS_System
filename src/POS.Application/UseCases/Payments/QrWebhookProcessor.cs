using MediatR;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Notifications;
using POS.Application.Abstractions.Payments;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Auditing;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;

namespace POS.Application.UseCases.Payments;

public sealed class QrWebhookProcessor(IEnumerable<IPaymentGateway> gateways, IPaymentRepository payments,
    IAuditLogRepository audit, IUnitOfWork uow, IPublisher domainEvents,
    IRealtimeEventPublisher realtime, TimeProvider clock, ILogger<QrWebhookProcessor> logger)
{
    public async Task<Result<WebhookAckDto>> ProcessAsync(PaymentMethod method, WebhookEnvelopeDto envelope,
        CancellationToken cancellationToken)
    {
        var gateway = gateways.SingleOrDefault(x => x.Method == method);
        if (gateway is null) return PaymentErrors.InvalidWebhook;
        var verified = gateway.VerifyWebhook(envelope);
        if (verified.IsFailure) return PaymentErrors.InvalidWebhook;
        var message = verified.Value!;

        var result = await PaymentTransactions.ExecuteAsync<PaymentRealtimeDto?>(uow, async ct =>
        {
            var payment = await payments.GetForWebhookAsync(method, message.MerchantReference, ct);
            if (payment is null || payment.Method != method || payment.Order is null
                || payment.Amount != message.Amount || (message.OrderId.HasValue && message.OrderId != payment.OrderId))
                return PaymentErrors.InvalidWebhook;

            var target = message.Success ? PaymentStatus.Success : PaymentStatus.Failed;
            if (message.IsFinal && payment.Status == target && payment.TransactionRef == message.TransactionReference)
                return Result<PaymentRealtimeDto?>.Success(null); // Verified replay, including older retries.

            var now = clock.GetUtcNow();
            if (payment.Status != PaymentStatus.Pending || payment.Order.Status != OrderStatus.Confirmed
                || string.IsNullOrWhiteSpace(message.TransactionReference) || message.TransactionReference.Length > 100
                || (message.TimestampTolerance is { } tolerance && (now - message.OccurredAt).Duration() > tolerance)
                || QrSessionDto.Read(payment) is not { } session || session.ExpiresAt <= now)
                return PaymentErrors.InvalidWebhook;
            if (await payments.TransactionExistsAsync(method, message.TransactionReference, payment.Id, ct))
                return PaymentErrors.InvalidWebhook;
            if (!message.IsFinal) return Result<PaymentRealtimeDto?>.Success(null);
            if (message.Success && payment.Order.GetPaymentTotals().TotalApplied + payment.Amount > payment.Order.GrandTotal)
                return PaymentErrors.InvalidWebhook;

            var complete = payment.CompleteQr(message.TransactionReference, message.Success, message.OccurredAt.UtcDateTime);
            if (complete.IsFailure) return PaymentErrors.InvalidWebhook;
            // T45 owns settlement arithmetic. Never create a second Success payment via checkout.
            payment.Order.AggregatePaymentStatus();
            if (payment.Order.Status == OrderStatus.Paid)
                await domainEvents.Publish(new OrderPaidDomainEvent(payment.OrderId), ct);
            await audit.AddAsync(AuditLog.SystemPayment(payment.Id, payment.Order.StoreId,
                message.Success ? "QrPaymentSuccess" : "QrPaymentFailed"), ct);
            await uow.SaveChangesAsync(ct);
            return new PaymentRealtimeDto(payment.Order.StoreId, PaymentStatusDto.From(payment));
        }, cancellationToken);

        if (result.IsFailure) return PaymentErrors.InvalidWebhook;
        // ExecuteSerializableAsync returns only after COMMIT. Publication cannot undo settlement.
        if (result.Value is { } notification)
        {
            try { await realtime.PublishPaymentAsync(notification, CancellationToken.None); }
            catch (Exception)
            {
                logger.LogWarning("Realtime publication failed for committed payment {PaymentId}", notification.Payment.PaymentId);
            }
        }
        return new WebhookAckDto();
    }
}

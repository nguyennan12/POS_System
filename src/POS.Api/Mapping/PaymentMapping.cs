using POS.Application.UseCases.Payments.Commands.GenerateQrPayment;
using POS.Application.UseCases.Payments.Commands.ProcessMomoWebhook;
using POS.Application.UseCases.Payments.Commands.ProcessVietQrWebhook;
using POS.Application.UseCases.Payments.DTOs;
using POS.Contracts.V1.Payments;

namespace POS.Api.Mappings;

public static class PaymentMapping
{
    public static GenerateQrPaymentCommand ToCommand(this GenerateQrPaymentRequest request) => new(request.OrderId, request.Method);

    public static ProcessMomoWebhookCommand ToCommand(this MomoWebhookRequest r) => new(new(
        r.PartnerCode, r.OrderId, r.RequestId, r.Amount, r.OrderInfo, r.OrderType, r.TransId,
        r.ResultCode, r.Message, r.PayType, r.ResponseTime, r.ExtraData, r.Signature));

    public static ProcessVietQrWebhookCommand ToCommand(this VietQrWebhookRequest r, byte[] rawBody, string? signature) =>
        new(new(r.Gateway, r.TransactionDate, r.AccountNumber, r.SubAccount, r.Amount, r.Content,
            r.TransferType, r.Description, r.TransactionId, r.ReferenceCode), rawBody, signature);

    public static QrPaymentResponse ToResponse(this QrPaymentDto dto) => new(dto.PaymentId, dto.OrderId,
        dto.Method, dto.Amount, dto.QrCodeData, dto.DeepLink, dto.ExpiresAt);

    public static PaymentStatusResponse ToResponse(this PaymentStatusDto dto) => new(dto.PaymentId,
        dto.OrderId, dto.Method, dto.Amount, dto.Status, dto.TransactionRef, dto.PaidAt);
}

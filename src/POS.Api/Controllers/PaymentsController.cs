using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Payments.Queries.GetPaymentStatus;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Payments;
using POS.Api.Mappings;
using POS.Api.Filters;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/payments")]
public class PaymentsController(ISender mediator) : ControllerBase
{
    [HttpPost("qr/generate")]
    public async Task<ActionResult<ApiResponse<QrPaymentResponse>>> GenerateQr(
        [FromBody] GenerateQrPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request.ToCommand(), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<QrPaymentResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<PaymentStatusResponse>>> GetPaymentStatus(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPaymentStatusQuery(id), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<PaymentStatusResponse>.Ok(result.Value!.ToResponse()));
    }

    [AllowAnonymous]
    [PaymentWebhook]
    [RequestSizeLimit(64 * 1024)]
    [HttpPost("webhook/momo")]
    public async Task<IActionResult> MomoWebhook([FromBody] MomoWebhookRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request.ToCommand(), cancellationToken);
        // MoMo v2 IPN requires HTTP 204 without a response body, including verified duplicate delivery.
        return result.IsSuccess ? NoContent()
            : BadRequest(new MomoWebhookResponse(1, "Không thể xác nhận thanh toán."));
    }

    [AllowAnonymous]
    [PaymentWebhook]
    [RequestSizeLimit(64 * 1024)]
    [HttpPost("webhook/vietqr")]
    public async Task<IActionResult> VietQrWebhook([FromBody] VietQrWebhookRequest request, CancellationToken cancellationToken)
    {
        var rawBody = HttpContext.Items[PaymentWebhookAttribute.RawBodyKey] as byte[] ?? [];
        var signature = Request.Headers["X-VietQR-Signature"].ToString();
        var result = await mediator.Send(request.ToCommand(rawBody, signature), cancellationToken);
        return result.IsSuccess ? Ok(new VietQrWebhookResponse(true, "OK"))
            : BadRequest(new VietQrWebhookResponse(false, "Không thể xác nhận thanh toán."));
    }
}

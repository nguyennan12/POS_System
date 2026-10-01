using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Payments.Queries.GetPaymentStatus;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Payments;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/payments")]
public class PaymentsController(ISender mediator) : ControllerBase
{
    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<PaymentStatusResponse>>> GetPaymentStatus(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPaymentStatusQuery(id), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<PaymentStatusResponse>.Ok(result.Value!));
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Invoices.Queries.GetInvoiceById;
using POS.Application.UseCases.Invoices.Queries.GetInvoices;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/invoices")]
public class InvoicesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<InvoiceSummaryResponse>>>> GetInvoices(
        [FromQuery] InvoiceFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvoicesQuery(filter), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<PagedResponse<InvoiceSummaryResponse>>.Ok(result.Value!));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceDetailResponse>>> GetInvoiceById(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<InvoiceDetailResponse>.Ok(result.Value!));
    }
}

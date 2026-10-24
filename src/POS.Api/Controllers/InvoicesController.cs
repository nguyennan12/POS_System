using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Invoices.Queries.GetInvoiceById;
using POS.Application.UseCases.Invoices.Queries.GetInvoices;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;
using POS.Api.Mappings;
using POS.Contracts.V1.Orders;
using POS.Application.UseCases.Invoices.Queries.GetInvoicePdf;
using POS.Application.UseCases.Invoices.Queries.GetReceiptData;
using POS.Application.UseCases.Invoices.Queries.GetEscPosReceipt;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/invoices")]
public class InvoicesController(ISender mediator) : ControllerBase
{
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid id, [FromQuery] bool reprint = false,
        [FromQuery] bool download = false, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetInvoicePdfQuery(id, reprint, download), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        Response.Headers.CacheControl = "private, no-store";
        var pdf = result.Value!;
        if (download) return File(pdf.FileBytes!, "application/pdf", pdf.InvoiceNo + ".pdf");
        var url = Url.Action(nameof(GetInvoicePdf), new { id, download = true, reprint })!;
        return Ok(ApiResponse<InvoicePdfResponse>.Ok(pdf.ToResponse(url)));
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<ActionResult<ApiResponse<ReceiptDataResponse>>> GetReceiptData(Guid id,
        [FromQuery] int? paperWidth = null, [FromQuery] bool reprint = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetReceiptDataQuery(id, paperWidth, reprint), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        Response.Headers.CacheControl = "private, no-store";
        return Ok(ApiResponse<ReceiptDataResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpGet("{id:guid}/receipt/escpos")]
    public async Task<IActionResult> GetEscPosReceipt(Guid id, [FromQuery] int? paperWidth = null,
        [FromQuery] bool reprint = false, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetEscPosReceiptQuery(id, paperWidth, reprint), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        Response.Headers.CacheControl = "private, no-store";
        return File(result.Value!, "application/octet-stream", id + ".escpos");
    }

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

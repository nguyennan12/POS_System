using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Suppliers.Commands.CreateSupplier;
using POS.Application.UseCases.Suppliers.Commands.CreateSupplierPayment;
using POS.Application.UseCases.Suppliers.Commands.DeleteSupplier;
using POS.Application.UseCases.Suppliers.Commands.UpdateSupplier;
using POS.Application.UseCases.Suppliers.Queries.GetSupplierById;
using POS.Application.UseCases.Suppliers.Queries.GetSupplierPayments;
using POS.Application.UseCases.Suppliers.Queries.GetSuppliers;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Inventory;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/v1/suppliers")]
public class SuppliersController(ISender mediator) : ControllerBase
{
    // ── GET api/v1/suppliers ──────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<SupplierResponse>>>> GetAll(
        [FromQuery] SupplierFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetSuppliersQuery(
            request.Search,
            request.IsActive,
            request.PageNumber,
            request.PageSize);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        var paged = result.Value!;

        return Ok(ApiResponse<PagedResponse<SupplierResponse>>.Ok(
            new PagedResponse<SupplierResponse>(
                paged.Items.Select(s => s.ToResponse()).ToList().AsReadOnly(),
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount)));
    }

    // ── GET api/v1/suppliers/{id} ─────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSupplierByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<SupplierResponse>.Ok(result.Value!.ToResponse()));
    }

    // ── POST api/v1/suppliers ─────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSupplierCommand(
            request.Name,
            request.TaxCode,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address,
            request.CreditTerms);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<SupplierResponse>.Ok(result.Value!.ToResponse()));
    }

    // ── PUT api/v1/suppliers/{id} ─────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> Update(
        Guid id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSupplierCommand(
            id,
            request.Name,
            request.TaxCode,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address,
            request.CreditTerms,
            request.IsActive);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<SupplierResponse>.Ok(result.Value!.ToResponse()));
    }

    // ── DELETE api/v1/suppliers/{id} ──────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteSupplierCommand(id), cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return NoContent();
    }

    // ── GET api/v1/suppliers/{id}/payments ────────────────────────────────
    [HttpGet("{id:guid}/payments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplierPaymentResponse>>>> GetPayments(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSupplierPaymentsQuery(id), cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<IReadOnlyList<SupplierPaymentResponse>>.Ok(
            result.Value!.Select(p => p.ToResponse()).ToList().AsReadOnly()));
    }

    // ── POST api/v1/suppliers/{id}/payments ───────────────────────────────
    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<ApiResponse<SupplierPaymentResponse>>> CreatePayment(
        Guid id,
        [FromBody] CreateSupplierPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSupplierPaymentCommand(
            id,
            request.Amount,
            request.Method,
            request.VoucherId,
            request.Note);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return CreatedAtAction(
            nameof(GetPayments),
            new { id },
            ApiResponse<SupplierPaymentResponse>.Ok(result.Value!.ToResponse()));
    }
}

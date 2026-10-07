using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Promotions.Commands.DeleteVoucher;
using POS.Application.UseCases.Promotions.Commands.UpdateVoucher;
using POS.Application.UseCases.Promotions.Queries.GetVoucherById;
using POS.Application.UseCases.Promotions.Queries.GetVouchers;
using POS.Application.UseCases.Promotions.Queries.ValidateVoucher;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Promotions;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/vouchers")]
public class VouchersController(ISender mediator, POS.Application.Abstractions.Auth.ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<VoucherResponse>>>> GetPaged(
        [FromQuery] VoucherFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetVouchersQuery(
            Code: request.Code,
            IsActive: request.IsActive,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize);

        var result = await mediator.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        var paged = result.Value!;
        var response = new PagedResponse<VoucherResponse>(
            paged.Items.Select(x => x.ToResponse()).ToList().AsReadOnly(),
            paged.PageNumber,
            paged.PageSize,
            paged.TotalCount);

        return Ok(ApiResponse<PagedResponse<VoucherResponse>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VoucherResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetVoucherByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<VoucherResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VoucherResponse>>> Update(
        Guid id,
        [FromBody] UpdateVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVoucherCommand(
            Id: id,
            MaxUses: request.MaxUses,
            PerCustomerLimit: request.PerCustomerLimit,
            ExpiresAt: request.ExpiresAt,
            IsActive: request.IsActive);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<VoucherResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteVoucherCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<bool>.Ok(result.Value));
    }

    [HttpGet("{code}/validate")]
    public async Task<ActionResult<ApiResponse<ValidateVoucherResponse>>> ValidateGet(
        string code,
        [FromQuery] ValidateVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var query = new ValidateVoucherQuery(
            Code: code,
            OrderSubtotal: request.OrderSubtotal,
            CustomerId: request.CustomerId,
            StoreId: currentUser.StoreId);

        var result = await mediator.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<ValidateVoucherResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost("{code}/validate")]
    public async Task<ActionResult<ApiResponse<ValidateVoucherResponse>>> ValidatePost(
        string code,
        [FromBody] ValidateVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var query = new ValidateVoucherQuery(
            Code: code,
            OrderSubtotal: request.OrderSubtotal,
            CustomerId: request.CustomerId,
            StoreId: currentUser.StoreId);

        var result = await mediator.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<ValidateVoucherResponse>.Ok(result.Value!.ToResponse()));
    }
}

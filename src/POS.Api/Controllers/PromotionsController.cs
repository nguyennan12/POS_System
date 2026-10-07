using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Promotions.Commands.CreatePromotion;
using POS.Application.UseCases.Promotions.Commands.CreateVoucher;
using POS.Application.UseCases.Promotions.Commands.DeletePromotion;
using POS.Application.UseCases.Promotions.Commands.UpdatePromotion;
using POS.Application.UseCases.Promotions.Queries.GetPromotionById;
using POS.Application.UseCases.Promotions.Queries.GetPromotions;
using POS.Application.UseCases.Promotions.Queries.GetVouchers;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Promotions;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/promotions")]
public class PromotionsController(ISender mediator, POS.Application.Abstractions.Auth.ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<PromotionSummaryResponse>>>> GetPaged(
        [FromQuery] PromotionFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetPromotionsQuery(
            request.StoreId,
            request.Status,
            request.Type,
            request.ActiveAt,
            request.PageNumber,
            request.PageSize);

        var result = await mediator.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        var paged = result.Value!;
        var response = new PagedResponse<PromotionSummaryResponse>(
            paged.Items.Select(x => x.ToSummaryResponse()).ToList().AsReadOnly(),
            paged.PageNumber,
            paged.PageSize,
            paged.TotalCount);

        return Ok(ApiResponse<PagedResponse<PromotionSummaryResponse>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PromotionDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPromotionByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<PromotionDetailResponse>.Ok(result.Value!.ToDetailResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PromotionDetailResponse>>> Create(
        [FromBody] CreatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePromotionCommand(
            StoreId: null,
            Name: request.Name,
            Type: request.Type,
            Value: request.Value,
            MinOrderAmount: request.MinOrderAmount,
            MaxDiscountAmount: request.MaxDiscountAmount,
            ConditionsJson: request.ConditionsJson,
            Priority: request.Priority,
            IsStackable: request.IsStackable,
            IsExclusive: request.IsExclusive,
            AppliesTo: request.AppliesTo,
            ValidFrom: request.ValidFrom,
            ValidTo: request.ValidTo,
            TargetCategoryIds: request.TargetCategoryIds,
            TargetSkuIds: request.TargetSkuIds,
            CreatedBy: currentUser.EmployeeId);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        var response = result.Value!.ToDetailResponse();
        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            ApiResponse<PromotionDetailResponse>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PromotionDetailResponse>>> Update(
        Guid id,
        [FromBody] UpdatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePromotionCommand(
            Id: id,
            Name: request.Name,
            Type: request.Type,
            Value: request.Value,
            MinOrderAmount: request.MinOrderAmount,
            MaxDiscountAmount: request.MaxDiscountAmount,
            ConditionsJson: request.ConditionsJson,
            Priority: request.Priority,
            IsStackable: request.IsStackable,
            IsExclusive: request.IsExclusive,
            AppliesTo: request.AppliesTo,
            ValidFrom: request.ValidFrom,
            ValidTo: request.ValidTo,
            Status: request.Status,
            TargetCategoryIds: request.TargetCategoryIds,
            TargetSkuIds: request.TargetSkuIds);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<PromotionDetailResponse>.Ok(result.Value!.ToDetailResponse()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeletePromotionCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<bool>.Ok(result.Value));
    }

    [HttpGet("{id:guid}/vouchers")]
    public async Task<ActionResult<ApiResponse<PagedResponse<VoucherResponse>>>> GetVouchersByPromotion(
        Guid id,
        [FromQuery] VoucherFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetVouchersQuery(
            Code: request.Code,
            IsActive: request.IsActive,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            PromotionId: id);

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

    [HttpPost("{id:guid}/vouchers")]
    public async Task<ActionResult<ApiResponse<VoucherResponse>>> CreateVoucherForPromotion(
        Guid id,
        [FromBody] CreateVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateVoucherCommand(
            PromotionId: id,
            Code: request.Code,
            MaxUses: request.MaxUses,
            PerCustomerLimit: request.PerCustomerLimit,
            ExpiresAt: request.ExpiresAt);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<VoucherResponse>.Ok(result.Value!.ToResponse()));
    }
}

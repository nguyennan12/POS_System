using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Customers.Commands.UpdateMemberTier;
using POS.Application.UseCases.Customers.Queries.GetMemberTiers;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Customers;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/member-tiers")]
public class MemberTiersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MemberTierResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMemberTiersQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<List<MemberTierResponse>>.Ok(result.Value!.Select(t => t.ToResponse()).ToList()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MemberTierResponse>>> Update(
        Guid id,
        [FromBody] UpdateMemberTierRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMemberTierCommand(
            id,
            request.MinSpending,
            request.PointRate,
            request.DiscountRate,
            request.DisplayColor);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<MemberTierResponse>.Ok(result.Value!.ToResponse()));
    }
}

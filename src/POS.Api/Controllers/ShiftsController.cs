using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Shifts.Commands.CloseShift;
using POS.Application.UseCases.Shifts.Commands.OpenShift;
using POS.Application.UseCases.Shifts.Queries.GetCurrentShift;
using POS.Application.UseCases.Shifts.Queries.GetShiftById;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Shifts;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/shifts")]
public class ShiftsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Open a new shift for a store.
    /// </summary>
    [HttpPost("open")]
    public async Task<ActionResult<ApiResponse<ShiftResponse>>> Open(
        [FromBody] OpenShiftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new OpenShiftCommand(request.StoreId, request.OpeningCash, request.Note),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<ShiftResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// Close an open shift with actual cash count.
    /// </summary>
    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<ShiftSummaryResponse>>> Close(
        Guid id,
        [FromBody] CloseShiftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CloseShiftCommand(id, request.ActualCash, request.Note),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<ShiftSummaryResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// Get a shift by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShiftResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetShiftByIdQuery(id),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<ShiftResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// Get the current open shift for a store (with live sales summary).
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<ShiftSummaryResponse>>> GetCurrent(
        [FromQuery] Guid storeId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetCurrentShiftQuery(storeId),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<ShiftSummaryResponse>.Ok(result.Value!.ToResponse()));
    }
}

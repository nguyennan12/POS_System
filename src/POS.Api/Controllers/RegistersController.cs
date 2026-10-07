using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Registers.Commands.CreateRegister;
using POS.Application.UseCases.Registers.Queries.GetRegistersByStore;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Registers;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/stores/{storeId:guid}/registers")]
public class RegistersController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Get all registers for a store.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RegisterResponse>>>> GetByStore(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRegistersByStoreQuery(storeId), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<IReadOnlyList<RegisterResponse>>.Ok(result.Value!));
    }

    /// <summary>
    /// Create a new register in a store.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Create(
        Guid storeId,
        [FromBody] CreateRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateRegisterCommand(storeId, request.Name, request.Code), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<RegisterResponse>.Ok(result.Value!));
    }
}

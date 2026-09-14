using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Stores.Commands.CreateStore;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Stores;
using POS.Api.Mappings;
using Microsoft.AspNetCore.Authorization;
using POS.Application.UseCases.Stores.Queries.GetAllStores;
using POS.Application.UseCases.Stores.Commands.UpdateStore;
using POS.Application.UseCases.Stores.Commands.UpdateStoreStatus;
using POS.Application.UseCases.Stores.Commands.AssignAdminToStore;
using POS.Application.UseCases.Stores.Commands.GrantOwnerAccess;

namespace POS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/stores")]
public class StoresController(ISender mediator) : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<ApiResponse<List<StoreResponse>>>> GetAll(CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetAllStoresQuery(), cancellationToken);
    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<List<StoreResponse>>.Ok(result.Value!.Select(s => s.ToResponse()).ToList()));
  }

  [HttpPut("{id:guid}")]
  public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> Update(
      Guid id, [FromBody] UpdateStoreRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new UpdateStoreCommand(id, request.Name, request.Address,
        request.Phone, request.Timezone, request.CurrencyCode, request.TaxCode,
        request.ReceiptHeader, request.ReceiptFooter), cancellationToken);
    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<StoreDetailResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPut("{id:guid}/status")]
  public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> UpdateStatus(
      Guid id, [FromBody] UpdateStoreStatusRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new UpdateStoreStatusCommand(id, request.IsActive), cancellationToken);
    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<StoreDetailResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPost("{id:guid}/assign-admin")]
  public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> AssignAdmin(
      Guid id, [FromBody] StoreAdminAssignmentRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new AssignAdminToStoreCommand(id, request.EmployeeId), cancellationToken);
    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<StoreDetailResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPost("{id:guid}/grant-owner-access")]
  public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> GrantOwnerAccess(
      Guid id, [FromBody] StoreOwnerAccessRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GrantOwnerAccessCommand(id, request.EmployeeId), cancellationToken);
    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<StoreDetailResponse>.Ok(result.Value!.ToResponse()));

  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> GetById(
      Guid id,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetStoreDetailQuery(id), cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(
    ApiResponse<StoreDetailResponse>.Ok(
        result.Value!.ToResponse()));
  }

  [HttpPost]
  public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> Create(
      [FromBody] CreateStoreRequest request,
      CancellationToken cancellationToken)
  {
    var command = new CreateStoreCommand(
        request.Name,
        request.Address,
        request.Phone,
        request.Timezone,
        request.CurrencyCode);

    var result = await mediator.Send(command, cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return CreatedAtAction(
     nameof(GetById),
     new { id = result.Value!.Id },
     ApiResponse<StoreDetailResponse>.Ok(
         result.Value!.ToResponse()));
  }
}

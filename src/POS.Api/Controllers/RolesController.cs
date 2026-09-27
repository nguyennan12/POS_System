using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Rbac.Commands.CreateRole;
using POS.Application.UseCases.Rbac.Commands.UpdateRole;
using POS.Application.UseCases.Rbac.Commands.UpdateRolePermissions;
using POS.Application.UseCases.Rbac.Queries.GetPermissions;
using POS.Application.UseCases.Rbac.Queries.GetResources;
using POS.Application.UseCases.Rbac.Queries.GetRoleById;
using POS.Application.UseCases.Rbac.Queries.GetRolePermissions;
using POS.Application.UseCases.Rbac.Queries.GetRoles;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Rbac;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/roles")]
public class RolesController(ISender mediator) : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<ApiResponse<List<RoleResponse>>>> GetRoles(
      [FromQuery] Guid? storeId,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetRolesQuery(storeId), cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<List<RoleResponse>>.Ok(result.Value!.Select(r => r.ToResponse()).ToList()));
  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ApiResponse<RoleDetailResponse>>> GetById(
      Guid id,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetRoleByIdQuery(id), cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<RoleDetailResponse>.Ok(result.Value!.ToDetailResponse()));
  }

  [HttpPost]
  public async Task<ActionResult<ApiResponse<RoleResponse>>> Create(
      [FromBody] CreateRoleRequest request,
      CancellationToken cancellationToken)
  {
    var command = new CreateRoleCommand(request.Name, request.Description, request.StoreId);
    var result = await mediator.Send(command, cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return CreatedAtAction(
        nameof(GetById),
        new { id = result.Value!.Id },
        ApiResponse<RoleResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPut("{id:guid}")]
  public async Task<ActionResult<ApiResponse<RoleResponse>>> Update(
      Guid id,
      [FromBody] UpdateRoleRequest request,
      CancellationToken cancellationToken)
  {
    var command = new UpdateRoleCommand(id, request.Name, request.Description);
    var result = await mediator.Send(command, cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<RoleResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpGet("{id:guid}/permissions")]
  public async Task<ActionResult<ApiResponse<List<PermissionResponse>>>> GetPermissionsByRoleId(
      Guid id,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetRolePermissionsQuery(id), cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<List<PermissionResponse>>.Ok(result.Value!.Select(p => p.ToResponse()).ToList()));
  }

  [HttpPut("{id:guid}/permissions")]
  public async Task<ActionResult<ApiResponse<RoleDetailResponse>>> UpdatePermissions(
      Guid id,
      [FromBody] UpdateRolePermissionsRequest request,
      CancellationToken cancellationToken)
  {
    var command = new UpdateRolePermissionsCommand(id, request.PermissionIds);
    var result = await mediator.Send(command, cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<RoleDetailResponse>.Ok(result.Value!.ToDetailResponse()));
  }

  [HttpGet("/api/v1/resources")]
  public async Task<ActionResult<ApiResponse<List<ResourceResponse>>>> GetResources(
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetResourcesQuery(), cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<List<ResourceResponse>>.Ok(result.Value!.Select(r => r.ToResponse()).ToList()));
  }

  [HttpGet("/api/v1/permissions")]
  public async Task<ActionResult<ApiResponse<List<PermissionResponse>>>> GetPermissions(
      [FromQuery] Guid? resourceId,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetPermissionsQuery(resourceId), cancellationToken);
    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<List<PermissionResponse>>.Ok(result.Value!.Select(p => p.ToResponse()).ToList()));
  }
}

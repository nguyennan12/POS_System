using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Employees.Commands.CreateEmployee;
using POS.Application.UseCases.Employees.Commands.UpdateEmployee;
using POS.Application.UseCases.Employees.Commands.LockEmployee;
using POS.Application.UseCases.Employees.Commands.ResetPassword;
using POS.Application.UseCases.Employees.Commands.ResetPin;
using POS.Application.UseCases.Employees.Queries.GetEmployees;
using POS.Application.UseCases.Employees.Queries.GetEmployeeDetail;
using POS.Application.UseCases.Employees.Queries.GetEmployeeLoginHistory;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<EmployeeResponse>>>> GetAll(
        [FromQuery] EmployeeFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmployeesQuery(request.StoreId, request.RoleId, request.Search,
            request.IsActive, request.PageNumber, request.PageSize), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        var page = result.Value!;
        return Ok(ApiResponse<PagedResponse<EmployeeResponse>>.Ok(new(
            page.Items.Select(e => e.ToResponse()).ToList(), page.PageNumber, page.PageSize, page.TotalCount)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmployeeDetailQuery(id), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<EmployeeDetailResponse>.Ok(result.Value!.ToDetailResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeDetailResponse>>> Create(
        [FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateEmployeeCommand(request.Name, request.Username, request.Password,
            request.Pin, request.RoleId, request.StoreId, request.IsChainOwner), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id },
            ApiResponse<EmployeeDetailResponse>.Ok(result.Value.ToDetailResponse()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailResponse>>> Update(
        Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateEmployeeCommand(id, request.Name, request.RoleId,
            request.StoreId, request.IsChainOwner), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<EmployeeDetailResponse>.Ok(result.Value!.ToDetailResponse()));
    }

    [HttpPut("{id:guid}/lock")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailResponse>>> Lock(
        Guid id, [FromBody] LockEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LockEmployeeCommand(id, request.IsActive), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<EmployeeDetailResponse>.Ok(result.Value!.ToDetailResponse()));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailResponse>>> ResetPassword(
        Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetPasswordCommand(id, request.NewPassword), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<EmployeeDetailResponse>.Ok(result.Value!.ToDetailResponse()));
    }

    [HttpPost("{id:guid}/reset-pin")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailResponse>>> ResetPin(
        Guid id, [FromBody] ResetPinRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetPinCommand(id, request.NewPin), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<EmployeeDetailResponse>.Ok(result.Value!.ToDetailResponse()));
    }

    [HttpGet("{id:guid}/login-history")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LoginHistoryResponse>>>> GetLoginHistory(
        Guid id, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmployeeLoginHistoryQuery(id, request.PageNumber, request.PageSize), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        var page = result.Value!;
        return Ok(ApiResponse<PagedResponse<LoginHistoryResponse>>.Ok(new(
            page.Items.Select(a => a.ToResponse()).ToList(), page.PageNumber, page.PageSize, page.TotalCount)));
    }
}

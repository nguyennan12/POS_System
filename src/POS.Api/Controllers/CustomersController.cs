using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Customers.Commands.CreateCustomer;
using POS.Application.UseCases.Customers.Commands.DeleteCustomer;
using POS.Application.UseCases.Customers.Commands.UpdateCustomer;
using POS.Application.UseCases.Customers.Queries.GetCustomerById;
using POS.Application.UseCases.Customers.Queries.GetCustomers;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Customers;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/customers")]
public class CustomersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<CustomerSummaryResponse>>>> GetPaged(
        [FromQuery] CustomerFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomersQuery(
            request.Phone,
            request.Name,
            request.Barcode,
            request.MemberTierId,
            request.PageNumber,
            request.PageSize);

        var result = await mediator.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        var paged = result.Value!;
        var response = new PagedResponse<CustomerSummaryResponse>(
            paged.Items.Select(x => x.ToSummaryResponse()).ToList().AsReadOnly(),
            paged.PageNumber,
            paged.PageSize,
            paged.TotalCount);

        return Ok(ApiResponse<PagedResponse<CustomerSummaryResponse>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<CustomerDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDetailResponse>>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(
            request.Name,
            request.Phone,
            request.Email,
            request.Dob,
            request.Barcode);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        var response = result.Value!.ToResponse();
        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            ApiResponse<CustomerDetailResponse>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDetailResponse>>> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand(
            id,
            request.Name,
            request.Phone,
            request.Email,
            request.Dob,
            request.Barcode,
            request.IsActive);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<CustomerDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteCustomerCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Ok(ApiResponse<bool>.Ok(result.Value));
    }
}

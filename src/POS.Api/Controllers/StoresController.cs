using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Stores.Commands.CreateStore;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Stores;
using POS.Api.Mappings;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/v1/stores")]
public class StoresController(ISender mediator) : ControllerBase
{
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
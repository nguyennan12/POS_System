using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Products.Commands.CreatePriceList;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/skus/{skuId}/price-lists")]
public class PriceListsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PriceListResponse>>> Create(
        Guid skuId,
        [FromBody] CreatePriceListRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePriceListCommand(
            skuId,
            request.Price,
            request.ValidFrom.UtcDateTime,
            request.ValidTo?.UtcDateTime,
            request.CustomerGroup);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<PriceListResponse>.Ok(new PriceListResponse(
            result.Value!.Id,
            result.Value.StoreId,
            result.Value.SkuId,
            result.Value.Price,
            result.Value.ValidFrom,
            result.Value.ValidTo,
            result.Value.CustomerGroup,
            result.Value.CreatedBy,
            result.Value.CreatedAt
        )));
    }
}

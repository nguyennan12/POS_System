using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Products.Commands.CreateUnitConversion;
using POS.Application.UseCases.Products.Commands.UpdateUnitConversion;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/skus/{skuId}/unit-conversions")]
public class UnitConversionsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UnitConversionResponse>>> Create(
        Guid skuId,
        [FromBody] CreateUnitConversionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUnitConversionCommand(
            skuId,
            request.UnitName,
            request.ConversionFactor,
            request.SellPrice);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<UnitConversionResponse>.Ok(new UnitConversionResponse(
            result.Value!.Id,
            result.Value.SkuId,
            result.Value.UnitName,
            result.Value.ConversionFactor,
            result.Value.SellPrice
        )));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UnitConversionResponse>>> Update(
        Guid skuId,
        Guid id,
        [FromBody] UpdateUnitConversionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUnitConversionCommand(
            id,
            request.UnitName,
            request.ConversionFactor,
            request.SellPrice);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<UnitConversionResponse>.Ok(new UnitConversionResponse(
            result.Value!.Id,
            result.Value.SkuId,
            result.Value.UnitName,
            result.Value.ConversionFactor,
            result.Value.SellPrice
        )));
    }
}

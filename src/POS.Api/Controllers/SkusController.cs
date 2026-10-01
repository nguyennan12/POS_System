using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Products.Commands.DeleteSku;
using POS.Application.UseCases.Products.Queries.GetSkuByBarcode;
using POS.Application.UseCases.Products.Queries.GetSkuById;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/skus")]
public class SkusController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// [S01] Lấy chi tiết SKU theo ID (kèm tồn kho và đơn vị tính quy đổi).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SkuDetailResponse>>> GetSkuById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSkuByIdQuery(id), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<SkuDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// [S02] Tra cứu SKU theo mã vạch — dùng cho màn hình quét POS.
    /// Phản hồi &lt;50ms (cache Redis hit). Trả HTTP 404 nếu không tìm thấy mã vạch.
    /// </summary>
    [HttpGet("barcode/{code}")]
    public async Task<ActionResult<ApiResponse<SkuBarcodeLookupResponse>>> GetByBarcode(
        string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(ApiResponse<object>.Fail(new ApiError
            {
                Code = "Barcode.Required",
                Message = "Mã vạch không được để trống.",
                Type = POS.Domain.Common.ErrorType.Validation
            }));

        var result = await mediator.Send(new GetSkuByBarcodeQuery(code.Trim()), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<SkuBarcodeLookupResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateSku(
        [FromBody] CreateSkuRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Application.UseCases.Products.Commands.CreateSku.CreateSkuCommand(
            request.ProductId,
            request.SkuCode,
            request.Barcode,
            request.CostPrice,
            request.SellPrice,
            request.TaxRate,
            request.Attributes
        );

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<Guid>.Ok(result.Value));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateSku(
        Guid id,
        [FromBody] UpdateSkuRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Application.UseCases.Products.Commands.UpdateSku.UpdateSkuCommand(
            id,
            request.SkuCode,
            request.Barcode,
            request.CostPrice,
            request.SellPrice,
            request.TaxRate,
            request.IsActive,
            request.Attributes
        );

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<Guid>.Ok(result.Value));
    }

    /// <summary>
    /// [S05] Xóa SKU theo ID. Chỉ cho phép xóa SKU thuộc store hiện tại.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSku(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteSkuCommand(id), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<object>.Ok(null));
    }
}

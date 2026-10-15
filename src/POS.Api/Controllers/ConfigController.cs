using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Config.Commands.UpdateStoreConfig;
using POS.Application.UseCases.Config.Commands.UpdateI18nDictionary;
using POS.Application.UseCases.Config.Queries.GetStoreConfig;
using POS.Application.UseCases.Config.Queries.GetI18nDictionary;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Config;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/config")]
public class ConfigController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Lấy toàn bộ cấu hình hệ thống của một cửa hàng.
    /// </summary>
    [HttpGet("store")]
    public async Task<ActionResult<ApiResponse<StoreConfigResponse>>> GetStoreConfig(
        [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStoreConfigQuery(storeId), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StoreConfigResponse>.Ok(result.Value!));
    }

    /// <summary>
    /// Cập nhật (upsert) cấu hình hệ thống của một cửa hàng.
    /// Không cần build lại app.
    /// </summary>
    [HttpPut("store")]
    public async Task<ActionResult<ApiResponse<StoreConfigResponse>>> UpdateStoreConfig(
        [FromQuery] Guid? storeId,
        [FromBody] UpdateStoreConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateStoreConfigCommand(storeId, request.Configs), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StoreConfigResponse>.Ok(result.Value!));
    }

    /// <summary>
    /// Lấy từ điển ngôn ngữ theo mã ngôn ngữ (vi, en, ...).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("i18n/{languageCode}")]
    public async Task<ActionResult<ApiResponse<I18nDictionaryResponse>>> GetI18nDictionary(
        string languageCode, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetI18nDictionaryQuery(languageCode), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<I18nDictionaryResponse>.Ok(result.Value!));
    }

    /// <summary>
    /// Cập nhật (upsert) từ điển ngôn ngữ. Thay đổi có hiệu lực ngay lập tức trên WinUI.
    /// </summary>
    [HttpPut("i18n/{languageCode}")]
    public async Task<ActionResult<ApiResponse<I18nDictionaryResponse>>> UpdateI18nDictionary(
        string languageCode,
        [FromBody] UpdateI18nDictionaryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateI18nDictionaryCommand(languageCode, request.Translations), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<I18nDictionaryResponse>.Ok(result.Value!));
    }
}

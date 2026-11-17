using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using POS.Api.Extensions;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.AI.Queries.AskAi;
using POS.Contracts.V1.Chatbot;
using POS.Contracts.V1.Common;
using POS.Domain.Common;
using POS.Infrastructure.AI.Gemini;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ai")]
public sealed class AiAssistantController(
    ISender mediator,
    IOptions<GeminiOptions> geminiOptions,
    ILogger<AiAssistantController> logger) : ControllerBase
{
    [HttpPost("ask")]
    [ProducesResponseType(typeof(ApiResponse<AskAiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<AskAiResponse>>> Ask(
        [FromBody] AskAiRequest request,
        CancellationToken cancellationToken)
    {
        if (!geminiOptions.Value.Enabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail(new ApiError
                {
                    Code = "AI_DISABLED",
                    Message = "Trợ lý AI hiện chưa được bật.",
                    Type = ErrorType.Unexpected
                }));
        }

        var history = request.History ?? Array.Empty<AiChatTurnRequest?>();
        if (history.Count > geminiOptions.Value.MaxHistoryMessages || history.Any(turn => turn is null))
        {
            return BadRequest(ApiResponse<object>.Fail(new ApiError
            {
                Code = "AI_INVALID_HISTORY",
                Message = "Lịch sử hội thoại không hợp lệ hoặc vượt quá giới hạn.",
                Type = ErrorType.Validation
            }));
        }

        var query = new AskAiQuery(
            request.Message,
            history.Select(turn => new AiConversationTurn(turn!.Role, turn.Content)).ToArray());

        try
        {
            var result = await mediator.Send(query, cancellationToken);
            if (result.IsFailure)
            {
                return this.ToActionResult(result);
            }

            return Ok(ApiResponse<AskAiResponse>.Ok(
                new AskAiResponse(result.Value!, DateTimeOffset.UtcNow)));
        }
        catch (AiModelException exception)
        {
            logger.LogWarning(
                "AI request failed with code {ErrorCode} and HTTP status {StatusCode}.",
                exception.Code,
                exception.StatusCode);

            return StatusCode(exception.StatusCode, ApiResponse<object>.Fail(new ApiError
            {
                Code = exception.Code,
                Message = exception.Message,
                Type = ErrorType.Unexpected
            }));
        }
    }
}

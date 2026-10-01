using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Auth;
using POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPassword;
using POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPin;
using POS.Application.UseCases.Auth.Commands.ChangePassword;
using POS.Application.UseCases.Auth.Commands.ChangePin;
using POS.Application.UseCases.Auth.Commands.Refresh;
using POS.Application.UseCases.Auth.Commands.Logout;
using POS.Application.UseCases.Auth.Queries.GetCurrentUser;
using POS.Api.Extensions;
using POS.Api.Mapping;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISender mediator) : ControllerBase
{
  [HttpGet("me")]
  [Authorize]
  public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetMe(CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetCurrentUserQuery(), cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<CurrentUserResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPost("employee/login")]
  public async Task<ActionResult<ApiResponse<AuthResponse>>> PasswordLogin(
    [FromBody] LoginRequest request,
    CancellationToken cancellationToken
  )
  {
    var command = new EmployeeLoginWithPasswordCommand(request.Username, request.Password);

    var result = await mediator.Send(command, cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<AuthResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPost("employee/pin")]
  public async Task<ActionResult<ApiResponse<AuthResponse>>> PinLogin(
    [FromBody] PinLoginRequest request,
    CancellationToken cancellationToken
  )
  {
    var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
    var command = new EmployeeLoginWithPinCommand(request.StoreId, request.Pin, deviceId ?? string.Empty);

    var result = await mediator.Send(command, cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<AuthResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPost("change-password")]
  [Authorize]
  public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequest request,
    CancellationToken cancellationToken
  )
  {
    var command = new ChangePasswordCommand(request.OldPassword, request.NewPassword);

    var result = await mediator.Send(command, cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return NoContent();
  }

  [HttpPost("change-pin")]
  [Authorize]
  public async Task<IActionResult> ChangePin(
    [FromBody] ChangePinRequest request,
    CancellationToken cancellationToken
  )
  {
    var command = new ChangePinCommand(request.OldPin, request.NewPin);

    var result = await mediator.Send(command, cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return NoContent();
  }

  [HttpPost("refresh")]
  public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
    [FromBody] RefreshTokenRequest request,
    CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
        new RefreshTokenCommand(request.RefreshToken),
        cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return Ok(ApiResponse<AuthResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPost("logout")]
  public async Task<IActionResult> Logout(
      [FromBody] LogoutRequest request,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
        new LogoutCommand(request.RefreshToken),
        cancellationToken);

    if (result.IsFailure)
      return this.ToActionResult(result);

    return NoContent();
  }
}


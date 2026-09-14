using Microsoft.AspNetCore.Mvc;
using MediatR;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Auth;
using POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPassword;
using POS.Api.Extensions;
using POS.Api.Mapping;
using POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPin;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISender mediator) : ControllerBase
{
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
}

using Microsoft.AspNetCore.Mvc;
using POS.Contracts.V1.Common;
using POS.Domain.Common;

namespace POS.Api.Extensions;

public static class ResultExtensions
{
  public static ActionResult ToActionResult(
      this ControllerBase controller,
      Result result,
      string? successMessage = null)
  {
    if (result.IsSuccess)
    {
      return controller.Ok(ApiResponse<object>.Ok(null, successMessage));
    }

    return controller.ToFailureActionResult(result);
  }

  public static ActionResult ToActionResult<T>(
      this ControllerBase controller,
      Result<T> result,
      string? successMessage = null)
  {
    if (result.IsSuccess)
    {
      return controller.Ok(ApiResponse<T>.Ok(result.Value!, successMessage));
    }

    return controller.ToFailureActionResult(result);
  }

  private static ActionResult ToFailureActionResult(
      this ControllerBase controller,
      Result result)
  {
    var error = new ApiError
    {
      Code = result.Error.Code,
      Message = result.Error.Message!,
      Type = result.Error.Type
    };

    if (result is IValidationResult validationResult)
    {
      error.ValidationErrors = validationResult.Errors
          .GroupBy(validationError => validationError.Code)
          .ToDictionary(
              group => group.Key,
              group => group
                  .Select(x => x.Message ?? "Invalid value")
                  .Distinct()
                  .ToArray());

      error.Code = IValidationResult.ValidationError.Code;
      error.Message = IValidationResult.ValidationError.Message ?? "A validation problem occurred.";
      error.Type = ErrorType.Validation;
    }

    return result.Error.Type switch
    {
      ErrorType.NotFound =>
          controller.NotFound(
              ApiResponse<object>.Fail(error)),

      ErrorType.AlreadyExists =>
          controller.Conflict(
              ApiResponse<object>.Fail(error)),

      ErrorType.Validation or ErrorType.Invalid =>
          controller.BadRequest(
              ApiResponse<object>.Fail(error)),

      ErrorType.Unauthorized =>
          controller.Unauthorized(ApiResponse<object>.Fail(error)),

      ErrorType.Forbidden =>
          controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(error)),

      _ =>
          controller.BadRequest(
              ApiResponse<object>.Fail(error))
    };
  }
}
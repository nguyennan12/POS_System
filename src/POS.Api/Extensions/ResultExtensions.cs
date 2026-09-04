using Microsoft.AspNetCore.Mvc;
using POS.Contracts.V1.Common;
using POS.Domain.Common;

namespace POS.Api.Extensions;

public static class ResultExtensions
{
  public static ActionResult ToActionResult<T>(
      this ControllerBase controller,
      Result<T> result)
  {
    if (result.IsSuccess)
    {
      return controller.Ok(ApiResponse<T>.Ok(result.Value!));
    }

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

      _ =>
          controller.BadRequest(
              ApiResponse<object>.Fail(error))
    };
  }
}
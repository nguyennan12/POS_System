using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using POS.Contracts.V1.Common;
using POS.Domain.Common;

namespace POS.Api.Exceptions;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment,
    IOptions<JsonOptions> jsonOptions) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
      HttpContext httpContext,
      Exception exception,
      CancellationToken cancellationToken)
  {
    var requestId = httpContext.TraceIdentifier;
    var traceId = System.Diagnostics.Activity.Current?.Id;

    logger.LogError(
        exception,
        "Unhandled exception. RequestId: {RequestId}, TraceId: {TraceId}",
        requestId,
        traceId);

    var statusCode = exception switch
    {
      ApplicationException => StatusCodes.Status400BadRequest,
      _ => StatusCodes.Status500InternalServerError
    };

    var apiError = new ApiError
    {
      Code = exception switch
      {
        ApplicationException => "APPLICATION_ERROR",
        _ => "INTERNAL_SERVER_ERROR"
      },
      Message = environment.IsDevelopment()
          ? exception.Message
          : "Đã có lỗi xảy ra từ máy chủ.",
      Type = exception switch
      {
        ApplicationException => ErrorType.Invalid,
        _ => ErrorType.Unexpected
      }
    };

    var response = ApiResponse<object>.Fail(apiError);

    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/json";
    await httpContext.Response.WriteAsJsonAsync(
        response,
        jsonOptions.Value.JsonSerializerOptions,
        cancellationToken);

    return true;
  }
}

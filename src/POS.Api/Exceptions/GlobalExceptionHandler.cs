using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace POS.Api.Exceptions;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
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

    httpContext.Response.StatusCode = exception switch
    {
      ApplicationException => StatusCodes.Status400BadRequest,
      _ => StatusCodes.Status500InternalServerError
    };

    return await problemDetailsService.TryWriteAsync(
        new ProblemDetailsContext
        {
          HttpContext = httpContext,
          Exception = exception,
          ProblemDetails = new ProblemDetails
          {
            Type = exception.GetType().Name,
            Title = "An error occurred",
            Status = httpContext.Response.StatusCode,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",

            Extensions =
                {
                        ["requestId"] = requestId,
                        ["traceId"] = traceId
                }
          }
        });
  }
}
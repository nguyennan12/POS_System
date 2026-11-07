using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using POS.Contracts.V1.Payments;

namespace POS.Api.Filters;

// Capture the original bytes BEFORE MVC model binding consumes the request stream.
[AttributeUsage(AttributeTargets.Method)]
public sealed class PaymentWebhookAttribute : Attribute, IAsyncResourceFilter, IActionFilter, IOrderedFilter
{
    public const string RawBodyKey = "PaymentWebhook.RawBody";
    public int Order => -3000; // Generic webhook errors precede ApiController's ModelStateInvalidFilter.

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        request.EnableBuffering(bufferThreshold: 64 * 1024, bufferLimit: 64 * 1024);
        try
        {
            using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, context.HttpContext.RequestAborted);
            context.HttpContext.Items[RawBodyKey] = buffer.ToArray();
            request.Body.Position = 0;
        }
        catch (IOException)
        {
            context.Result = Failure(context.HttpContext);
            return;
        }
        await next();
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid) context.Result = Failure(context.HttpContext);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static BadRequestObjectResult Failure(HttpContext context) =>
        context.Request.Path.Value?.EndsWith("/momo", StringComparison.OrdinalIgnoreCase) == true
            ? new(new MomoWebhookResponse(1, "Không thể xác nhận thanh toán."))
            : new(new VietQrWebhookResponse(false, "Không thể xác nhận thanh toán."));
}

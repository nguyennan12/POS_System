using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;

namespace POS.Api.Extensions;

public static class SerilogExtensions
{
    /// <summary>
    /// Cấu hình Serilog Request Logging: lọc bỏ các endpoint (/metrics, /health, /scalar) và phân loại log level theo StatusCode.
    /// </summary>
    public static IApplicationBuilder UsePosSerilogRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = ExcludeNoiseAndDetermineLevel;
        });
    }

    /// <summary>
    /// Middleware tự động ghi nhận chi tiết Request Body và Response Body (đã che các trường nhạy cảm) vào Serilog Diagnostic Context để đẩy lên Loki.
    /// </summary>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }

    private static LogEventLevel ExcludeNoiseAndDetermineLevel(HttpContext httpContext, double elapsedMs, Exception? ex)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;

        // Bỏ qua log từ internal/infra endpoints
        if (path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            return LogEventLevel.Verbose;
        }

        if (ex != null || httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode >= 400)
            return LogEventLevel.Warning;

        return LogEventLevel.Information;
    }
}

/// <summary>
/// Middleware nội bộ đọc Request & Response Body, che mật khẩu/token và gắn vào log Serilog Loki.
/// </summary>
internal sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDiagnosticContext _diagnosticContext;
    private const int MaxBodyLength = 16 * 1024; // 16 KB

    // Tự động che các trường nhạy cảm trong JSON để bảo mật dữ liệu log
    private static readonly Regex SensitiveDataRegex = new(
        @"""(password|pin|pinLookupSecret|secret|token|refreshToken|accessToken|oldPassword|newPassword)""\s*:\s*""([^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RequestResponseLoggingMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
    {
        _next = next;
        _diagnosticContext = diagnosticContext;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Bỏ qua các endpoint hệ thống không cần thiết để tránh làm rác log
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 1. Đọc và làm sạch Request Body
        await LogRequestBodyAsync(context);

        // 2. Bọc Response Body stream để bắt dữ liệu trả về
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
            await LogResponseBodyAsync(context, responseBodyStream);
        }
        finally
        {
            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestBodyAsync(HttpContext context)
    {
        var req = context.Request;
        if (req.ContentLength == null || req.ContentLength == 0 ||
            (!HttpMethods.IsPost(req.Method) && !HttpMethods.IsPut(req.Method) && !HttpMethods.IsPatch(req.Method) && !HttpMethods.IsDelete(req.Method)))
        {
            return;
        }

        req.EnableBuffering();

        using var reader = new StreamReader(req.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        req.Body.Position = 0;

        if (!string.IsNullOrWhiteSpace(body))
        {
            if (body.Length > MaxBodyLength)
            {
                body = body[..MaxBodyLength] + "... (truncated)";
            }

            var maskedBody = MaskSensitiveData(body);
            _diagnosticContext.Set("RequestBody", maskedBody);
        }
    }

    private async Task LogResponseBodyAsync(HttpContext context, MemoryStream responseBodyStream)
    {
        var contentType = context.Response.ContentType;
        if (contentType != null && !contentType.Contains("application/json") && !contentType.Contains("text/") && !contentType.Contains("application/problem+json"))
        {
            return;
        }

        responseBodyStream.Position = 0;
        using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        responseBodyStream.Position = 0;

        if (!string.IsNullOrWhiteSpace(body))
        {
            if (body.Length > MaxBodyLength)
            {
                body = body[..MaxBodyLength] + "... (truncated)";
            }

            var maskedBody = MaskSensitiveData(body);
            _diagnosticContext.Set("ResponseBody", maskedBody);
        }
    }

    private static string MaskSensitiveData(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        try
        {
            return SensitiveDataRegex.Replace(content, "\"$1\": \"***\"");
        }
        catch
        {
            return content;
        }
    }
}

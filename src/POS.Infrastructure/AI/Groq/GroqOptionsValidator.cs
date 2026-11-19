using Microsoft.Extensions.Options;

namespace POS.Infrastructure.AI.Groq;

public sealed class GroqOptionsValidator : IValidateOptions<GroqOptions>
{
    public ValidateOptionsResult Validate(string? name, GroqOptions options)
    {
        var errors = new List<string>();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(baseUri.UserInfo))
        {
            errors.Add("Groq:BaseUrl phải là địa chỉ HTTPS hợp lệ.");
        }

        if (options.TimeoutSeconds is < 5 or > 120)
            errors.Add("Groq:TimeoutSeconds phải nằm trong khoảng 5–120.");
        if (options.MaxCompletionTokens is < 128 or > 8192)
            errors.Add("Groq:MaxCompletionTokens phải nằm trong khoảng 128–8192.");
        if (options.Temperature is < 0 or > 1)
            errors.Add("Groq:Temperature phải nằm trong khoảng 0–1.");
        if (options.MaxHistoryMessages is < 1 or > 20)
            errors.Add("Groq:MaxHistoryMessages phải nằm trong khoảng 1–20.");

        if (options.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                errors.Add("Groq:ApiKey bắt buộc khi Groq được bật.");
            if (string.IsNullOrWhiteSpace(options.Model))
                errors.Add("Groq:Model bắt buộc khi Groq được bật.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}

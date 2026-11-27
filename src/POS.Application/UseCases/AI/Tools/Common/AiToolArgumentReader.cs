using System.Globalization;
using System.Text.Json;

namespace POS.Application.UseCases.AI.Tools.Common;

/// <summary>
/// Bộ đọc và kiểm tra biên tham số JSON dùng chung cho mọi <c>IAiToolHandler</c>.
/// Đảm bảo lỗi đầu vào luôn trả về <see cref="AiToolErrors.InvalidArguments"/> an toàn thay vì văng ngoại lệ.
/// </summary>
public static class AiToolArgumentReader
{
    public static bool EnsureAllowedProperties(
        JsonElement arguments,
        IReadOnlySet<string> allowedProperties,
        out AiToolExecutionResult? error)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            error = AiToolErrors.InvalidArgument("Tham số đầu vào phải là một đối tượng JSON.");
            return false;
        }

        foreach (var prop in arguments.EnumerateObject())
        {
            if (!allowedProperties.Contains(prop.Name))
            {
                error = AiToolErrors.InvalidArgument($"Tham số '{prop.Name}' không được hỗ trợ.");
                return false;
            }
        }

        error = null;
        return true;
    }

    public static bool TryReadRequiredString(
        JsonElement arguments,
        string propertyName,
        int minLength,
        int maxLength,
        out string value,
        out AiToolExecutionResult? error)
    {
        value = string.Empty;

        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind == JsonValueKind.Null ||
            prop.ValueKind == JsonValueKind.Undefined)
        {
            error = AiToolErrors.InvalidArgument($"Thiếu tham số bắt buộc '{propertyName}'.");
            return false;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            error = AiToolErrors.InvalidArgument($"Tham số '{propertyName}' phải là chuỗi ký tự.");
            return false;
        }

        var trimmed = (prop.GetString() ?? string.Empty).Trim();
        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            error = AiToolErrors.InvalidArgument(
                $"Tham số '{propertyName}' phải có độ dài từ {minLength} đến {maxLength} ký tự.");
            return false;
        }

        value = trimmed;
        error = null;
        return true;
    }

    public static bool TryReadOptionalString(
        JsonElement arguments,
        string propertyName,
        int maxLength,
        out string? value,
        out AiToolExecutionResult? error)
    {
        value = null;

        if (arguments.ValueKind != JsonValueKind.Object)
        {
            error = AiToolErrors.InvalidArgument("Tham số đầu vào phải là một đối tượng JSON.");
            return false;
        }

        if (!arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            error = null;
            return true;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            error = AiToolErrors.InvalidArgument($"Tham số '{propertyName}' phải là chuỗi ký tự.");
            return false;
        }

        var trimmed = (prop.GetString() ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            error = null;
            return true;
        }

        if (trimmed.Length > maxLength)
        {
            error = AiToolErrors.InvalidArgument(
                $"Tham số '{propertyName}' không được vượt quá {maxLength} ký tự.");
            return false;
        }

        value = trimmed;
        error = null;
        return true;
    }

    public static bool TryReadRequiredGuid(
        JsonElement arguments,
        string propertyName,
        out Guid value,
        out AiToolExecutionResult? error)
    {
        value = Guid.Empty;

        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            error = AiToolErrors.InvalidArgument($"Thiếu tham số bắt buộc '{propertyName}'.");
            return false;
        }

        if (prop.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(prop.GetString(), out var parsed) ||
            parsed == Guid.Empty)
        {
            error = AiToolErrors.InvalidArgument($"Tham số '{propertyName}' phải là mã UUID hợp lệ.");
            return false;
        }

        value = parsed;
        error = null;
        return true;
    }

    public static bool TryReadOptionalGuid(
        JsonElement arguments,
        string propertyName,
        out Guid? value,
        out AiToolExecutionResult? error)
    {
        value = null;

        if (arguments.ValueKind != JsonValueKind.Object)
        {
            error = AiToolErrors.InvalidArgument("Tham số đầu vào phải là một đối tượng JSON.");
            return false;
        }

        if (!arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            error = null;
            return true;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            error = AiToolErrors.InvalidArgument($"Tham số '{propertyName}' phải là mã UUID hợp lệ.");
            return false;
        }

        var raw = (prop.GetString() ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            error = null;
            return true;
        }

        if (!Guid.TryParse(raw, out var parsed) || parsed == Guid.Empty)
        {
            error = AiToolErrors.InvalidArgument($"Tham số '{propertyName}' phải là mã UUID hợp lệ.");
            return false;
        }

        value = parsed;
        error = null;
        return true;
    }

    public static bool TryReadOptionalInt(
        JsonElement arguments,
        string propertyName,
        int defaultValue,
        int min,
        int max,
        out int value,
        out AiToolExecutionResult? error)
    {
        value = defaultValue;

        if (arguments.ValueKind != JsonValueKind.Object)
        {
            error = AiToolErrors.InvalidArgument("Tham số đầu vào phải là một đối tượng JSON.");
            return false;
        }

        if (!arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            error = null;
            return true;
        }

        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var parsed))
        {
            error = AiToolErrors.InvalidArgument($"Tham số '{propertyName}' phải là số nguyên hợp lệ.");
            return false;
        }

        if (parsed < min || parsed > max)
        {
            error = AiToolErrors.InvalidArgument(
                $"Tham số '{propertyName}' phải nằm trong khoảng từ {min} đến {max}.");
            return false;
        }

        value = parsed;
        error = null;
        return true;
    }

    public static bool TryReadOptionalDecimal(
        JsonElement arguments,
        string propertyName,
        decimal defaultValue,
        decimal minExclusive,
        decimal maxInclusive,
        out decimal value,
        out AiToolExecutionResult? error)
    {
        value = defaultValue;

        if (arguments.ValueKind != JsonValueKind.Object)
        {
            error = AiToolErrors.InvalidArgument("Tham số đầu vào phải là một đối tượng JSON.");
            return false;
        }

        if (!arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            error = null;
            return true;
        }

        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetDecimal(out var parsed))
        {
            error = AiToolErrors.InvalidArgument($"Tham số '{propertyName}' phải là số hợp lệ.");
            return false;
        }

        if (parsed <= minExclusive || parsed > maxInclusive)
        {
            error = AiToolErrors.InvalidArgument(
                $"Tham số '{propertyName}' phải lớn hơn {minExclusive} và không vượt quá {maxInclusive}.");
            return false;
        }

        value = parsed;
        error = null;
        return true;
    }

    public static bool TryReadRequiredDateOnly(
        JsonElement arguments,
        string propertyName,
        out DateOnly value,
        out AiToolExecutionResult? error)
    {
        value = default;

        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            error = AiToolErrors.InvalidArgument($"Thiếu tham số ngày bắt buộc '{propertyName}'.");
            return false;
        }

        if (prop.ValueKind != JsonValueKind.String ||
            !TryParseIsoDateOnly(prop.GetString(), out value))
        {
            error = AiToolErrors.InvalidArgument(
                $"Tham số '{propertyName}' phải theo định dạng ngày ISO YYYY-MM-DD.");
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryReadOptionalDateOnly(
        JsonElement arguments,
        string propertyName,
        out DateOnly? value,
        out AiToolExecutionResult? error)
    {
        value = null;

        if (arguments.ValueKind != JsonValueKind.Object)
        {
            error = AiToolErrors.InvalidArgument("Tham số đầu vào phải là một đối tượng JSON.");
            return false;
        }

        if (!arguments.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            error = null;
            return true;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            error = AiToolErrors.InvalidArgument(
                $"Tham số '{propertyName}' phải theo định dạng ngày ISO YYYY-MM-DD.");
            return false;
        }

        var raw = (prop.GetString() ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            error = null;
            return true;
        }

        if (!TryParseIsoDateOnly(raw, out var parsed))
        {
            error = AiToolErrors.InvalidArgument(
                $"Tham số '{propertyName}' phải theo định dạng ngày ISO YYYY-MM-DD.");
            return false;
        }

        value = parsed;
        error = null;
        return true;
    }

    public static bool TryReadRequiredDateRangeUtc(
        JsonElement arguments,
        string fromPropertyName,
        string toPropertyName,
        out DateTimeOffset fromUtc,
        out DateTimeOffset toUtc,
        out AiToolExecutionResult? error)
    {
        fromUtc = default;
        toUtc = default;

        if (!TryReadRequiredDateOnly(arguments, fromPropertyName, out var fromDate, out error))
            return false;

        if (!TryReadRequiredDateOnly(arguments, toPropertyName, out var toDate, out error))
            return false;

        if (fromDate > toDate)
        {
            error = AiToolErrors.InvalidArgument(
                $"Khoảng ngày không hợp lệ: '{fromPropertyName}' không được lớn hơn '{toPropertyName}'.");
            return false;
        }

        fromUtc = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);
        toUtc = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc), TimeSpan.Zero);
        error = null;
        return true;
    }

    public static bool TryReadOptionalDateRangeUtc(
        JsonElement arguments,
        string fromPropertyName,
        string toPropertyName,
        out DateTimeOffset? fromUtc,
        out DateTimeOffset? toUtc,
        out AiToolExecutionResult? error)
    {
        fromUtc = null;
        toUtc = null;

        if (!TryReadOptionalDateOnly(arguments, fromPropertyName, out var fromDate, out error))
            return false;

        if (!TryReadOptionalDateOnly(arguments, toPropertyName, out var toDate, out error))
            return false;

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            error = AiToolErrors.InvalidArgument(
                $"Khoảng ngày không hợp lệ: '{fromPropertyName}' không được lớn hơn '{toPropertyName}'.");
            return false;
        }

        if (fromDate.HasValue)
            fromUtc = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);

        if (toDate.HasValue)
            toUtc = new DateTimeOffset(toDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc), TimeSpan.Zero);

        error = null;
        return true;
    }

    public static bool TryReadOptionalEnum<TEnum>(
        JsonElement arguments,
        string propertyName,
        out TEnum? value,
        out string? canonicalName,
        out AiToolExecutionResult? error)
        where TEnum : struct, Enum
    {
        value = null;
        canonicalName = null;

        if (!TryReadOptionalString(arguments, propertyName, 64, out var raw, out error))
            return false;

        if (raw is null)
            return true;

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<TEnum>(raw, ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            error = AiToolErrors.InvalidArgument($"Giá trị '{raw}' của tham số '{propertyName}' không hợp lệ.");
            return false;
        }

        value = parsed;
        canonicalName = parsed.ToString();
        error = null;
        return true;
    }

    private static bool TryParseIsoDateOnly(string? raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        if (DateOnly.TryParseExact(
                trimmed,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            return true;
        }

        if (DateTimeOffset.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dto))
        {
            date = DateOnly.FromDateTime(dto.UtcDateTime);
            return true;
        }

        return false;
    }
}

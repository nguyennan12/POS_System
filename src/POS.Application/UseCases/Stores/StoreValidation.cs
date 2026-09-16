namespace POS.Application.UseCases.Stores;

internal static class StoreValidation
{
    internal static bool IsValidTimezone(string? value) =>
        !string.IsNullOrWhiteSpace(value) && TimeZoneInfo.TryFindSystemTimeZoneById(value, out _);
}

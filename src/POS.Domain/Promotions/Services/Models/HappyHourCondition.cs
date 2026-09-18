namespace POS.Domain.Promotions.Services.Models;

/// <summary>Defines the time window and weekdays for a happy-hour promotion.</summary>
public record HappyHourCondition(
    TimeSpan? StartTime = null,
    TimeSpan? EndTime = null,
    IReadOnlyList<DayOfWeek>? DaysOfWeek = null);

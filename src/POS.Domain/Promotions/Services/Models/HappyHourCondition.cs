namespace POS.Domain.Promotions.Services.Models;

public record HappyHourCondition(
    TimeSpan? StartTime = null,
    TimeSpan? EndTime = null,
    IReadOnlyList<DayOfWeek>? DaysOfWeek = null);

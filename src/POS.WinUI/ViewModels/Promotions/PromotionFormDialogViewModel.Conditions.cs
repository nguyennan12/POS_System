using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.ViewModels.Promotions;

public sealed partial class PromotionWeekdayOption(DayOfWeek day, string displayName) : ObservableObject
{
    public DayOfWeek Day { get; } = day;
    public string DisplayName { get; } = displayName;

    [ObservableProperty] private bool _isSelected;
}

public sealed partial class PromotionFormDialogViewModel
{
    private static readonly JsonSerializerOptions ConditionJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private static readonly string[] ClockFormats = [@"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss", "c"];

    private BuyXGetYCondition? _originalBuyCondition;
    private HappyHourCondition? _originalHourCondition;
    private BuyFields? _originalBuyFields;
    private HourFields? _originalHourFields;
    private bool _invalidOriginalConditions;

    [ObservableProperty] private string _buyQuantityText = "1";
    [ObservableProperty] private string _getQuantityText = "1";
    [ObservableProperty] private Guid? _freeSkuId = Guid.Empty;
    [ObservableProperty] private IReadOnlyList<PromotionTargetOption> _freeSkuOptions = [];
    [ObservableProperty] private string _discountPercentText = "100";
    [ObservableProperty] private string _startTimeText = string.Empty;
    [ObservableProperty] private string _endTimeText = string.Empty;

    public IReadOnlyList<PromotionWeekdayOption> WeekdayOptions { get; } =
    [
        new(DayOfWeek.Monday, "Thứ 2"),
        new(DayOfWeek.Tuesday, "Thứ 3"),
        new(DayOfWeek.Wednesday, "Thứ 4"),
        new(DayOfWeek.Thursday, "Thứ 5"),
        new(DayOfWeek.Friday, "Thứ 6"),
        new(DayOfWeek.Saturday, "Thứ 7"),
        new(DayOfWeek.Sunday, "Chủ nhật")
    ];

    public string? ConditionLoadWarning => _invalidOriginalConditions && IsSameTypeAsOriginal
        ? "Điều kiện đã lưu không đọc được. Các trường bên dưới sẽ thay thế điều kiện cũ khi bạn sửa chúng."
        : null;

    private BuyFields CurrentBuyFields => new(BuyQuantityText, GetQuantityText, FreeSkuId ?? Guid.Empty, DiscountPercentText);
    private HourFields CurrentHourFields => new(StartTimeText, EndTimeText,
        WeekdayOptions.Where(day => day.IsSelected).Sum(day => 1 << (int)day.Day));

    private void LoadConditions(string? json)
    {
        try
        {
            if (IsBuyXGetY && !string.IsNullOrWhiteSpace(json))
            {
                _originalBuyCondition = JsonSerializer.Deserialize<BuyXGetYCondition>(json, ConditionJsonOptions);
                if (_originalBuyCondition is { } buy)
                {
                    BuyQuantityText = buy.BuyQuantity.ToString(_culture);
                    GetQuantityText = buy.GetQuantity.ToString(_culture);
                    FreeSkuId = buy.FreeSkuId ?? Guid.Empty;
                    DiscountPercentText = buy.DiscountPercent.ToString(_culture);
                }
            }
            else if (IsHappyHour && !string.IsNullOrWhiteSpace(json))
            {
                _originalHourCondition = JsonSerializer.Deserialize<HappyHourCondition>(json, ConditionJsonOptions);
                if (_originalHourCondition is { } hour)
                {
                    StartTimeText = hour.StartTime?.ToString("c", CultureInfo.InvariantCulture) ?? string.Empty;
                    EndTimeText = hour.EndTime?.ToString("c", CultureInfo.InvariantCulture) ?? string.Empty;
                    foreach (var day in WeekdayOptions)
                        day.IsSelected = hour.DaysOfWeek?.Contains(day.Day) == true;
                }
            }
        }
        catch (JsonException)
        {
            _invalidOriginalConditions = true;
        }

        _originalBuyFields = CurrentBuyFields;
        _originalHourFields = CurrentHourFields;
    }

    private bool TryBuildConditionsJson(out string? json)
    {
        json = null;
        if (IsSameTypeAsOriginal &&
            (!IsBuyXGetY || CurrentBuyFields == _originalBuyFields) &&
            (!IsHappyHour || CurrentHourFields == _originalHourFields))
        {
            json = _original!.ConditionsJson;
            return true;
        }

        if (IsBuyXGetY)
        {
            if (!int.TryParse(BuyQuantityText, NumberStyles.Integer, _culture, out var buyQuantity) || buyQuantity <= 0 ||
                !int.TryParse(GetQuantityText, NumberStyles.Integer, _culture, out var getQuantity) || getQuantity <= 0 ||
                (long)buyQuantity + getQuantity > int.MaxValue)
                return Invalid("Số lượng mua và số lượng tặng phải là số nguyên dương hợp lệ.");
            if (!TryDecimal(DiscountPercentText, out var discountPercent) || discountPercent <= 0 || discountPercent > 100)
                return Invalid("Mức giảm cho sản phẩm tặng phải lớn hơn 0% và không vượt quá 100%.");

            var giftId = FreeSkuId is Guid id && id != Guid.Empty ? id : (Guid?)null;
            var condition = new BuyXGetYCondition(buyQuantity, getQuantity, giftId, discountPercent)
            {
                AdditionalFields = IsSameTypeAsOriginal ? _originalBuyCondition?.AdditionalFields : null
            };
            json = JsonSerializer.Serialize(condition, ConditionJsonOptions);
        }
        else if (IsHappyHour)
        {
            if (!TryParseClock(StartTimeText, out var start) || !TryParseClock(EndTimeText, out var end))
                return Invalid("Nhập giờ bắt đầu và kết thúc theo định dạng HH:mm UTC.");
            if (start.HasValue && end.HasValue && start > end)
                return Invalid("Giờ kết thúc phải từ giờ bắt đầu trở đi trong cùng ngày UTC.");

            var days = WeekdayOptions.Where(day => day.IsSelected).Select(day => day.Day).ToArray();
            var condition = new HappyHourCondition(start, end, days)
            {
                AdditionalFields = IsSameTypeAsOriginal ? _originalHourCondition?.AdditionalFields : null
            };
            json = JsonSerializer.Serialize(condition, ConditionJsonOptions);
        }

        return true;
    }

    private static bool TryParseClock(string text, out TimeSpan? time)
    {
        time = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (!TimeSpan.TryParseExact(text.Trim(), ClockFormats, CultureInfo.InvariantCulture, out var parsed)
            || parsed < TimeSpan.Zero || parsed >= TimeSpan.FromDays(1))
            return false;

        time = parsed;
        return true;
    }

    private sealed record BuyFields(string BuyQuantity, string GetQuantity, Guid GiftId, string DiscountPercent);
    private sealed record HourFields(string Start, string End, int Days);

    // Mirrors the condition shapes consumed by the backend promotion engine.
    private sealed record BuyXGetYCondition(
        int BuyQuantity = 1, int GetQuantity = 1, Guid? FreeSkuId = null, decimal DiscountPercent = 100)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
    }

    private sealed record HappyHourCondition(
        TimeSpan? StartTime = null, TimeSpan? EndTime = null, IReadOnlyList<DayOfWeek>? DaysOfWeek = null)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
    }
}

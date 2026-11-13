using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Promotions;

namespace POS.WinUI.ViewModels.Promotions;

public record PromotionTypeOption(string Code, string DisplayName);

public sealed partial class PromotionFormDialogViewModel : ObservableObject
{
    public Guid? Id { get; }
    public bool IsEdit => Id.HasValue;
    public string DialogTitle => IsEdit ? "Cập nhật chương trình khuyến mãi" : "Tạo chương trình khuyến mãi mới";
    public string DialogSubtitle => IsEdit ? "Điều chỉnh mức giảm giá, điều kiện áp dụng và thời hạn" : "Thiết lập quy tắc giảm giá tự động hoặc giờ vàng cho khách mua hàng";

    public IReadOnlyList<PromotionTypeOption> TypeOptions { get; } =
    [
        new("OrderDiscount", "Chiết khấu đơn hàng (%)"),
        new("ItemDiscount", "Giảm giá sản phẩm (Tiền mặt)"),
        new("BuyXGetY", "Mua X tặng Y"),
        new("HappyHour", "Giờ vàng (Happy Hour)")
    ];

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private PromotionTypeOption _selectedType;
    [ObservableProperty] private string _valueText = string.Empty;
    [ObservableProperty] private string _minOrderAmountText = "0";
    [ObservableProperty] private string? _maxDiscountAmountText;
    [ObservableProperty] private DateTime? _validFrom = DateTime.Today;
    [ObservableProperty] private DateTime? _validTo;
    [ObservableProperty] private bool _isStackable;
    [ObservableProperty] private bool _isExclusive;
    [ObservableProperty] private string _status = "Active";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSaving;

    public PromotionFormDialogViewModel(PromotionDetailResponse? promo = null)
    {
        SelectedType = TypeOptions[0];

        if (promo != null)
        {
            Id = promo.Id;
            Name = promo.Name;
            SelectedType = TypeOptions.FirstOrDefault(t => t.Code == promo.Type) ?? TypeOptions[0];
            ValueText = promo.Value.ToString("G29");
            MinOrderAmountText = promo.MinOrderAmount.ToString("N0");
            MaxDiscountAmountText = promo.MaxDiscountAmount.HasValue ? promo.MaxDiscountAmount.Value.ToString("N0") : null;
            ValidFrom = promo.ValidFrom.LocalDateTime.Date;
            ValidTo = promo.ValidTo.HasValue ? promo.ValidTo.Value.LocalDateTime.Date : null;
            IsStackable = promo.IsStackable;
            IsExclusive = promo.IsExclusive;
            Status = promo.Status;
        }
    }

    public bool Validate(out decimal value, out decimal minOrder, out decimal? maxDiscount)
    {
        value = 0;
        minOrder = 0;
        maxDiscount = null;
        ErrorMessage = null;

        var trimmedName = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Length < 3)
        {
            ErrorMessage = "Tên chương trình khuyến mãi bắt buộc nhập (tối thiểu 3 ký tự).";
            return false;
        }

        if (!decimal.TryParse(ValueText.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value) || value <= 0)
        {
            ErrorMessage = "Giá trị khuyến mãi phải là số dương lớn hơn 0.";
            return false;
        }

        if (SelectedType.Code == "OrderDiscount" && value > 100)
        {
            ErrorMessage = "Mức giảm chiết khấu theo phần trăm không được vượt quá 100%.";
            return false;
        }

        if (!decimal.TryParse(MinOrderAmountText.Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out minOrder) || minOrder < 0)
        {
            ErrorMessage = "Giá trị đơn hàng tối thiểu phải là số không âm (>= 0).";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(MaxDiscountAmountText))
        {
            if (decimal.TryParse(MaxDiscountAmountText.Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMax) && parsedMax > 0)
            {
                maxDiscount = parsedMax;
            }
            else
            {
                ErrorMessage = "Mức giảm tối đa (nếu có) phải là một số tiền hợp lệ lớn hơn 0.";
                return false;
            }
        }

        if (ValidFrom.HasValue && ValidTo.HasValue && ValidTo.Value.Date < ValidFrom.Value.Date)
        {
            ErrorMessage = "Ngày kết thúc chương trình phải lớn hơn hoặc bằng ngày bắt đầu.";
            return false;
        }

        return true;
    }

    public CreatePromotionRequest ToCreateRequest(decimal value, decimal minOrder, decimal? maxDiscount) => new(
        Name: Name.Trim(),
        Type: SelectedType.Code,
        Value: value,
        MinOrderAmount: minOrder,
        MaxDiscountAmount: maxDiscount,
        IsStackable: IsStackable,
        IsExclusive: IsExclusive,
        ValidFrom: ValidFrom.HasValue ? new DateTimeOffset(ValidFrom.Value, TimeSpan.Zero) : null,
        ValidTo: ValidTo.HasValue ? new DateTimeOffset(ValidTo.Value.AddDays(1).AddTicks(-1), TimeSpan.Zero) : null
    );

    public UpdatePromotionRequest ToUpdateRequest(decimal value, decimal minOrder, decimal? maxDiscount) => new(
        Name: Name.Trim(),
        Type: SelectedType.Code,
        Value: value,
        MinOrderAmount: minOrder,
        MaxDiscountAmount: maxDiscount,
        IsStackable: IsStackable,
        IsExclusive: IsExclusive,
        ValidFrom: ValidFrom.HasValue ? new DateTimeOffset(ValidFrom.Value, TimeSpan.Zero) : null,
        ValidTo: ValidTo.HasValue ? new DateTimeOffset(ValidTo.Value.AddDays(1).AddTicks(-1), TimeSpan.Zero) : null,
        Status: Status
    );
}

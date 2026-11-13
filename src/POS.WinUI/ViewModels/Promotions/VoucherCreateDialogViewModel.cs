using System;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Promotions;

namespace POS.WinUI.ViewModels.Promotions;

public sealed partial class VoucherCreateDialogViewModel : ObservableObject
{
    private static readonly Regex CodeRegex = new(@"^[A-Z0-9_\-]{3,30}$", RegexOptions.Compiled);

    public Guid PromotionId { get; }
    public string PromotionName { get; }

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _maxUsesText = "100";
    [ObservableProperty] private string _perCustomerLimitText = "1";
    [ObservableProperty] private DateTime? _expiresAt = DateTime.Today.AddDays(30);
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSaving;

    public VoucherCreateDialogViewModel(Guid promotionId, string promotionName)
    {
        PromotionId = promotionId;
        PromotionName = promotionName;
    }

    public bool Validate(out int maxUses, out int perCustomerLimit)
    {
        maxUses = 0;
        perCustomerLimit = 1;
        ErrorMessage = null;

        var upperCode = Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(upperCode) || !CodeRegex.IsMatch(upperCode))
        {
            ErrorMessage = "Mã Voucher chỉ gồm 3-30 ký tự chữ in hoa (A-Z), số (0-9) và dấu gạch nối, không chứa dấu cách.";
            return false;
        }

        if (!int.TryParse(MaxUsesText.Trim(), out maxUses) || maxUses <= 0)
        {
            ErrorMessage = "Số lượt sử dụng tối đa phải là một số nguyên dương (>= 1).";
            return false;
        }

        if (!int.TryParse(PerCustomerLimitText.Trim(), out perCustomerLimit) || perCustomerLimit <= 0)
        {
            ErrorMessage = "Giới hạn lượt dùng trên mỗi khách hàng phải là số nguyên dương (>= 1).";
            return false;
        }

        if (ExpiresAt.HasValue && ExpiresAt.Value.Date < DateTime.Today)
        {
            ErrorMessage = "Ngày hết hạn của mã Voucher phải lớn hơn hoặc bằng ngày hiện tại.";
            return false;
        }

        return true;
    }

    public CreateVoucherRequest ToRequest(int maxUses, int perCustomerLimit) => new(
        Code: Code.Trim().ToUpperInvariant(),
        MaxUses: maxUses,
        PerCustomerLimit: perCustomerLimit,
        ExpiresAt: ExpiresAt.HasValue ? new DateTimeOffset(ExpiresAt.Value.AddDays(1).AddTicks(-1), TimeSpan.Zero) : null
    );
}

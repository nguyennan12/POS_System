using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Customers;

namespace POS.WinUI.ViewModels.Customers;

public sealed partial class PointAdjustDialogViewModel : ObservableObject
{
    public Guid CustomerId { get; }
    public string CustomerName { get; }
    public decimal CurrentBalance { get; }

    [ObservableProperty] private bool _isAccrue = true;
    [ObservableProperty] private string _pointsText = string.Empty;
    [ObservableProperty] private string _reason = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSaving;

    public PointAdjustDialogViewModel(Guid customerId, string customerName, decimal currentBalance)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CurrentBalance = currentBalance;
    }

    public bool Validate(out decimal validatedPoints)
    {
        validatedPoints = 0;
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(PointsText) ||
            !decimal.TryParse(PointsText.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ||
            parsed <= 0)
        {
            ErrorMessage = "Số điểm điều chỉnh phải là một số dương lớn hơn 0.";
            return false;
        }

        if (!IsAccrue && parsed > CurrentBalance)
        {
            ErrorMessage = $"Số điểm trừ ({parsed:N0}) không được vượt quá số dư điểm hiện có ({CurrentBalance:N0} điểm).";
            return false;
        }

        var trimmedReason = Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedReason) || trimmedReason.Length < 3)
        {
            ErrorMessage = "Lý do điều chỉnh điểm bắt buộc nhập (tối thiểu 3 ký tự) để phục vụ kiểm toán.";
            return false;
        }

        validatedPoints = parsed;
        return true;
    }

    public AdjustPointsRequest ToRequest(decimal points) => new(
        Points: IsAccrue ? points : -points,
        Note: Reason.Trim(),
        Reason: Reason.Trim()
    );
}

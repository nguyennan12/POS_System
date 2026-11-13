using System;
using System.Net.Mail;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Customers;

namespace POS.WinUI.ViewModels.Customers;

public sealed partial class CustomerFormDialogViewModel : ObservableObject
{
    private static readonly Regex PhoneRegex = new(@"^(0[35789])[0-9]{8}$", RegexOptions.Compiled);

    public Guid? Id { get; }
    public bool IsEdit => Id.HasValue;
    public string DialogTitle => IsEdit ? "Cập nhật hồ sơ khách hàng" : "Thêm khách hàng mới";
    public string DialogSubtitle => IsEdit ? "Chỉnh sửa thông tin liên hệ và trạng thái hoạt động" : "Tạo mới tài khoản thành viên để tích lũy điểm khi mua hàng";

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private DateTime? _dob;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSaving;

    public CustomerFormDialogViewModel(CustomerDetailResponse? customer = null)
    {
        if (customer != null)
        {
            Id = customer.Id;
            Name = customer.Name;
            Phone = customer.Phone;
            Email = customer.Email;
            Dob = customer.Dob.HasValue ? customer.Dob.Value.ToDateTime(TimeOnly.MinValue) : null;
            Barcode = customer.Barcode;
            IsActive = customer.IsActive;
        }
    }

    public bool Validate()
    {
        ErrorMessage = null;

        var trimmedName = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Length < 2)
        {
            ErrorMessage = "Họ và tên khách hàng bắt buộc nhập (tối thiểu 2 ký tự).";
            return false;
        }

        var trimmedPhone = Phone?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedPhone) || !PhoneRegex.IsMatch(trimmedPhone))
        {
            ErrorMessage = "Số điện thoại không hợp lệ (yêu cầu 10 chữ số chuẩn định dạng Việt Nam, ví dụ: 0912345678).";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            try
            {
                _ = new MailAddress(Email.Trim());
            }
            catch
            {
                ErrorMessage = "Định dạng hòm thư điện tử (Email) không hợp lệ.";
                return false;
            }
        }

        if (Dob.HasValue && Dob.Value.Date >= DateTime.Today)
        {
            ErrorMessage = "Ngày sinh của khách hàng phải nhỏ hơn ngày hiện tại.";
            return false;
        }

        return true;
    }

    public CreateCustomerRequest ToCreateRequest() => new(
        Name: Name.Trim(),
        Phone: Phone.Trim(),
        Email: string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
        Dob: Dob.HasValue ? DateOnly.FromDateTime(Dob.Value) : null,
        Barcode: string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim()
    );

    public UpdateCustomerRequest ToUpdateRequest() => new(
        Name: Name.Trim(),
        Phone: Phone.Trim(),
        Email: string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
        Dob: Dob.HasValue ? DateOnly.FromDateTime(Dob.Value) : null,
        Barcode: string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
        IsActive: IsActive
    );
}

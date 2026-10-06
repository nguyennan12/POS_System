using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Customers;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ KHÁCH HÀNG & THÀNH VIÊN ════════════════════

    [ObservableProperty]
    private string _customerSearchText = string.Empty;

    [ObservableProperty]
    private string _selectedCustomerName = "Khách lẻ";

    [ObservableProperty]
    private Guid? _selectedCustomerId = null;

    [ObservableProperty]
    private string _customerPhoneNumber = string.Empty;

    [ObservableProperty]
    private decimal _customerLoyaltyPoints = 0;

    [ObservableProperty]
    private string _customerTierName = "Thành viên";

    [ObservableProperty]
    private decimal _customerPointRate = 0.01m;

    [ObservableProperty]
    private decimal _customerDiscountRate = 0m;

    [ObservableProperty]
    private decimal _customerPointRedemptionRate = 1000m;

    [ObservableProperty]
    private bool _hasSelectedCustomer = false;

    [ObservableProperty]
    private bool _isCustomerNotFound = false;

    [ObservableProperty]
    private bool _isSearchingCustomer = false;

    public ObservableCollection<CustomerSummaryResponse> CustomerSearchResults { get; } = new();

    [ObservableProperty]
    private bool _showQuickRegister = false;

    [ObservableProperty]
    private string _quickRegisterName = string.Empty;

    [ObservableProperty]
    private string _quickRegisterPhone = string.Empty;

    [ObservableProperty]
    private string _quickRegisterError = string.Empty;

    [ObservableProperty]
    private bool _isCreatingCustomer = false;

    [ObservableProperty]
    private bool _useLoyaltyPoints = false;

    [ObservableProperty]
    private decimal _loyaltyDiscount = 0;

    [ObservableProperty]
    private string _loyaltyUseDisplayText = "Dùng điểm";

    [ObservableProperty]
    private bool _canUseLoyaltyPoints = false;

    [ObservableProperty]
    private string _orderNote = string.Empty;

    [RelayCommand]
    private void ClearOrderNote()
    {
        OrderNote = string.Empty;
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (IsCustomerNotFound)
        {
            IsCustomerNotFound = false;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            CustomerSearchResults.Clear();
            if (!HasSelectedCustomer)
            {
                SelectedCustomerId = null;
                SelectedCustomerName = "Khách lẻ";
                CustomerPhoneNumber = string.Empty;
                CustomerLoyaltyPoints = 0;
                CustomerTierName = "Thành viên";
                CustomerPointRate = 0.01m;
                CustomerDiscountRate = 0m;
                CustomerPointRedemptionRate = 1000m;
                HasSelectedCustomer = false;
                IsCustomerNotFound = false;
                ShowQuickRegister = false;
            }
        }
    }

    [RelayCommand]
    public async Task SearchCustomerAsync()
    {
        var query = CustomerSearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            QuickSelectCustomer("Retail");
            return;
        }

        // Kiểm tra nếu người dùng đang nhập số điện thoại
        bool isDigitsOnly = Regex.IsMatch(query, @"^[0-9]+$");
        if (isDigitsOnly)
        {
            // Yêu cầu bắt buộc phải nhập đúng 10 số bắt đầu bằng 0
            if (!Regex.IsMatch(query, @"^0[0-9]{9}$"))
            {
                IsCustomerNotFound = true;
                ShowError("Số điện thoại không hợp lệ");
                return;
            }
        }

        IsSearchingCustomer = true;
        IsCustomerNotFound = false;

        try
        {
            string? phoneQuery = isDigitsOnly ? query : null;
            string? nameQuery = !isDigitsOnly ? query : null;

            var res = await _customerApiClient.GetCustomersAsync(
                phone: phoneQuery,
                name: nameQuery,
                pageNumber: 1,
                pageSize: 5);

            CustomerSearchResults.Clear();

            if (res?.Success == true && res.Data?.Items != null && res.Data.Items.Count > 0)
            {
                // Nếu tìm theo SĐT -> Khớp chính xác 100% SĐT
                CustomerSummaryResponse? matched = null;
                if (isDigitsOnly)
                {
                    matched = res.Data.Items.FirstOrDefault(c => string.Equals(c.Phone?.Trim(), query, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    matched = res.Data.Items.FirstOrDefault();
                }

                if (matched != null)
                {
                    SelectCustomerFromList(matched);
                    ShowSuccess($"Đã chọn: {matched.Name}");
                }
                else
                {
                    SelectedCustomerId = null;
                    SelectedCustomerName = "Khách lẻ";
                    CustomerPhoneNumber = string.Empty;
                    CustomerLoyaltyPoints = 0;
                    CustomerTierName = "Thành viên";
                    CustomerPointRate = 0.01m;
                    CustomerDiscountRate = 0m;
                    CustomerPointRedemptionRate = 1000m;
                    HasSelectedCustomer = false;
                    IsCustomerNotFound = true;
                    ShowWarning("Số điện thoại không tồn tại");
                }
            }
            else
            {
                SelectedCustomerId = null;
                SelectedCustomerName = "Khách lẻ";
                CustomerPhoneNumber = string.Empty;
                CustomerLoyaltyPoints = 0;
                CustomerTierName = "Thành viên";
                CustomerPointRate = 0.01m;
                CustomerDiscountRate = 0m;
                CustomerPointRedemptionRate = 1000m;
                HasSelectedCustomer = false;
                IsCustomerNotFound = true;
                string notFoundMsg = isDigitsOnly ? "Số điện thoại không tồn tại" : "Khách hàng không tồn tại";
                ShowWarning(notFoundMsg);
            }
        }
        catch (Exception ex)
        {
            SelectedCustomerId = null;
            HasSelectedCustomer = false;
            IsCustomerNotFound = true;
            ShowError($"Lỗi tìm kiếm: {ex.Message}");
        }
        finally
        {
            IsSearchingCustomer = false;
            RecalculateTotals();
        }
    }

    private readonly List<MemberTierResponse> _cachedMemberTiers = new();

    public async Task LoadMemberTiersAsync()
    {
        try
        {
            var res = await _customerApiClient.GetMemberTiersAsync();
            if (res?.Success == true && res.Data != null)
            {
                _cachedMemberTiers.Clear();
                _cachedMemberTiers.AddRange(res.Data);
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    [RelayCommand]
    public void SelectCustomerFromList(CustomerSummaryResponse? cust)
    {
        if (cust == null) return;

        SelectedCustomerId = cust.Id;
        SelectedCustomerName = cust.Name;
        CustomerPhoneNumber = cust.Phone;
        CustomerLoyaltyPoints = cust.PointsBalance;
        CustomerTierName = !string.IsNullOrWhiteSpace(cust.MemberTierName) ? cust.MemberTierName : "Thành viên";
        UpdateRatesFromTier(CustomerTierName, cust.MemberTierId);
        HasSelectedCustomer = true;
        IsCustomerNotFound = false;
        ShowQuickRegister = false;
        CustomerSearchResults.Clear();
        CustomerSearchText = string.Empty;
        RecalculateTotals();

        // Lấy thông tin tài khoản hội viên & rate chính xác từ máy chủ
        _ = FetchLoyaltyAccountDetailsAsync(cust.Id);
    }

    [RelayCommand]
    private void QuickSelectCustomer(string customerType)
    {
        SelectedCustomerId = null;
        SelectedCustomerName = "Khách lẻ";
        CustomerPhoneNumber = string.Empty;
        CustomerLoyaltyPoints = 0;
        CustomerTierName = "Thành viên";
        CustomerPointRate = 0.01m;
        CustomerDiscountRate = 0m;
        CustomerPointRedemptionRate = 1000m;
        CustomerSearchText = string.Empty;
        HasSelectedCustomer = false;
        IsCustomerNotFound = false;
        CustomerSearchResults.Clear();
        UseLoyaltyPoints = false;
        ShowQuickRegister = false;
        QuickRegisterError = string.Empty;
        RecalculateTotals();
    }

    private void UpdateRatesFromTier(string? tierName, Guid? tierId = null)
    {
        // 1. Ưu tiên tra cứu trực tiếp từ danh sách cấu hình Member Tiers trong Database (không hardcode)
        var matched = _cachedMemberTiers.FirstOrDefault(t =>
            (tierId.HasValue && t.Id == tierId.Value) ||
            (!string.IsNullOrWhiteSpace(tierName) && string.Equals(t.Name, tierName, StringComparison.OrdinalIgnoreCase)));

        if (matched != null)
        {
            CustomerPointRate = matched.PointRate;
            CustomerDiscountRate = matched.DiscountRate;
            CustomerPointRedemptionRate = matched.PointRedemptionRate > 0 ? matched.PointRedemptionRate : 1000m;
            return;
        }

        // 2. Dự phòng mặc định nếu hệ thống chưa kịp load DB
        CustomerPointRate = 0.01m;
        CustomerDiscountRate = 0m;
        CustomerPointRedemptionRate = 1000m;
    }

    private async Task FetchLoyaltyAccountDetailsAsync(Guid customerId)
    {
        try
        {
            var res = await _customerApiClient.GetLoyaltyAccountAsync(customerId);
            if (res?.Success == true && res.Data != null)
            {
                var account = res.Data;
                CustomerLoyaltyPoints = account.PointsBalance;
                if (!string.IsNullOrWhiteSpace(account.TierName))
                {
                    CustomerTierName = account.TierName;
                }
                if (account.PointRate > 0)
                {
                    CustomerPointRate = account.PointRate;
                }
                if (account.PointRedemptionRate > 0)
                {
                    CustomerPointRedemptionRate = account.PointRedemptionRate;
                }
                CustomerDiscountRate = account.DiscountRate;
                RecalculateTotals();
            }
        }
        catch
        {
            // Bỏ qua nếu offline, giữ cấu hình mặc định theo tier
        }
    }

    [RelayCommand]
    private void ClearSelectedCustomer()
    {
        QuickSelectCustomer("Retail");
    }

    [RelayCommand]
    private void OpenQuickRegister()
    {
        var query = CustomerSearchText?.Trim() ?? string.Empty;
        QuickRegisterError = string.Empty;

        // Nếu chuỗi tìm kiếm gồm các chữ số -> điền sẵn vào Phone, chờ nhập Tên
        if (Regex.IsMatch(query, @"^[0-9]+$"))
        {
            QuickRegisterPhone = query;
            QuickRegisterName = string.Empty;
        }
        else // Nếu chuỗi tìm kiếm là chữ cái -> điền sẵn vào Tên, chờ nhập SĐT
        {
            QuickRegisterPhone = string.Empty;
            QuickRegisterName = query;
        }

        ShowQuickRegister = true;
    }

    [RelayCommand]
    private void CancelQuickRegister()
    {
        ShowQuickRegister = false;
        QuickRegisterError = string.Empty;
    }

    [RelayCommand]
    public async Task CreateQuickCustomerAsync()
    {
        QuickRegisterError = string.Empty;
        var name = QuickRegisterName?.Trim() ?? string.Empty;
        var phone = QuickRegisterPhone?.Trim() ?? string.Empty;

        // 1. Validate Số điện thoại
        if (string.IsNullOrWhiteSpace(phone))
        {
            QuickRegisterError = "Vui lòng nhập số điện thoại";
            ShowError(QuickRegisterError);
            return;
        }

        if (!Regex.IsMatch(phone, @"^0[0-9]{9}$"))
        {
            QuickRegisterError = "Số điện thoại không hợp lệ";
            ShowError(QuickRegisterError);
            return;
        }

        // 2. Validate Tên khách hàng
        if (string.IsNullOrWhiteSpace(name))
        {
            QuickRegisterError = "Vui lòng nhập tên khách hàng";
            ShowError(QuickRegisterError);
            return;
        }

        if (name.Length > 200)
        {
            QuickRegisterError = "Tên khách hàng quá dài";
            ShowError(QuickRegisterError);
            return;
        }

        IsCreatingCustomer = true;

        try
        {
            var req = new CreateCustomerRequest(name, phone);
            var res = await _customerApiClient.CreateCustomerAsync(req);

            if (res?.Success == true && res.Data != null)
            {
                var cust = res.Data;
                SelectedCustomerId = cust.Id;
                SelectedCustomerName = cust.Name;
                CustomerPhoneNumber = cust.Phone;
                CustomerLoyaltyPoints = 0;
                CustomerTierName = "Normal";
                HasSelectedCustomer = true;
                IsCustomerNotFound = false;
                ShowQuickRegister = false;
                QuickRegisterError = string.Empty;
                CustomerSearchText = string.Empty;
                ShowSuccess("Đăng ký thành viên thành công");
            }
            else
            {
                QuickRegisterError = res?.Error?.Message ?? "Số điện thoại đã tồn tại";
                ShowError(QuickRegisterError);
            }
        }
        catch (Exception ex)
        {
            QuickRegisterError = $"Lỗi tạo thành viên: {ex.Message}";
            ShowError(QuickRegisterError);
        }
        finally
        {
            IsCreatingCustomer = false;
            RecalculateTotals();
        }
    }

    partial void OnUseLoyaltyPointsChanged(bool value)
    {
        RecalculateTotals();
    }
}

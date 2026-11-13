using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Config;
using POS.Contracts.V1.Stores;

namespace POS.WinUI.ViewModels.Settings;

public sealed partial class SettingsViewModel
{
    // ── Thông tin cửa hàng ──────────────────────────────────────────
    [ObservableProperty]
    private string _storeName = "Cửa Hàng Trung Tâm";

    [ObservableProperty]
    private string _taxCode = "0101234567";

    [ObservableProperty]
    private string _phone = "0901234567";

    [ObservableProperty]
    private string _businessEmail = "contact@abcmart.vn";

    [ObservableProperty]
    private string _storeAddress = "123 Đường Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh";

    [ObservableProperty]
    private string _businessCategory = "Bán lẻ & Siêu thị mini";

    [ObservableProperty]
    private string _timezone = "Asia/Ho_Chi_Minh";

    [ObservableProperty]
    private string _currencyCode = "VND";

    private void InitializeStoreSettings()
    {
        // Khởi tạo các giá trị mặc định cho mô hình bán lẻ
        BusinessCategory = "Bán lẻ & Siêu thị mini";
        Timezone = "Asia/Ho_Chi_Minh";
        CurrencyCode = "VND";
    }

    public async Task LoadSettingsAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            if (Guid.TryParse(CurrentStoreId, out var storeGuid))
            {
                // 1. Tải thông tin Store từ Backend Store API
                var storeRes = await _storeApi.GetByIdAsync(storeGuid);
                if (storeRes?.Success == true && storeRes.Data is { } s)
                {
                    if (!string.IsNullOrWhiteSpace(s.Name)) StoreName = s.Name;
                    if (!string.IsNullOrWhiteSpace(s.Address)) StoreAddress = s.Address;
                    if (!string.IsNullOrWhiteSpace(s.Phone)) Phone = s.Phone;
                    if (!string.IsNullOrWhiteSpace(s.TaxCode)) TaxCode = s.TaxCode;
                    if (!string.IsNullOrWhiteSpace(s.Timezone)) Timezone = s.Timezone;
                    if (!string.IsNullOrWhiteSpace(s.CurrencyCode)) CurrencyCode = s.CurrencyCode;
                    if (!string.IsNullOrWhiteSpace(s.ReceiptHeader)) ReceiptHeader = s.ReceiptHeader;
                    if (!string.IsNullOrWhiteSpace(s.ReceiptFooter)) ReceiptFooter = s.ReceiptFooter;
                }

                // 2. Tải cấu hình thiết bị từ SystemConfig
                var configRes = await _configApi.GetStoreConfigAsync(storeGuid);
                if (configRes?.Success == true && configRes.Data?.Configs is { } dict)
                {
                    if (dict.TryGetValue("business_email", out var email) && !string.IsNullOrWhiteSpace(email))
                        BusinessEmail = email;

                    if (dict.TryGetValue("printer_model", out var model) && !string.IsNullOrWhiteSpace(model))
                        ApplyPrinterModel(model);

                    if (dict.TryGetValue("printer_port", out var port) && !string.IsNullOrWhiteSpace(port))
                        SelectedPort = port;

                    if (dict.TryGetValue("auto_print", out var autoStr) && bool.TryParse(autoStr, out var autoP))
                        AutoPrintReceipt = autoP;

                    if (dict.TryGetValue("print_copies", out var copiesStr) && int.TryParse(copiesStr, out var copies))
                        PrintCopies = copies;

                    if (dict.TryGetValue("wifi_ssid", out var ssid) && !string.IsNullOrWhiteSpace(ssid))
                        WifiSsid = ssid;

                    if (dict.TryGetValue("wifi_password", out var pwd) && !string.IsNullOrWhiteSpace(pwd))
                        WifiPassword = pwd;
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Không thể tải cấu hình cửa hàng: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAllSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(StoreName))
        {
            ShowError("Tên cửa hàng không được để trống.");
            return;
        }

        if (ReceiptHeader.Length > 300)
        {
            ShowError("Tiêu đề hóa đơn không được vượt quá 300 ký tự.");
            return;
        }

        if (ReceiptFooter.Length > 300)
        {
            ShowError("Chân trang hóa đơn không được vượt quá 300 ký tự.");
            return;
        }

        IsSaving = true;

        try
        {
            if (Guid.TryParse(CurrentStoreId, out var storeGuid))
            {
                // 1. Cập nhật thông tin Store vào Backend (Name, Address, Phone, TaxCode, Timezone, Currency, Header, Footer)
                var storeReq = new UpdateStoreRequest(
                    Name: StoreName.Trim(),
                    Address: StoreAddress?.Trim(),
                    Phone: Phone?.Trim(),
                    Timezone: Timezone,
                    CurrencyCode: CurrencyCode,
                    TaxCode: TaxCode?.Trim(),
                    ReceiptHeader: ReceiptHeader?.Trim(),
                    ReceiptFooter: ReceiptFooter?.Trim()
                );

                var updateStoreRes = await _storeApi.UpdateAsync(storeGuid, storeReq);
                if (updateStoreRes?.Success != true)
                {
                    ShowError(updateStoreRes?.Message ?? "Không thể cập nhật thông tin cửa hàng.");
                    return;
                }

                // 2. Cập nhật các cấu hình thiết bị máy in và WiFi vào SystemConfig
                var configMap = new Dictionary<string, string>
                {
                    ["business_email"] = BusinessEmail?.Trim() ?? string.Empty,
                    ["business_category"] = "Retail",
                    ["printer_model"] = PrinterModel ?? "K80",
                    ["printer_port"] = SelectedPort ?? "USB (COM3)",
                    ["auto_print"] = AutoPrintReceipt.ToString().ToLowerInvariant(),
                    ["print_copies"] = PrintCopies.ToString(),
                    ["wifi_ssid"] = WifiSsid?.Trim() ?? string.Empty,
                    ["wifi_password"] = WifiPassword?.Trim() ?? string.Empty,
                };

                var configRes = await _configApi.UpdateStoreConfigAsync(storeGuid, new UpdateStoreConfigRequest(configMap));
                if (configRes?.Success != true)
                {
                    ShowWarning("Đã lưu thông tin cửa hàng, nhưng cấu hình máy in chưa được lưu vào server.");
                    return;
                }

                ShowSuccess("Lưu toàn bộ thông tin cửa hàng và cấu hình máy in thành công!");
            }
            else
            {
                ShowSuccess("Đã lưu cấu hình thiết bị trên máy trạm.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi lưu cài đặt: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }
}

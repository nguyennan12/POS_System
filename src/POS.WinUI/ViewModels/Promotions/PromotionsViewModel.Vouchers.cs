using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Promotions;

namespace POS.WinUI.ViewModels.Promotions;

public sealed partial class PromotionsViewModel
{
    public ObservableCollection<VoucherResponse> Vouchers { get; } = [];

    [ObservableProperty] private string _voucherSearchCode = string.Empty;
    [ObservableProperty] private bool? _voucherActiveFilter;
    [ObservableProperty] private int _voucherPageNumber = 1;
    [ObservableProperty] private int _voucherPageSize = 15;
    [ObservableProperty] private int _voucherTotalPages = 1;
    [ObservableProperty] private bool _isVoucherEmpty;

    partial void OnVoucherSearchCodeChanged(string value)
    {
        VoucherPageNumber = 1;
        _ = LoadVouchersAsync();
    }

    [RelayCommand]
    private async Task PreviousVoucherPageAsync()
    {
        if (VoucherPageNumber > 1)
        {
            VoucherPageNumber--;
            await LoadVouchersAsync();
        }
    }

    [RelayCommand]
    private async Task NextVoucherPageAsync()
    {
        if (VoucherPageNumber < VoucherTotalPages)
        {
            VoucherPageNumber++;
            await LoadVouchersAsync();
        }
    }

    [RelayCommand]
    private async Task ClearVoucherFiltersAsync()
    {
        VoucherSearchCode = string.Empty;
        VoucherActiveFilter = null;
        VoucherPageNumber = 1;
        await LoadVouchersAsync();
    }

    public async Task LoadVouchersAsync()
    {
        Vouchers.Clear();
        IsLoading = true;
        ListError = null;

        try
        {
            var req = new VoucherFilterRequest(
                Code: string.IsNullOrWhiteSpace(VoucherSearchCode) ? null : VoucherSearchCode.Trim(),
                IsActive: VoucherActiveFilter,
                PageNumber: VoucherPageNumber,
                PageSize: VoucherPageSize
            );

            var vRes = await _api.GetAllVouchersAsync(req);
            if (vRes?.Success == true && vRes.Data != null)
            {
                foreach (var v in vRes.Data.Items)
                {
                    Vouchers.Add(v);
                }

                VoucherTotalPages = Math.Max(1, (int)Math.Ceiling((double)vRes.Data.TotalCount / VoucherPageSize));
                ActiveVouchersCount = vRes.Data.Items.Count(v => v.IsActive);
                TotalVouchersUsed = vRes.Data.Items.Sum(v => v.UsedCount);
                PromotionRevenueEstimate = TotalVouchersUsed * 50000m; // Ước tính doanh thu từ voucher
            }

            IsVoucherEmpty = Vouchers.Count == 0;
        }
        catch (Exception ex)
        {
            ListError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenCreateVoucher()
    {
        var targetPromo = Promotions.FirstOrDefault(p => p.Status == "Active") ?? Promotions.FirstOrDefault();
        if (targetPromo == null)
        {
            ShowError("Vui lòng tạo ít nhất một chương trình khuyến mãi trước khi phát hành mã Voucher.");
            return;
        }

        VoucherCreateDialog = new VoucherCreateDialogViewModel(targetPromo.Id, targetPromo.Name);
    }

    [RelayCommand]
    private async Task SaveVoucherAsync()
    {
        if (VoucherCreateDialog == null || !VoucherCreateDialog.Validate(out var maxUses, out var perCustomerLimit)) return;

        VoucherCreateDialog.IsSaving = true;
        try
        {
            var req = VoucherCreateDialog.ToRequest(maxUses, perCustomerLimit);
            var res = await _api.CreateVoucherAsync(VoucherCreateDialog.PromotionId, req);
            if (res?.Success == true)
            {
                ShowSuccess($"Đã phát hành mã Voucher '{req.Code}' thành công!");
                VoucherCreateDialog = null;
                await LoadVouchersAsync();
            }
            else
            {
                VoucherCreateDialog.ErrorMessage = res?.Message ?? "Không thể phát hành mã Voucher.";
            }
        }
        catch (Exception ex)
        {
            VoucherCreateDialog.ErrorMessage = ex.Message;
        }
        finally
        {
            if (VoucherCreateDialog != null)
                VoucherCreateDialog.IsSaving = false;
        }
    }
}

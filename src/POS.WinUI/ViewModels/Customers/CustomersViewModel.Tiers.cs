using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Customers;

namespace POS.WinUI.ViewModels.Customers;

public sealed partial class CustomersViewModel
{
    public ObservableCollection<MemberTierResponse> MemberTiers { get; } = [];

    [ObservableProperty] private MemberTierResponse? _editingTier;
    [ObservableProperty] private string _tierMinSpendingText = string.Empty;
    [ObservableProperty] private string _tierPointRateText = string.Empty;
    [ObservableProperty] private string _tierDiscountRateText = string.Empty;
    [ObservableProperty] private string? _tierErrorMessage;
    [ObservableProperty] private bool _isTierModalOpen;

    public async Task LoadMemberTiersAsync()
    {
        try
        {
            var res = await _api.GetMemberTiersAsync();
            if (res?.Success == true && res.Data != null)
            {
                MemberTiers.Clear();
                TiersFilterList.Clear();
                TiersFilterList.Add(new MemberTierFilterOption(null, "Tất cả hạng thẻ"));

                foreach (var tier in res.Data)
                {
                    MemberTiers.Add(tier);
                    TiersFilterList.Add(new MemberTierFilterOption(tier.Id, tier.Name));
                }

                if (SelectedTierFilter == null)
                {
                    SelectedTierFilter = TiersFilterList.First();
                }
            }
        }
        catch (Exception ex)
        {
            ListError = ex.Message;
        }
    }

    [RelayCommand]
    private void OpenEditTier(MemberTierResponse tier)
    {
        if (tier == null || !IsOwner) return;

        EditingTier = tier;
        TierMinSpendingText = tier.MinSpending.ToString("N0");
        TierPointRateText = tier.PointRate.ToString("G29");
        TierDiscountRateText = tier.DiscountRate.ToString("G29");
        TierErrorMessage = null;
        IsTierModalOpen = true;
    }

    [RelayCommand]
    private void CloseTierModal()
    {
        IsTierModalOpen = false;
        EditingTier = null;
    }

    [RelayCommand]
    private async Task SaveTierPolicyAsync()
    {
        if (EditingTier == null) return;
        TierErrorMessage = null;

        if (!decimal.TryParse(TierMinSpendingText.Replace(",", "").Trim(), out var minSpending) || minSpending < 0)
        {
            TierErrorMessage = "Hạn mức chi tiêu tối thiểu phải là số không âm (>= 0).";
            return;
        }

        if (!decimal.TryParse(TierPointRateText.Trim(), out var pointRate) || pointRate < 0 || pointRate > 100)
        {
            TierErrorMessage = "Tỷ lệ tích điểm phải nằm trong khoảng từ 0% đến 100%.";
            return;
        }

        if (!decimal.TryParse(TierDiscountRateText.Trim(), out var discountRate) || discountRate < 0 || discountRate > 100)
        {
            TierErrorMessage = "Tỷ lệ chiết khấu giảm giá phải nằm trong khoảng từ 0% đến 100%.";
            return;
        }

        IsLoading = true;
        try
        {
            var req = new UpdateMemberTierRequest(
                MinSpending: minSpending,
                PointRate: pointRate,
                DiscountRate: discountRate,
                DisplayColor: EditingTier.DisplayColor,
                PointRedemptionRate: EditingTier.PointRedemptionRate
            );

            var res = await _api.UpdateMemberTierAsync(EditingTier.Id, req);
            if (res?.Success == true)
            {
                ShowSuccess($"Đã cập nhật chính sách hạng thẻ '{EditingTier.Name}' thành công!");
                CloseTierModal();
                await LoadMemberTiersAsync();
            }
            else
            {
                TierErrorMessage = res?.Message ?? "Không thể cập nhật chính sách hạng thẻ.";
            }
        }
        catch (Exception ex)
        {
            TierErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

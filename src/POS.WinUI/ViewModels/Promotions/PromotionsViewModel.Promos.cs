using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Promotions;

namespace POS.WinUI.ViewModels.Promotions;

public sealed partial class PromotionsViewModel
{
    private CancellationTokenSource? _promoSearchCts;

    public ObservableCollection<PromotionSummaryResponse> Promotions { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _selectedTypeFilter;
    [ObservableProperty] private string? _selectedStatusFilter;
    [ObservableProperty] private int _pageNumber = 1;
    [ObservableProperty] private int _pageSize = 15;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _isPromoEmpty;

    public bool CanPreviousPromoPage => PageNumber > 1 && !IsLoading;
    public bool CanNextPromoPage => PageNumber < TotalPages && !IsLoading;

    partial void OnSearchTextChanged(string value)
    {
        _promoSearchCts?.Cancel();
        _promoSearchCts = new CancellationTokenSource();
        var ct = _promoSearchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, ct);
                if (ct.IsCancellationRequested) return;
                PageNumber = 1;
                await LoadPromotionsAsync(ct);
            }
            catch (OperationCanceledException) { }
        }, ct);
    }

    public async Task LoadPromotionsAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ListError = null;

        try
        {
            var req = new PromotionFilterRequest(
                StoreId: Guid.TryParse(CurrentStoreId, out var sId) ? sId : null,
                Status: SelectedStatusFilter,
                Type: SelectedTypeFilter,
                PageNumber: PageNumber,
                PageSize: PageSize
            );

            var res = await _api.GetPromotionsAsync(req, ct);
            if (res?.Success == true && res.Data != null)
            {
                Promotions.Clear();
                foreach (var p in res.Data.Items)
                {
                    Promotions.Add(p);
                }

                TotalPages = Math.Max(1, (int)Math.Ceiling((double)res.Data.TotalCount / PageSize));
                IsPromoEmpty = Promotions.Count == 0;

                ActivePromotionsCount = Promotions.Count(p => p.Status == "Active");
            }
            else
            {
                ListError = res?.Message ?? "Không thể tải danh sách chương trình khuyến mãi.";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ListError = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanPreviousPromoPage));
            OnPropertyChanged(nameof(CanNextPromoPage));
        }
    }

    [RelayCommand]
    private void ClearPromoFilters()
    {
        SearchText = string.Empty;
        SelectedTypeFilter = null;
        SelectedStatusFilter = null;
        PageNumber = 1;
        _ = LoadPromotionsAsync();
    }

    [RelayCommand]
    private async Task PreviousPromoPageAsync()
    {
        if (CanPreviousPromoPage)
        {
            PageNumber--;
            await LoadPromotionsAsync();
        }
    }

    [RelayCommand]
    private async Task NextPromoPageAsync()
    {
        if (CanNextPromoPage)
        {
            PageNumber++;
            await LoadPromotionsAsync();
        }
    }

    [RelayCommand]
    private void OpenCreatePromotion()
    {
        PromotionFormDialog = new PromotionFormDialogViewModel();
    }

    [RelayCommand]
    private async Task OpenEditPromotionAsync(PromotionSummaryResponse promo)
    {
        if (promo == null) return;
        IsLoading = true;
        try
        {
            var detailRes = await _api.GetPromotionByIdAsync(promo.Id);
            if (detailRes?.Success == true && detailRes.Data != null)
            {
                PromotionFormDialog = new PromotionFormDialogViewModel(detailRes.Data);
            }
            else
            {
                ShowError(detailRes?.Message ?? "Không thể lấy thông tin chi tiết chương trình khuyến mãi.");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SavePromotionAsync()
    {
        if (PromotionFormDialog == null || !PromotionFormDialog.Validate(out var value, out var minOrder, out var maxDiscount)) return;

        PromotionFormDialog.IsSaving = true;
        try
        {
            if (PromotionFormDialog.IsEdit && PromotionFormDialog.Id.HasValue)
            {
                var updateReq = PromotionFormDialog.ToUpdateRequest(value, minOrder, maxDiscount);
                var res = await _api.UpdatePromotionAsync(PromotionFormDialog.Id.Value, updateReq);
                if (res?.Success == true)
                {
                    ShowSuccess("Cập nhật chương trình khuyến mãi thành công!");
                    PromotionFormDialog = null;
                    await LoadPromotionsAsync();
                }
                else
                {
                    PromotionFormDialog.ErrorMessage = res?.Message ?? "Không thể cập nhật chương trình khuyến mãi.";
                }
            }
            else
            {
                var createReq = PromotionFormDialog.ToCreateRequest(value, minOrder, maxDiscount);
                var res = await _api.CreatePromotionAsync(createReq);
                if (res?.Success == true)
                {
                    ShowSuccess("Tạo mới chương trình khuyến mãi thành công!");
                    PromotionFormDialog = null;
                    await LoadPromotionsAsync();
                }
                else
                {
                    PromotionFormDialog.ErrorMessage = res?.Message ?? "Không thể tạo mới chương trình khuyến mãi.";
                }
            }
        }
        catch (Exception ex)
        {
            PromotionFormDialog.ErrorMessage = ex.Message;
        }
        finally
        {
            if (PromotionFormDialog != null)
                PromotionFormDialog.IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task DeletePromotionAsync(PromotionSummaryResponse promo)
    {
        if (promo == null) return;

        var confirmed = await _dialogs.ShowConfirmAsync(new()
        {
            Title = "Ngừng chương trình khuyến mãi",
            Subtitle = "Hành động này sẽ hủy kích hoạt chương trình ưu đãi",
            Message = $"Bạn có chắc chắn muốn ngừng chương trình khuyến mãi '{promo.Name}'?",
            WarningMessage = "Các hóa đơn tại POS sẽ không còn được áp dụng mức chiết khấu của chương trình này nữa.",
            ConfirmText = "Xác nhận ngừng",
            CancelText = "Hủy bỏ",
            Variant = Core.Services.ConfirmDialogVariant.Danger
        });

        if (!confirmed) return;

        IsLoading = true;
        try
        {
            var res = await _api.DeletePromotionAsync(promo.Id);
            if (res?.Success == true)
            {
                ShowSuccess($"Đã ngừng chương trình '{promo.Name}'.");
                await LoadPromotionsAsync();
            }
            else
            {
                ShowError(res?.Message ?? "Không thể ngừng chương trình khuyến mãi.");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenIssueVoucherForPromo(PromotionSummaryResponse promo)
    {
        if (promo == null) return;
        VoucherCreateDialog = new VoucherCreateDialogViewModel(promo.Id, promo.Name);
    }
}

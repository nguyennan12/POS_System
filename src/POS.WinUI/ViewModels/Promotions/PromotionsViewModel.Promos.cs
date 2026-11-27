using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Categories;
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
    private async Task OpenCreatePromotionAsync(CancellationToken ct)
    {
        if (PromotionStoreId is not Guid storeId)
        {
            ShowError("Chọn chi nhánh trước khi tạo chương trình khuyến mãi.");
            return;
        }

        var dialog = new PromotionFormDialogViewModel(catalogStoreId: storeId);
        PromotionFormDialog = dialog;
        await LoadPromotionCatalogAsync(dialog, ct);
    }

    [RelayCommand]
    private async Task OpenEditPromotionAsync(PromotionSummaryResponse promo, CancellationToken ct)
    {
        if (promo == null) return;
        var storeId = PromotionStoreId;
        PromotionFormDialogViewModel? dialog = null;
        IsLoading = true;
        try
        {
            var detailRes = await _api.GetPromotionByIdAsync(promo.Id, ct);
            if (ct.IsCancellationRequested || storeId != PromotionStoreId)
                return;

            if (detailRes?.Success == true && detailRes.Data is { } detail)
            {
                if (detail.StoreId != Guid.Empty && detail.StoreId != storeId)
                {
                    ShowError("Chương trình khuyến mãi thuộc chi nhánh khác.");
                    return;
                }

                dialog = new PromotionFormDialogViewModel(detail, storeId);
                PromotionFormDialog = dialog;
            }
            else
            {
                ShowError(detailRes?.Message ?? "Không thể lấy thông tin chi tiết chương trình khuyến mãi.");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }

        if (dialog is not null)
            await LoadPromotionCatalogAsync(dialog, ct);
    }

    private Guid? PromotionStoreId =>
        Guid.TryParse(CurrentStoreId, out var storeId) && storeId != Guid.Empty ? storeId : null;

    private async Task LoadPromotionCatalogAsync(PromotionFormDialogViewModel dialog, CancellationToken ct)
    {
        if (dialog.CatalogStoreId is not Guid storeId)
        {
            dialog.CatalogMessage = "Chọn chi nhánh để tải danh mục và SKU.";
            return;
        }

        dialog.IsLoadingCatalog = true;
        try
        {
            var errors = await Task.WhenAll(
                LoadPromotionCategoriesAsync(dialog, storeId, ct), LoadPromotionSkusAsync(dialog, storeId, ct));
            dialog.CatalogMessage = string.Join(" ", errors.OfType<string>());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        finally
        {
            dialog.IsLoadingCatalog = false;
        }
    }

    private async Task<string?> LoadPromotionCategoriesAsync(
        PromotionFormDialogViewModel dialog, Guid storeId, CancellationToken ct)
    {
        try
        {
            var response = await _categoryApi.GetCategoriesTreeAsync(storeId, ct);
            if (response?.Success == true && response.Data is { } categories)
            {
                dialog.SetCategoryCatalog(FlattenPromotionCategories(categories));
                return null;
            }
        }
        catch (Exception) when (!ct.IsCancellationRequested) { }

        return "Không thể tải danh mục của chi nhánh.";
    }

    private static IEnumerable<PromotionTargetOption> FlattenPromotionCategories(
        IEnumerable<CategoryResponse> categories, string? parent = null)
    {
        foreach (var category in categories)
        {
            var displayName = parent is null ? category.Name : $"{parent} / {category.Name}";
            yield return new PromotionTargetOption(category.Id, displayName);

            if (category.SubCategories is { } children)
            {
                foreach (var child in FlattenPromotionCategories(children, displayName))
                    yield return child;
            }
        }
    }

    private async Task<string?> LoadPromotionSkusAsync(
        PromotionFormDialogViewModel dialog, Guid storeId, CancellationToken ct)
    {
        const string loadError = "Không thể tải SKU của chi nhánh.";
        var options = new List<PromotionTargetOption>();
        try
        {
            for (var page = 1; ; page++)
            {
                var response = await _productApi.GetPosCatalogAsync(pageNumber: page, pageSize: 200, storeId: storeId, ct: ct);
                if (response?.Success != true || response.Data is not { } data)
                    return loadError;

                options.AddRange(data.Items.Select(sku => new PromotionTargetOption(
                    sku.SkuId, $"{sku.ProductName} ({sku.SkuCode})")));
                if (!data.HasNextPage)
                    break;
            }

            dialog.SetSkuCatalog(options);
            return null;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return loadError;
        }
    }

    [RelayCommand]
    private async Task SavePromotionAsync()
    {
        var dialog = PromotionFormDialog;
        if (dialog is null)
            return;
        if (dialog.CatalogStoreId != PromotionStoreId)
        {
            dialog.ErrorMessage = "Chi nhánh đã thay đổi. Mở lại biểu mẫu trước khi lưu.";
            return;
        }

        dialog.IsSaving = true;
        try
        {
            if (dialog.Id is Guid id)
            {
                if (!dialog.TryUpdateRequest(out var updateReq))
                    return;

                var res = await _api.UpdatePromotionAsync(id, updateReq);
                if (res?.Success == true)
                {
                    ShowSuccess("Cập nhật chương trình khuyến mãi thành công!");
                    if (ReferenceEquals(PromotionFormDialog, dialog))
                        CloseModal();
                    await LoadPromotionsAsync();
                }
                else
                {
                    dialog.ErrorMessage = res?.Message ?? "Không thể cập nhật chương trình khuyến mãi.";
                }
            }
            else
            {
                if (!dialog.TryCreateRequest(out var createReq))
                    return;

                var res = await _api.CreatePromotionAsync(createReq);
                if (res?.Success == true)
                {
                    ShowSuccess("Tạo mới chương trình khuyến mãi thành công!");
                    if (ReferenceEquals(PromotionFormDialog, dialog))
                        CloseModal();
                    await LoadPromotionsAsync();
                }
                else
                {
                    dialog.ErrorMessage = res?.Message ?? "Không thể tạo mới chương trình khuyến mãi.";
                }
            }
        }
        catch (Exception ex)
        {
            dialog.ErrorMessage = ex.Message;
        }
        finally
        {
            dialog.IsSaving = false;
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

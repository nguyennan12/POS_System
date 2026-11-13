using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Customers;

namespace POS.WinUI.ViewModels.Customers;

public record MemberTierFilterOption(Guid? Id, string Name);

public sealed partial class CustomersViewModel
{
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<CustomerSummaryResponse> Customers { get; } = [];
    public ObservableCollection<MemberTierFilterOption> TiersFilterList { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private MemberTierFilterOption? _selectedTierFilter;
    [ObservableProperty] private int _pageNumber = 1;
    [ObservableProperty] private int _pageSize = 15;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _isEmpty;

    public bool CanPreviousPage => PageNumber > 1 && !IsLoading;
    public bool CanNextPage => PageNumber < TotalPages && !IsLoading;

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, ct);
                if (ct.IsCancellationRequested) return;
                PageNumber = 1;
                await LoadCustomersAsync(ct);
            }
            catch (OperationCanceledException) { }
        }, ct);
    }

    partial void OnSelectedTierFilterChanged(MemberTierFilterOption? value)
    {
        PageNumber = 1;
        _ = LoadCustomersAsync();
    }

    public async Task LoadCustomersAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ListError = null;
        try
        {
            var isPhone = !string.IsNullOrWhiteSpace(SearchText) && SearchText.Trim().All(char.IsDigit);
            var phoneParam = isPhone ? SearchText.Trim() : null;
            var nameParam = !isPhone && !string.IsNullOrWhiteSpace(SearchText) ? SearchText.Trim() : null;

            var response = await _api.GetCustomersAsync(
                phone: phoneParam,
                name: nameParam,
                memberTierId: SelectedTierFilter?.Id,
                pageNumber: PageNumber,
                pageSize: PageSize,
                ct: ct
            );

            if (response?.Success == true && response.Data != null)
            {
                Customers.Clear();
                foreach (var c in response.Data.Items)
                {
                    Customers.Add(c);
                }

                TotalCustomersCount = response.Data.TotalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)response.Data.TotalCount / PageSize));
                IsEmpty = Customers.Count == 0;

                // Update KPI calculations
                TotalLoyaltyPoints = Customers.Sum(c => c.PointsBalance);
                var activeCount = Customers.Count(c => c.IsActive);
                ActiveCustomersRate = Customers.Count > 0 ? $"{Math.Round((double)activeCount / Customers.Count * 100, 1)}%" : "100%";
                NewCustomersThisMonth = Customers.Count(c => c.CreatedAt >= new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero));
            }
            else
            {
                ListError = response?.Message ?? "Không thể nạp danh sách khách hàng.";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ListError = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanPreviousPage));
            OnPropertyChanged(nameof(CanNextPage));
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedTierFilter = TiersFilterList.FirstOrDefault(t => t.Id == null);
        PageNumber = 1;
        _ = LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CanPreviousPage)
        {
            PageNumber--;
            await LoadCustomersAsync();
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CanNextPage)
        {
            PageNumber++;
            await LoadCustomersAsync();
        }
    }

    [RelayCommand]
    private void OpenCreateCustomer()
    {
        CustomerFormDialog = new CustomerFormDialogViewModel();
    }

    [RelayCommand]
    private async Task OpenEditCustomerAsync(CustomerSummaryResponse customer)
    {
        if (customer == null) return;
        IsLoading = true;
        try
        {
            var detailRes = await _api.GetCustomerByIdAsync(customer.Id);
            if (detailRes?.Success == true && detailRes.Data != null)
            {
                CustomerFormDialog = new CustomerFormDialogViewModel(detailRes.Data);
            }
            else
            {
                ShowError(detailRes?.Message ?? "Không thể lấy thông tin chi tiết khách hàng.");
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
    private async Task SaveCustomerAsync()
    {
        if (CustomerFormDialog == null || !CustomerFormDialog.Validate()) return;

        CustomerFormDialog.IsSaving = true;
        try
        {
            if (CustomerFormDialog.IsEdit && CustomerFormDialog.Id.HasValue)
            {
                var updateReq = CustomerFormDialog.ToUpdateRequest();
                var res = await _api.UpdateCustomerAsync(CustomerFormDialog.Id.Value, updateReq);
                if (res?.Success == true)
                {
                    ShowSuccess("Cập nhật thông tin khách hàng thành công!");
                    CustomerFormDialog = null;
                    await LoadCustomersAsync();
                }
                else
                {
                    CustomerFormDialog.ErrorMessage = res?.Message ?? "Không thể cập nhật khách hàng.";
                }
            }
            else
            {
                var createReq = CustomerFormDialog.ToCreateRequest();
                var res = await _api.CreateCustomerAsync(createReq);
                if (res?.Success == true)
                {
                    ShowSuccess("Tạo mới hồ sơ khách hàng thành công!");
                    CustomerFormDialog = null;
                    await LoadCustomersAsync();
                }
                else
                {
                    CustomerFormDialog.ErrorMessage = res?.Message ?? "Không thể tạo mới khách hàng.";
                }
            }
        }
        catch (Exception ex)
        {
            CustomerFormDialog.ErrorMessage = ex.Message;
        }
        finally
        {
            if (CustomerFormDialog != null)
                CustomerFormDialog.IsSaving = false;
        }
    }

    [RelayCommand]
    private void OpenAdjustPoints(CustomerSummaryResponse customer)
    {
        if (customer == null) return;
        PointAdjustDialog = new PointAdjustDialogViewModel(customer.Id, customer.Name, customer.PointsBalance);
    }

    [RelayCommand]
    private async Task SubmitAdjustPointsAsync()
    {
        if (PointAdjustDialog == null || !PointAdjustDialog.Validate(out var points)) return;

        PointAdjustDialog.IsSaving = true;
        try
        {
            var req = PointAdjustDialog.ToRequest(points);
            var res = await _api.AdjustPointsAsync(PointAdjustDialog.CustomerId, req);
            if (res?.Success == true)
            {
                ShowSuccess($"Đã điều chỉnh {(PointAdjustDialog.IsAccrue ? "cộng" : "trừ")} {points:N0} điểm cho khách hàng '{PointAdjustDialog.CustomerName}'.");
                PointAdjustDialog = null;
                await LoadCustomersAsync();
            }
            else
            {
                PointAdjustDialog.ErrorMessage = res?.Message ?? "Không thể điều chỉnh điểm.";
            }
        }
        catch (Exception ex)
        {
            PointAdjustDialog.ErrorMessage = ex.Message;
        }
        finally
        {
            if (PointAdjustDialog != null)
                PointAdjustDialog.IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(CustomerSummaryResponse customer)
    {
        if (customer == null) return;

        var confirmed = await _dialogs.ShowConfirmAsync(new()
        {
            Title = "Ngừng hoạt động khách hàng",
            Subtitle = "Hành động này sẽ tạm ngừng tài khoản tích điểm",
            Message = $"Bạn có chắc chắn muốn ngừng hoạt động khách hàng '{customer.Name}' ({customer.Phone})?",
            WarningMessage = "Khách hàng sẽ không thể tích điểm hoặc hưởng ưu đãi giảm giá tại POS cho đến khi được kích hoạt lại.",
            ConfirmText = "Xác nhận ngừng",
            CancelText = "Hủy bỏ",
            Variant = Core.Services.ConfirmDialogVariant.Danger
        });

        if (!confirmed) return;

        IsLoading = true;
        try
        {
            var res = await _api.DeleteCustomerAsync(customer.Id);
            if (res?.Success == true)
            {
                ShowSuccess($"Đã ngừng hoạt động khách hàng '{customer.Name}'.");
                await LoadCustomersAsync();
            }
            else
            {
                ShowError(res?.Message ?? "Không thể ngừng hoạt động khách hàng.");
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
}

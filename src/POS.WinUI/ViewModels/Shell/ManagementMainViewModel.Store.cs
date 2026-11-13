using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.Models;

namespace POS.WinUI.ViewModels.Shell;

public partial class ManagementMainViewModel
{
    public async Task LoadStoresAsync()
    {
        if (!IsOwnerRole) return;
        IsLoadingStores = true;
        try
        {
            var res = await _storeApiClient.GetPublicStoresAsync();
            if (res?.Success == true && res.Data != null)
            {
                Stores.Clear();
                // 1. Thêm "Tất cả chi nhánh" cho Chủ chuỗi (Owner)
                var allStoreItem = new StoreItem { Id = Guid.Empty, Name = "Tất cả chi nhánh" };
                Stores.Add(allStoreItem);

                StoreItem? currentSelected = null;
                foreach (var s in res.Data)
                {
                    var item = new StoreItem { Id = s.Id, Name = s.Name };
                    Stores.Add(item);
                    if (!string.IsNullOrWhiteSpace(_sessionService.StoreId) &&
                        s.Id.ToString().Equals(_sessionService.StoreId, StringComparison.OrdinalIgnoreCase))
                    {
                        currentSelected = item;
                    }
                }

                if (currentSelected != null)
                {
                    SelectedStore = currentSelected;
                }
                else
                {
                    // Mặc định Owner chọn "Tất cả chi nhánh" để thấy dữ liệu toàn chuỗi
                    SelectedStore = allStoreItem;
                }
            }
        }
        catch
        {
            // Network handler covers errors
        }
        finally
        {
            IsLoadingStores = false;
        }
    }

    partial void OnSelectedStoreChanged(StoreItem? value)
    {
        if (value == null) return;
        var newStoreId = value.Id == Guid.Empty ? string.Empty : value.Id.ToString();
        if (string.Equals(_sessionService.StoreId, newStoreId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(StoreName, value.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _sessionService.SetStore(newStoreId, value.Name);
        StoreName = value.Name;

        // Xóa cache các tab để nạp lại dữ liệu cho chi nhánh mới
        _tabViewModelCache.Clear();
        if (SelectedTab != null)
        {
            SelectTab(SelectedTab);
        }

        _ = LoadCurrentShiftAsync();
    }

    [RelayCommand]
    public async Task LoadCurrentShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(_sessionService.StoreId) || !Guid.TryParse(_sessionService.StoreId, out var storeId))
        {
            ShiftName = "Chưa mở ca";
            CurrentShiftId = null;
            return;
        }

        IsLoadingShift = true;
        try
        {
            var res = await _shiftApiClient.GetCurrentShiftAsync(storeId);
            if (res?.Success == true && res.Data != null)
            {
                var shift = res.Data;
                CurrentShiftId = shift.ShiftId;
                _sessionService.SetShift(shift.ShiftId.ToString(), "Ca đang mở");
                ShiftName = "Đang trong ca";
            }
            else
            {
                CurrentShiftId = null;
                ShiftName = "Chưa mở ca";
            }
        }
        catch
        {
            CurrentShiftId = null;
            ShiftName = "Chưa mở ca";
        }
        finally
        {
            IsLoadingShift = false;
        }
    }
}

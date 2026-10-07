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
                StoreItem? currentSelected = null;
                foreach (var s in res.Data)
                {
                    var item = new StoreItem { Id = s.Id, Name = s.Name };
                    Stores.Add(item);
                    if (s.Id.ToString().Equals(_sessionService.StoreId, StringComparison.OrdinalIgnoreCase))
                    {
                        currentSelected = item;
                    }
                }

                if (currentSelected != null)
                {
                    SelectedStore = currentSelected;
                }
                else if (Stores.Count > 0)
                {
                    SelectedStore = Stores[0];
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
        if (string.Equals(_sessionService.StoreId, value.Id.ToString(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(StoreName, value.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _sessionService.SetStore(value.Id.ToString(), value.Name);
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

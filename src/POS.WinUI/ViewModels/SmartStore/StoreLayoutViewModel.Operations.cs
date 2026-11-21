using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.StoreLayouts;

namespace POS.WinUI.ViewModels.SmartStore;

public sealed partial class StoreLayoutViewModel
{
    // ── Create Layout ──
    [RelayCommand]
    public void OpenCreateDialog()
    {
        NewLayoutName = $"Mặt bằng v{Layouts.Count + 1}";
        NewLayoutWidth = 20;
        NewLayoutHeight = 15;
        NewLayoutCopyFromActive = false;
        IsCreateDialogOpen = true;
    }

    [RelayCommand]
    public void CloseCreateDialog() => IsCreateDialogOpen = false;

    [RelayCommand]
    public async Task CreateLayoutAsync()
    {
        if (string.IsNullOrWhiteSpace(NewLayoutName))
        {
            ShowError("Tên mặt bằng không được để trống.");
            return;
        }

        if (NewLayoutWidth < 4 || NewLayoutWidth > 100)
        {
            ShowError("Chiều rộng phải nằm trong khoảng từ 4m đến 100m.");
            return;
        }

        if (NewLayoutHeight < 4 || NewLayoutHeight > 100)
        {
            ShowError("Chiều dài phải nằm trong khoảng từ 4m đến 100m.");
            return;
        }

        IsLoading = true;
        try
        {
            var storeGuid = Guid.TryParse(CurrentStoreId, out var g) ? g : (Guid?)null;
            var req = new CreateStoreLayoutRequest(
                NewLayoutName.Trim(),
                NewLayoutWidth,
                NewLayoutHeight,
                storeGuid);

            var res = await _layoutClient.CreateAsync(req);
            if (res?.Success == true && res.Data != null)
            {
                ShowSuccess($"Đã tạo bản thảo mặt bằng: {res.Data.Name}");
                IsCreateDialogOpen = false;
                await LoadLayoutsAsync();
                var created = Layouts.FirstOrDefault(l => l.Id == res.Data.Id);
                if (created != null)
                {
                    await SelectLayoutAsync(created);
                    SwitchMode("Design");
                }
            }
            else
            {
                ShowError(res?.Message ?? "Tạo mặt bằng thất bại.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Publish Layout ──
    [RelayCommand]
    public void OpenPublishConfirm()
    {
        if (!CanPublishCurrentLayout)
        {
            ShowWarning("Chỉ có thể kích hoạt mặt bằng ở trạng thái Bản thảo (Draft).");
            return;
        }
        IsPublishConfirmOpen = true;
    }

    [RelayCommand]
    public void ClosePublishConfirm() => IsPublishConfirmOpen = false;

    [RelayCommand]
    public async Task PublishLayoutAsync()
    {
        if (CurrentLayout == null) return;
        IsLoading = true;
        try
        {
            var req = new PublishStoreLayoutRequest(CurrentLayout.Revision);
            var res = await _layoutClient.PublishAsync(CurrentLayout.Id, req);
            if (res?.Success == true && res.Data != null)
            {
                ShowSuccess($"Đã kích hoạt mặt bằng {res.Data.Name} thành công!");
                IsPublishConfirmOpen = false;
                await LoadLayoutsAsync();
                var published = Layouts.FirstOrDefault(l => l.Id == res.Data.Id);
                if (published != null)
                {
                    await SelectLayoutAsync(published);
                }
                SwitchMode("Map");
            }
            else
            {
                ShowError(res?.Message ?? "Kích hoạt mặt bằng thất bại.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi kích hoạt: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Archive Layout ──
    [RelayCommand]
    public void OpenArchiveConfirm()
    {
        if (CurrentLayout == null || CurrentLayout.IsArchived)
        {
            ShowWarning("Mặt bằng này đã được lưu trữ.");
            return;
        }
        IsArchiveConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseArchiveConfirm() => IsArchiveConfirmOpen = false;

    [RelayCommand]
    public async Task ArchiveLayoutAsync()
    {
        if (CurrentLayout == null) return;
        IsLoading = true;
        try
        {
            var req = new ArchiveStoreLayoutRequest(CurrentLayout.Revision);
            var res = await _layoutClient.ArchiveAsync(CurrentLayout.Id, req);
            if (res?.Success == true && res.Data != null)
            {
                ShowSuccess($"Đã lưu trữ mặt bằng {res.Data.Name}.");
                IsArchiveConfirmOpen = false;
                await LoadLayoutsAsync();
            }
            else
            {
                ShowError(res?.Message ?? "Lưu trữ mặt bằng thất bại.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Clone Layout ──
    [RelayCommand]
    public void OpenCloneDialog()
    {
        if (CurrentLayout == null) return;
        CloneLayoutName = $"{CurrentLayout.Name} (Bản sao)";
        CloneTargetStoreId = CurrentLayout.StoreId;
        CloneCopyPlacements = false;
        IsCloneDialogOpen = true;
    }

    [RelayCommand]
    public void CloseCloneDialog() => IsCloneDialogOpen = false;

    [RelayCommand]
    public async Task CloneLayoutAsync()
    {
        if (CurrentLayout == null) return;
        if (string.IsNullOrWhiteSpace(CloneLayoutName))
        {
            ShowError("Tên bản sao không được để trống.");
            return;
        }

        IsLoading = true;
        try
        {
            var targetStore = CloneTargetStoreId ?? CurrentLayout.StoreId;
            var req = new CloneStoreLayoutRequest(targetStore, CloneLayoutName.Trim(), CloneCopyPlacements, CurrentLayout.Revision);
            var res = await _layoutClient.CloneAsync(CurrentLayout.Id, req);
            if (res?.Success == true && res.Data != null)
            {
                ShowSuccess($"Nhân bản mặt bằng thành công: {CloneLayoutName}");
                IsCloneDialogOpen = false;
                await LoadLayoutsAsync();
                var cloned = Layouts.FirstOrDefault(l => l.Id == res.Data.NewLayoutId);
                if (cloned != null)
                {
                    await SelectLayoutAsync(cloned);
                    SwitchMode("Design");
                }
            }
            else
            {
                ShowError(res?.Message ?? "Nhân bản mặt bằng thất bại.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi nhân bản: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

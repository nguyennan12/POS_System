using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.ViewModels.SmartStore.Models;

namespace POS.WinUI.ViewModels.SmartStore;

public sealed partial class StoreLayoutViewModel
{
    async partial void OnSelectedLayoutSummaryChanged(StoreLayoutSummaryItem? value)
    {
        if (value != null && (CurrentLayout == null || CurrentLayout.Id != value.Id))
        {
            await SelectLayoutAsync(value);
        }
    }

    [RelayCommand]
    public async Task LoadLayoutsAsync()
    {
        IsLoading = true;
        try
        {
            var storeGuid = Guid.TryParse(CurrentStoreId, out var g) ? g : (Guid?)null;
            var res = await _layoutClient.GetLayoutsAsync(storeId: storeGuid, pageNumber: 1, pageSize: 50);

            Layouts.Clear();
            if (res?.Success == true && res.Data?.Items != null)
            {
                foreach (var item in res.Data.Items)
                {
                    Layouts.Add(new StoreLayoutSummaryItem
                    {
                        Id = item.Id,
                        StoreId = item.StoreId,
                        VersionNumber = item.VersionNumber,
                        Name = item.Name,
                        GridWidth = item.GridWidth,
                        GridHeight = item.GridHeight,
                        Status = item.Status.ToString(),
                        Revision = item.Revision,
                        CreatedAt = item.CreatedAt,
                        PublishedAt = item.PublishedAt
                    });
                }
            }

            // Tự động chọn layout Active, hoặc layout đầu tiên
            var active = Layouts.FirstOrDefault(l => l.IsActive) ?? Layouts.FirstOrDefault();
            if (active != null)
            {
                await SelectLayoutAsync(active);
            }
            else
            {
                CurrentLayout = null;
                ActiveMode = "Map";
                MapVm.Cells.Clear();
                MapVm.Zones.Clear();
                MapVm.Shelves.Clear();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Không thể tải danh sách mặt bằng: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectLayoutAsync(StoreLayoutSummaryItem? item)
    {
        if (item == null) return;
        if (SelectedLayoutSummary != item)
        {
            SelectedLayoutSummary = item;
        }

        IsLoading = true;
        try
        {
            var res = await _layoutClient.GetByIdAsync(item.Id);
            if (res?.Success == true && res.Data != null)
            {
                ApplyLayoutDetail(res.Data);
            }
            else
            {
                ShowError(res?.Message ?? "Không thể lấy chi tiết mặt bằng.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi tải chi tiết mặt bằng: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ReloadCurrentLayoutAsync()
    {
        if (CurrentLayout == null) return;
        IsLoading = true;
        try
        {
            var res = await _layoutClient.GetByIdAsync(CurrentLayout.Id);
            if (res?.Success == true && res.Data != null)
            {
                ApplyLayoutDetail(res.Data);
                ShowSuccess($"Đã tải lại sơ đồ mới nhất ({CurrentLayout.Name} - Rev #{CurrentLayout.Revision}).");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Không thể tải lại sơ đồ: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ReloadCurrentLayoutQuietAsync()
    {
        if (CurrentLayout == null) return;
        try
        {
            var res = await _layoutClient.GetByIdAsync(CurrentLayout.Id);
            if (res?.Success == true && res.Data != null)
            {
                ApplyLayoutDetail(res.Data);
            }
        }
        catch
        {
            // Ignore background reload errors
        }
    }

    private void ApplyLayoutDetail(StoreLayoutDetailResponse d)
    {
        var model = new StoreLayoutDetailModel
        {
            Id = d.Id,
            StoreId = d.StoreId,
            VersionNumber = d.VersionNumber,
            Name = d.Name,
            GridWidth = d.GridWidth,
            GridHeight = d.GridHeight,
            Status = d.Status.ToString(),
            Revision = d.Revision,
            CreatedBy = d.CreatedBy,
            CreatedAt = d.CreatedAt,
            PublishedBy = d.PublishedBy,
            PublishedAt = d.PublishedAt
        };

        foreach (var z in d.Zones)
        {
            model.Zones.Add(new StoreZoneModel
            {
                Id = z.Id,
                LayoutId = z.LayoutId,
                Code = z.Code,
                Name = z.Name,
                Color = z.Color,
                PositionX = z.PositionX,
                PositionY = z.PositionY,
                Width = z.Width,
                Height = z.Height
            });
        }

        foreach (var c in d.Cells)
        {
            model.Cells.Add(new StoreLayoutCellModel
            {
                Id = c.Id,
                X = c.X,
                Y = c.Y,
                CellType = c.CellType.ToString(),
                TraversalCost = c.TraversalCost
            });
        }

        foreach (var s in d.Shelves)
        {
            var shelfModel = new ShelfModel
            {
                Id = s.Id,
                ZoneId = s.ZoneId,
                Code = s.Code,
                Name = s.Name,
                PositionX = s.PositionX,
                PositionY = s.PositionY,
                Width = s.Width,
                Height = s.Height,
                Orientation = s.Orientation.ToString(),
                Status = s.Status.ToString()
            };

            foreach (var lv in s.Levels)
            {
                shelfModel.Levels.Add(new ShelfLevelModel
                {
                    Id = lv.Id,
                    ShelfId = lv.ShelfId,
                    LevelNumber = lv.LevelNumber,
                    Label = lv.Label,
                    SortOrder = lv.SortOrder
                });
            }

            model.Shelves.Add(shelfModel);
        }

        CurrentLayout = model;
        var summary = Layouts.FirstOrDefault(l => l.Id == model.Id);
        if (summary != null)
        {
            summary.Revision = model.Revision;
        }

        OnPropertyChanged(nameof(CanEditCurrentLayout));
        OnPropertyChanged(nameof(CanPublishCurrentLayout));

        // Load geometry vào Canvas Map
        MapVm.LoadLayoutGeometry(model);

        // Đồng bộ sub-viewmodels
        EditorVm.Initialize(model);
        PlacementVm.Initialize(model);
        PickingVm.Initialize(model);
    }

    protected override void OnStoreChanged(string storeId, string? storeName)
    {
        CurrentLayout = null;
        Layouts.Clear();
        MapVm.Cells.Clear();
        MapVm.Zones.Clear();
        MapVm.Shelves.Clear();
        MapVm.ClearRoute();
        _ = LoadLayoutsAsync();
    }
}

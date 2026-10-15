using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Categories;

namespace POS.WinUI.ViewModels.Products;

public partial class ProductsViewModel
{
    // ── Trạng thái Modal Quản Lý Danh Mục ──
    [ObservableProperty]
    private bool _isCategoryModalOpen = false;

    [ObservableProperty]
    private bool _isCategoryEditMode = false;

    public string CategoryModalTitle => IsCategoryEditMode ? "Chỉnh sửa danh mục" : "Thêm mới danh mục hàng";

    [ObservableProperty]
    private Guid _categoryEditId = Guid.Empty;

    [ObservableProperty]
    private string _categoryEditName = string.Empty;

    [ObservableProperty]
    private CategoryItemModel? _categoryEditSelectedParent;

    [ObservableProperty]
    private int _categoryEditDisplayOrder = 1;

    [ObservableProperty]
    private bool _categoryEditIsVisible = true;

    [ObservableProperty]
    private string _categoryEditErrorMessage = string.Empty;

    [RelayCommand]
    public void OpenCreateCategoryModal()
    {
        IsCategoryEditMode = false;
        CategoryEditId = Guid.Empty;
        CategoryEditName = string.Empty;
        CategoryEditSelectedParent = null;
        CategoryEditDisplayOrder = CategoriesTree.Count + 1;
        CategoryEditIsVisible = true;
        CategoryEditErrorMessage = string.Empty;

        OnPropertyChanged(nameof(CategoryModalTitle));
        IsCategoryModalOpen = true;
    }

    [RelayCommand]
    public void OpenEditCategoryModal(CategoryItemModel? cat)
    {
        if (cat == null) return;

        IsCategoryEditMode = true;
        CategoryEditId = cat.Id;
        CategoryEditName = cat.Name;
        CategoryEditDisplayOrder = cat.DisplayOrder;
        CategoryEditIsVisible = cat.IsVisible;
        CategoryEditErrorMessage = string.Empty;

        CategoryEditSelectedParent = CategoryFilterOptions.FirstOrDefault(c => c.Id == cat.ParentId);

        OnPropertyChanged(nameof(CategoryModalTitle));
        IsCategoryModalOpen = true;
    }

    [RelayCommand]
    public void CloseCategoryModal()
    {
        IsCategoryModalOpen = false;
    }

    [RelayCommand]
    public async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(CategoryEditName))
        {
            CategoryEditErrorMessage = "Vui lòng nhập tên danh mục.";
            return;
        }

        Guid? parentId = (CategoryEditSelectedParent != null && CategoryEditSelectedParent.Id != Guid.Empty && CategoryEditSelectedParent.Id != CategoryEditId)
            ? CategoryEditSelectedParent.Id
            : null;

        CategoryEditErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            if (!IsCategoryEditMode)
            {
                var req = new CreateCategoryRequest(
                    Name: CategoryEditName.Trim(),
                    ParentId: parentId,
                    DisplayOrder: CategoryEditDisplayOrder,
                    ImageUrl: null,
                    IsVisible: CategoryEditIsVisible
                );

                var res = await _categoryApi.CreateCategoryAsync(req);
                if (res != null && res.Success)
                {
                    ShowStatus($"Tạo danh mục '{CategoryEditName}' thành công!");
                    CloseCategoryModal();
                    await LoadCategoriesAsync();
                }
                else
                {
                    CategoryEditErrorMessage = res?.Error?.Message ?? "Không thể tạo danh mục.";
                }
            }
            else
            {
                var req = new UpdateCategoryRequest(
                    Name: CategoryEditName.Trim(),
                    ParentId: parentId,
                    DisplayOrder: CategoryEditDisplayOrder,
                    ImageUrl: null,
                    IsVisible: CategoryEditIsVisible
                );

                var res = await _categoryApi.UpdateCategoryAsync(CategoryEditId, req);
                if (res != null && res.Success)
                {
                    ShowStatus($"Cập nhật danh mục '{CategoryEditName}' thành công!");
                    CloseCategoryModal();
                    await LoadCategoriesAsync();
                }
                else
                {
                    CategoryEditErrorMessage = res?.Error?.Message ?? "Không thể cập nhật danh mục.";
                }
            }
        }
        catch (Exception ex)
        {
            CategoryEditErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteCategoryAsync(CategoryItemModel? cat)
    {
        if (cat == null) return;

        IsLoading = true;
        try
        {
            var res = await _categoryApi.DeleteCategoryAsync(cat.Id);
            if (res != null && res.Success)
            {
                ShowStatus($"Đã xóa danh mục '{cat.Name}'.");
                await LoadCategoriesAsync();
            }
            else
            {
                ShowStatus(res?.Error?.Message ?? "Không thể xóa danh mục. Có thể đang có sản phẩm thuộc danh mục này.", isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi khi xóa danh mục: {ex.Message}", isError: true);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

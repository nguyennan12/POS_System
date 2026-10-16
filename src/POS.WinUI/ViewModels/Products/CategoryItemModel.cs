using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.ViewModels.Products;

public sealed partial class CategoryItemModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private Guid? _parentId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _displayOrder;

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private bool _isExpanded = false;

    public CategoryItemModel()
    {
        SubCategories.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasSubCategories));
            OnPropertyChanged(nameof(SubCategoriesCount));
            OnPropertyChanged(nameof(IsExpandedAndHasSubCategories));
        };
    }

    public string DisplayName => Level > 0 ? $"{new string('─', Level * 2)} {Name}" : Name;

    public ObservableCollection<CategoryItemModel> SubCategories { get; } = new();

    public bool HasSubCategories => SubCategories.Count > 0;
    public int SubCategoriesCount => SubCategories.Count;
    public bool IsExpandedAndHasSubCategories => IsExpanded && HasSubCategories;

    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(IsExpandedAndHasSubCategories));
    }

    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(IsExpandedAndHasSubCategories));
    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnLevelChanged(int value) => OnPropertyChanged(nameof(DisplayName));
}

namespace POS.Contracts.V1.Categories;

public record CreateCategoryRequest(
    string Name,
    Guid? ParentId = null,
    int DisplayOrder = 0,
    string? ImageUrl = null,
    bool IsVisible = true
);

public record UpdateCategoryRequest(
    string Name,
    Guid? ParentId = null,
    int DisplayOrder = 0,
    string? ImageUrl = null,
    bool IsVisible = true
);

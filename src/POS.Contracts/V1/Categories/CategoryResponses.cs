namespace POS.Contracts.V1.Categories;

public record CategoryResponse(
    Guid Id,
    Guid? ParentId,
    string Name,
    int DisplayOrder,
    string? ImageUrl,
    bool IsVisible,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CategoryResponse>? SubCategories = null
);

namespace POS.Application.UseCases.Categories.Dtos;

public record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    int DisplayOrder,
    string? ImageUrl,
    bool IsVisible,
    DateTime CreatedAt,
    List<CategoryDto>? SubCategories = null
);

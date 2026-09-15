using POS.Application.UseCases.Categories.Dtos;
using POS.Contracts.V1.Categories;

namespace POS.Api.Mappings;

public static class CategoryMapping
{
    public static CategoryResponse ToResponse(this CategoryDto dto)
    {
        return new CategoryResponse(
            dto.Id,
            dto.ParentId,
            dto.Name,
            dto.DisplayOrder,
            dto.ImageUrl,
            dto.IsVisible,
            new DateTimeOffset(dto.CreatedAt, TimeSpan.Zero),
            dto.SubCategories?.Select(c => c.ToResponse()).ToList()
        );
    }
}

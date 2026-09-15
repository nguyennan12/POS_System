using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Categories.Dtos;

namespace POS.Application.UseCases.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    Guid? ParentId,
    int DisplayOrder,
    string? ImageUrl,
    bool IsVisible
) : ICommand<CategoryDto>;

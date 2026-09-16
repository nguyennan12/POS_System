using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Categories.Dtos;
using POS.Domain.Common;
using POS.Application.UseCases.Categories.Errors;

namespace POS.Application.UseCases.Categories.Commands.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result<CategoryDto>.Failure(CategoryErrors.NotFound);
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == request.Id)
            {
                return Result<CategoryDto>.Failure(new Error("Category.InvalidParent", "A category cannot be its own parent."));
            }

            var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                return Result<CategoryDto>.Failure(CategoryErrors.NotFound);
            }
        }

        category.Update(
            request.Name,
            request.ParentId,
            request.DisplayOrder,
            request.ImageUrl,
            request.IsVisible);

        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto(
            category.Id,
            category.ParentId,
            category.Name,
            category.DisplayOrder,
            category.ImageUrl,
            category.IsVisible,
            category.CreatedAt,
            null);
    }
}

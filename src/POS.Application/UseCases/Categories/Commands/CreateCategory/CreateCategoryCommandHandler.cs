using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Categories.Dtos;
using POS.Domain.Common;
using POS.Domain.Products;
using POS.Application.UseCases.Categories.Errors;

namespace POS.Application.UseCases.Categories.Commands.CreateCategory;

internal sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                return CategoryErrors.NotFound;
            }
        }

        var storeContext = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeContext.IsFailure) return storeContext.Error;

        var storeId = storeContext.Value;

        var category = Category.Create(
            storeId,
            request.Name,
            request.ParentId,
            request.DisplayOrder,
            request.ImageUrl,
            request.IsVisible);

        await _categoryRepository.AddAsync(category, cancellationToken);
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

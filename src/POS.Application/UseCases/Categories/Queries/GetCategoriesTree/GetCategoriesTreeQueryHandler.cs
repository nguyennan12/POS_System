using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Categories.Dtos;
using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Application.UseCases.Categories.Queries.GetCategoriesTree;

internal sealed class GetCategoriesTreeQueryHandler : IQueryHandler<GetCategoriesTreeQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUser _currentUser;

    public GetCategoriesTreeQueryHandler(
        ICategoryRepository categoryRepository,
        ICurrentUser currentUser)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<CategoryDto>>> Handle(GetCategoriesTreeQuery request, CancellationToken cancellationToken)
    {
        var storeId = _currentUser.StoreId;
        if (!storeId.HasValue)
        {
            return Result<List<CategoryDto>>.Failure(new Error("Store.Required", "Store context is missing."));
        }

        var categories = await _categoryRepository.GetAllByStoreIdAsync(storeId.Value, cancellationToken);

        var responseList = BuildTree(categories, null);
        
        return responseList;
    }

    private List<CategoryDto> BuildTree(List<Category> allCategories, Guid? parentId)
    {
        return allCategories
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(
                c.Id,
                c.ParentId,
                c.Name,
                c.DisplayOrder,
                c.ImageUrl,
                c.IsVisible,
                c.CreatedAt,
                BuildTree(allCategories, c.Id)
            ))
            .ToList();
    }
}

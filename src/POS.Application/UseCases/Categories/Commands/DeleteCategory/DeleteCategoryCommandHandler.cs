using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Application.UseCases.Categories.Errors;

namespace POS.Application.UseCases.Categories.Commands.DeleteCategory;

internal sealed class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(CategoryErrors.NotFound);
        }

        var hasProducts = await _categoryRepository.HasProductsAsync(request.Id, cancellationToken);
        if (hasProducts)
        {
            return Result.Failure(CategoryErrors.HasProducts);
        }

        var hasChildren = await _categoryRepository.HasChildrenAsync(request.Id, cancellationToken);
        if (hasChildren)
        {
            return Result.Failure(CategoryErrors.HasChildren);
        }

        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

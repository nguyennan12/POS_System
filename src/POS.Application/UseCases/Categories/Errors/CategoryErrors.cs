using POS.Domain.Common;

namespace POS.Application.UseCases.Categories.Errors;

public static class CategoryErrors
{
    public static readonly Error NotFound = new(
        "Category.NotFound",
        "The category with the specified ID was not found."
    );

    public static readonly Error HasProducts = new(
        "Category.HasProducts",
        "Cannot delete the category because it contains one or more products."
    );

    public static readonly Error HasChildren = new(
        "Category.HasChildren",
        "Cannot delete the category because it contains one or more child categories."
    );
}

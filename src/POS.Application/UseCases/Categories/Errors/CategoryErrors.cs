using POS.Domain.Common;

namespace POS.Application.UseCases.Categories.Errors;

public static class CategoryErrors
{
    public static readonly Error NotFound = new(
        "Category.NotFound",
        "The category with the specified ID was not found."
    );

    public static readonly Error StoreRequired = new(
        ErrorType.Unauthorized,
        "Store.Required",
        "Thiếu ngữ cảnh cửa hàng."
    );

    public static readonly Error StoreAccessDenied = new(
        ErrorType.Forbidden,
        "Category.StoreAccessDenied",
        "Bạn không có quyền truy cập cửa hàng này."
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

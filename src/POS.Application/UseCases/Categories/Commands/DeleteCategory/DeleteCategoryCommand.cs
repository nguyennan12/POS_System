using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : ICommand, IRequirePermission
{
    public string RequiredPermission => "categories:delete";
}

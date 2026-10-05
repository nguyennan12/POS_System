using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    Guid CategoryId,
    string BaseUnit,
    string? Description,
    string? Brand,
    string? ImageUrl,
    string Status
) : ICommand<Guid>, IRequirePermission
{
    public string RequiredPermission => "products:update";
}

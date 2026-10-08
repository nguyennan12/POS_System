using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Commands.DeletePromotion;

public record DeletePromotionCommand(Guid Id) : ICommand<bool>, IRequirePermission
{
    public string RequiredPermission => "promotions:manage";
}

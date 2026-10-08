using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Commands.CreateVoucher;

public record CreateVoucherCommand(
    Guid PromotionId,
    string Code,
    int MaxUses,
    int PerCustomerLimit = 1,
    DateTimeOffset? ExpiresAt = null
) : ICommand<VoucherDto>, IRequirePermission
{
    public string RequiredPermission => "vouchers:manage";
}

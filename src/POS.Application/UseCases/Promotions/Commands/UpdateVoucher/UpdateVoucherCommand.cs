using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Commands.UpdateVoucher;

public record UpdateVoucherCommand(
    Guid Id,
    int MaxUses,
    int PerCustomerLimit,
    DateTimeOffset? ExpiresAt,
    bool IsActive
) : ICommand<VoucherDto>, IRequirePermission
{
    public string RequiredPermission => "vouchers:manage";
}

using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Queries.ValidateVoucher;

public record ValidateVoucherQuery(
    string Code,
    decimal OrderSubtotal,
    Guid? CustomerId = null,
    Guid? StoreId = null
) : IQuery<ValidateVoucherResultDto>, IRequirePermission
{
    public string RequiredPermission => "vouchers:read";
}

using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Queries.GetVouchers;

public record GetVouchersQuery(
    string? Code = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20,
    Guid? PromotionId = null
) : IQuery<PagedVoucherList>, IRequirePermission
{
    public string RequiredPermission => "discounts:read";
}

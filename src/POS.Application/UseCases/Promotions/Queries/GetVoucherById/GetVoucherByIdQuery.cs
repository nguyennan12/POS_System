using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Queries.GetVoucherById;

public record GetVoucherByIdQuery(Guid Id) : IQuery<VoucherDto>, IRequirePermission
{
    public string RequiredPermission => "discounts:read";
}

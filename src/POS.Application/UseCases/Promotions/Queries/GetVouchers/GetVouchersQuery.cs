using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.GetVouchers;

public record GetVouchersQuery(
    string? Code = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20,
    Guid? PromotionId = null
) : IRequest<Result<PagedVoucherList>>;

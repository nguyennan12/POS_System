using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.GetVouchers;

public class GetVouchersQueryHandler(IVoucherRepository voucherRepository)
    : IRequestHandler<GetVouchersQuery, Result<PagedVoucherList>>
{
    public async Task<Result<PagedVoucherList>> Handle(GetVouchersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = request.PromotionId.HasValue
            ? await voucherRepository.GetPagedByPromotionIdAsync(
                request.PromotionId.Value,
                request.Code,
                request.IsActive,
                request.PageNumber,
                request.PageSize,
                cancellationToken)
            : await voucherRepository.GetPagedAsync(
                request.Code,
                request.IsActive,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

        var dtos = items.Select(v => new VoucherDto(
            v.Id,
            v.PromotionId,
            v.Promotion?.Name,
            v.Code,
            v.MaxUses,
            v.UsedCount,
            v.PerCustomerLimit,
            v.ExpiresAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(v.ExpiresAt.Value, DateTimeKind.Utc)) : null,
            v.IsActive
        )).ToList();

        return new PagedVoucherList(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}

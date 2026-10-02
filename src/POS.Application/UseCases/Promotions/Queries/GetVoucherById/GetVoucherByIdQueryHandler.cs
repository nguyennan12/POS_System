using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Errors;

namespace POS.Application.UseCases.Promotions.Queries.GetVoucherById;

public class GetVoucherByIdQueryHandler(IVoucherRepository voucherRepository)
    : IRequestHandler<GetVoucherByIdQuery, Result<VoucherDto>>
{
    public async Task<Result<VoucherDto>> Handle(GetVoucherByIdQuery request, CancellationToken cancellationToken)
    {
        var voucher = await voucherRepository.GetByIdWithPromotionAsync(request.Id, cancellationToken);
        if (voucher is null)
        {
            return PromotionErrors.VoucherNotFound;
        }

        var dto = new VoucherDto(
            voucher.Id,
            voucher.PromotionId,
            voucher.Promotion?.Name,
            voucher.Code,
            voucher.MaxUses,
            voucher.UsedCount,
            voucher.PerCustomerLimit,
            voucher.ExpiresAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(voucher.ExpiresAt.Value, DateTimeKind.Utc)) : null,
            voucher.IsActive
        );

        return dto;
    }
}

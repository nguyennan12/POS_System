using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Errors;

namespace POS.Application.UseCases.Promotions.Commands.UpdateVoucher;

public class UpdateVoucherCommandHandler(
    IVoucherRepository voucherRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVoucherCommand, Result<VoucherDto>>
{
    public async Task<Result<VoucherDto>> Handle(UpdateVoucherCommand request, CancellationToken cancellationToken)
    {
        var voucher = await voucherRepository.GetByIdWithPromotionAsync(request.Id, cancellationToken);
        if (voucher is null)
        {
            return PromotionErrors.VoucherNotFound;
        }

        if (request.MaxUses < voucher.UsedCount)
        {
            return PromotionErrors.VoucherMaxUsesLessThanUsedCount;
        }

        voucher.Update(
            maxUses: request.MaxUses,
            perCustomerLimit: request.PerCustomerLimit,
            expiresAt: request.ExpiresAt?.UtcDateTime,
            isActive: request.IsActive
        );

        await unitOfWork.SaveChangesAsync(cancellationToken);

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

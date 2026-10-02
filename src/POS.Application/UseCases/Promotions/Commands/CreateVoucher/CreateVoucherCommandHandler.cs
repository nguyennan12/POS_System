using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Errors;

namespace POS.Application.UseCases.Promotions.Commands.CreateVoucher;

public class CreateVoucherCommandHandler(
    IVoucherRepository voucherRepository,
    IPromotionRepository promotionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVoucherCommand, Result<VoucherDto>>
{
    public async Task<Result<VoucherDto>> Handle(CreateVoucherCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.NotFound;
        }

        var normalizedCode = request.Code.Trim().ToUpper();
        var isUnique = await voucherRepository.IsCodeUniqueAsync(normalizedCode, null, cancellationToken);
        if (!isUnique)
        {
            return PromotionErrors.VoucherDuplicateCode;
        }

        var voucher = new Voucher(
            promotionId: request.PromotionId,
            code: normalizedCode,
            maxUses: request.MaxUses,
            perCustomerLimit: request.PerCustomerLimit,
            expiresAt: request.ExpiresAt?.UtcDateTime,
            isActive: true
        );

        await voucherRepository.AddAsync(voucher, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new VoucherDto(
            voucher.Id,
            voucher.PromotionId,
            promotion.Name,
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

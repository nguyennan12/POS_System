using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Services;

public interface IPromotionPreviewService
{
    Task<Result<PromotionPreviewResult>> PreviewOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Result<VoucherPreviewResult>> PreviewVoucherForOrderAsync(
        Guid orderId, string code, CancellationToken cancellationToken);

    Task<Result<PromotionPreviewResult>> PreviewCartAsync(
        PromotionPreviewCart cart, CancellationToken cancellationToken);
}

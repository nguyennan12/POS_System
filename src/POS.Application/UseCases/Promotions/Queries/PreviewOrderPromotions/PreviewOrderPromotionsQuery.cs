using MediatR;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.Queries.GetAccessibleOrderById;
using POS.Application.UseCases.Promotions.Services;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.PreviewOrderPromotions;

internal sealed record PreviewOrderPromotionsQuery(Guid OrderId) : IQuery<PromotionPreviewResult>;

internal sealed class PreviewOrderPromotionsQueryHandler(ISender sender, IPromotionPreviewService previewService)
    : IQueryHandler<PreviewOrderPromotionsQuery, PromotionPreviewResult>
{
    public async Task<Result<PromotionPreviewResult>> Handle(
        PreviewOrderPromotionsQuery query, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetAccessibleOrderByIdQuery(query.OrderId), cancellationToken);
        if (order.IsFailure)
            return order.Error;

        return await previewService.PreviewOrderAsync(query.OrderId, cancellationToken);
    }
}

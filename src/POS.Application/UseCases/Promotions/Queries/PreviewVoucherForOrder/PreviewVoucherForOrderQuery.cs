using MediatR;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.Queries.GetAccessibleOrderById;
using POS.Application.UseCases.Promotions.Services;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.PreviewVoucherForOrder;

internal sealed record PreviewVoucherForOrderQuery(Guid OrderId, string Code)
    : IQuery<VoucherPreviewResult>, IRequirePermission
{
    public string RequiredPermission => "vouchers:read";
}

internal sealed class PreviewVoucherForOrderQueryHandler(ISender sender, IPromotionPreviewService previewService)
    : IQueryHandler<PreviewVoucherForOrderQuery, VoucherPreviewResult>
{
    public async Task<Result<VoucherPreviewResult>> Handle(
        PreviewVoucherForOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetAccessibleOrderByIdQuery(query.OrderId), cancellationToken);
        if (order.IsFailure)
            return order.Error;

        return await previewService.PreviewVoucherForOrderAsync(query.OrderId, query.Code, cancellationToken);
    }
}

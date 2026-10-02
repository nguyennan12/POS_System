using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotions;

public class GetPromotionsQueryHandler(IPromotionRepository promotionRepository)
    : IRequestHandler<GetPromotionsQuery, Result<PagedPromotionList>>
{
    public async Task<Result<PagedPromotionList>> Handle(GetPromotionsQuery request, CancellationToken cancellationToken)
    {
        PromotionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<PromotionStatus>(request.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        PromotionType? type = null;
        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<PromotionType>(request.Type, true, out var parsedType))
        {
            type = parsedType;
        }

        var (items, totalCount) = await promotionRepository.GetPagedAsync(
            request.StoreId,
            status,
            type,
            request.ActiveAt,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(p => new PromotionSummaryDto(
            p.Id,
            p.StoreId,
            p.Name,
            p.Type.ToString(),
            p.Value,
            p.AppliesTo.ToString(),
            new DateTimeOffset(DateTime.SpecifyKind(p.ValidFrom, DateTimeKind.Utc)),
            p.ValidTo.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(p.ValidTo.Value, DateTimeKind.Utc)) : null,
            p.Status.ToString(),
            new DateTimeOffset(DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc))
        )).ToList();

        return new PagedPromotionList(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}

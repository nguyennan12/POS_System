using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotions;

public record GetPromotionsQuery(
    Guid? StoreId = null,
    string? Status = null,
    string? Type = null,
    DateTimeOffset? ActiveAt = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PagedPromotionList>>;

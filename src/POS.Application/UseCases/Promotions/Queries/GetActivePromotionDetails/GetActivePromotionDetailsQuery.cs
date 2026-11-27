using FluentValidation;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Queries.GetActivePromotionDetails;

public sealed record GetActivePromotionDetailsQuery(PromotionType? Type = null, int Limit = 20)
    : IQuery<IReadOnlyList<ActivePromotionDetailDto>>, IRequirePermission
{
    public string RequiredPermission => "promotions:read";
}

public sealed class GetActivePromotionDetailsQueryValidator : AbstractValidator<GetActivePromotionDetailsQuery>
{
    public GetActivePromotionDetailsQueryValidator()
    {
        RuleFor(query => query.Type).IsInEnum();
        RuleFor(query => query.Limit).InclusiveBetween(1, 20);
    }
}

public sealed class GetActivePromotionDetailsQueryHandler(
    IStoreAccessPolicy storeAccess,
    IPromotionRepository promotions,
    PromotionDetailsReader detailsReader)
    : IQueryHandler<GetActivePromotionDetailsQuery, IReadOnlyList<ActivePromotionDetailDto>>
{
    public async Task<Result<IReadOnlyList<ActivePromotionDetailDto>>> Handle(
        GetActivePromotionDetailsQuery query, CancellationToken cancellationToken)
    {
        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return store.Error;

        var active = await promotions.GetActivePromotionsAsync(store.Value, DateTime.UtcNow, cancellationToken);
        var selected = active.Where(p => query.Type is null || p.Type == query.Type)
            .Take(query.Limit)
            .ToList();
        var details = await detailsReader.ReadAsync(store.Value, selected, cancellationToken);
        return Result<IReadOnlyList<ActivePromotionDetailDto>>.Success(details);
    }
}

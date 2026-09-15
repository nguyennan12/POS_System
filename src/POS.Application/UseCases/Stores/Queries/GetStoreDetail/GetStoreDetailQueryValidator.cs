using FluentValidation;

namespace POS.Application.UseCases.Stores.Queries.GetStoreDetail;

public class GetStoreDetailQueryValidator : AbstractValidator<GetStoreDetailQuery>
{
    public GetStoreDetailQueryValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Id cửa hàng không hợp lệ.");
    }
}

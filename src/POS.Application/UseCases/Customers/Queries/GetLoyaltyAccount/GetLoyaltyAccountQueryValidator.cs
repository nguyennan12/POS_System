using FluentValidation;

namespace POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;

public class GetLoyaltyAccountQueryValidator : AbstractValidator<GetLoyaltyAccountQuery>
{
    public GetLoyaltyAccountQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Mã khách hàng không được để trống.");
    }
}

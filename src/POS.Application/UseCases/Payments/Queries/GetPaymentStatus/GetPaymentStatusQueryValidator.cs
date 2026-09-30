using FluentValidation;

namespace POS.Application.UseCases.Payments.Queries.GetPaymentStatus;

public class GetPaymentStatusQueryValidator : AbstractValidator<GetPaymentStatusQuery>
{
    public GetPaymentStatusQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithMessage("Id thanh toán không hợp lệ.");
    }
}

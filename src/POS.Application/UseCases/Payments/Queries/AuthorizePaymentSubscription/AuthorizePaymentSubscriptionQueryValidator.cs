using FluentValidation;

namespace POS.Application.UseCases.Payments.Queries.AuthorizePaymentSubscription;

public sealed class AuthorizePaymentSubscriptionQueryValidator : AbstractValidator<AuthorizePaymentSubscriptionQuery>
{
    public AuthorizePaymentSubscriptionQueryValidator()
    {
        RuleFor(query => query.PaymentId).NotEmpty().WithMessage("Id thanh toán không hợp lệ.");
    }
}

using FluentValidation;

namespace POS.Application.UseCases.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ID khách hàng không được để trống.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên khách hàng không được để trống.").MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^0[0-9]{9}$").WithMessage("Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Dob).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).When(x => x.Dob.HasValue);
        RuleFor(x => x.Barcode).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Barcode));
    }
}

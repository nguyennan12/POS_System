using FluentValidation;

namespace POS.Application.UseCases.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên khách hàng không được để trống.")
            .MaximumLength(200).WithMessage("Tên khách hàng không vượt quá 200 ký tự.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^0[0-9]{9}$").WithMessage("Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(100).WithMessage("Email không vượt quá 100 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Dob)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại.")
            .When(x => x.Dob.HasValue);

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("Mã vạch/thẻ không vượt quá 50 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Barcode));
    }
}

using FluentValidation;
using POS.Domain.Customers.Enums;

namespace POS.Application.UseCases.Customers.Queries.GetPointTransactions;

public class GetPointTransactionsQueryValidator : AbstractValidator<GetPointTransactionsQuery>
{

  /// Configures customer, pagination, transaction type, and date range validation rules.
  /// </summary>
  public GetPointTransactionsQueryValidator()
  {
    RuleFor(x => x.CustomerId)
        .NotEmpty().WithMessage("Mã khách hàng không được để trống.");

    RuleFor(x => x.PageNumber)
        .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

    RuleFor(x => x.PageSize)
        .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");

    RuleFor(x => x.Type)
        .Must(t => string.IsNullOrWhiteSpace(t) || Enum.TryParse<PointTransactionType>(t, true, out _))
        .WithMessage("Loại giao dịch điểm không hợp lệ (hỗ trợ: Earn, Redeem, Adjust).");

    RuleFor(x => x)
        .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
        .WithMessage("Thời gian bắt đầu (From) không được lớn hơn thời gian kết thúc (To).");
  }
}

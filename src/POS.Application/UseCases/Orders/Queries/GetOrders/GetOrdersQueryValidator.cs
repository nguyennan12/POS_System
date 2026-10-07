using FluentValidation;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Orders.Queries.GetOrders;

public class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    public GetOrdersQueryValidator()
    {
        RuleFor(x => x.Filter).NotNull();
        When(x => x.Filter is not null, () =>
        {
            RuleFor(x => x.Filter.PageNumber).GreaterThanOrEqualTo(1)
                .WithMessage("Số trang phải lớn hơn hoặc bằng 1.");
            RuleFor(x => x.Filter.PageSize).InclusiveBetween(1, 100)
                .WithMessage("Kích thước trang phải từ 1 đến 100.");
            RuleFor(x => x.Filter.StoreId).NotEqual(Guid.Empty)
                .When(x => x.Filter.StoreId.HasValue)
                .WithMessage("Id cửa hàng không hợp lệ.");
            RuleFor(x => x.Filter.ShiftId).NotEqual(Guid.Empty)
                .When(x => x.Filter.ShiftId.HasValue)
                .WithMessage("Id ca làm việc không hợp lệ.");
            RuleFor(x => x.Filter.From).LessThanOrEqualTo(x => x.Filter.To)
                .When(x => x.Filter.From.HasValue && x.Filter.To.HasValue)
                .WithMessage("Thời điểm bắt đầu phải nhỏ hơn hoặc bằng thời điểm kết thúc.");
            RuleFor(x => x.Filter.Status)
                .Must(status => Enum.TryParse<OrderStatus>(status, true, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.Filter.Status))
                .WithMessage("Trạng thái đơn hàng không hợp lệ (hợp lệ: Draft, Confirmed, Paid, Cancelled).");
        });
    }
}

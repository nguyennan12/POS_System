using POS.Domain.Common;

namespace POS.Application.UseCases.Invoices.Errors;

public static class InvoiceErrors
{
    public static readonly Error OrderNotPaid = new(
        ErrorType.Invalid, "INVOICE.ORDER_NOT_PAID", "Chỉ có thể lập hóa đơn cho đơn hàng đã thanh toán.");

    public static readonly Error AlreadyInvoiced = new(
        ErrorType.AlreadyExists, "INVOICE.ALREADY_EXISTS", "Đơn hàng đã có hóa đơn.");
}

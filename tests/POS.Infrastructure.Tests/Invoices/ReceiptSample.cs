using POS.Application.UseCases.Invoices.DTOs;
using POS.Application.UseCases.Orders.DTOs;

namespace POS.Infrastructure.Tests.Invoices;

internal static class ReceiptSample
{
    public static InvoiceReceiptDto Create(int count = 2, bool reprint = false)
    {
        var time = new DateTimeOffset(2026, 10, 24, 7, 30, 0, TimeSpan.FromHours(7));
        var items = Enumerable.Range(1, count).Select(i => new OrderItemDto(
            new Guid(i, 0, 0, new byte[8]), new Guid(i, 1, 0, new byte[8]), "SKU-" + i,
            "Cà phê sữa đá Việt Nam đặc biệt nguyên chất thơm ngon " + i,
            2, 25000, 5000, 3600, 48600)).ToArray();
        var receipt = new ReceiptDataDto("Cửa hàng Nguyễn Huệ", "123 Đường Nguyễn Huệ, TP. Hồ Chí Minh",
            "0901234567", "12345678", "Nguyễn Thị Hồng", time, items,
            50000 * count, 5000 * count, 3600 * count, 48600 * count, 48600 * count,
            100000 * count - 48600 * count, "Xin kính chào quý khách", "Cảm ơn quý khách! Hẹn gặp lại.");
        return new InvoiceReceiptDto(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "HD-TEST-20261024-000001", time, "Trần Văn Hải", "0123456789", "Quận Bình Thạnh",
            receipt, [new OrderPaymentDto(Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Cash", 100000 * count, receipt.ChangeAmount, null, "Success", time)], reprint, 80);
    }
}

namespace POS.WinUI.ViewModels.Orders;

// Local fixtures for reviewing T52's frontend. No API or database access.
internal static class InvoicePreviewData
{
    public static IReadOnlyList<InvoiceViewItem> Create()
    {
        string[] names = ["Nguyễn Minh Anh", "Trần Hoàng Nam", "Khách lẻ", "Lê Thu Hà", "Phạm Quốc Huy", "Võ Ngọc Linh"];
        string[] phones = ["0901234567", "0912345678", "—", "0987654321", "0934567890", "0961234567"];
        string[] payments = ["Tiền mặt", "VietQR", "Thẻ ngân hàng", "MoMo"];
        var invoices = new List<InvoiceViewItem>();
        for (var i = 0; i < 28; i++)
        {
            var date = DateTime.Today.AddDays(-(i / 4)).AddHours(16 - (i % 4) * 2).AddMinutes(42 - i);
            InvoiceLineItem[] items = (i % 3) switch
            {
                0 => [new("SUA-TH-180", "Sữa tươi TH true MILK 180ml", 4, 8500), new("BANH-OREO", "Bánh Oreo kem vani 133g", 2, 18000), new("NUOC-LAVIE", "Nước khoáng La Vie 500ml", 2, 5000)],
                1 => [new("CAFE-G7", "Cà phê G7 3 trong 1 · Hộp 21 gói", 1, 62000), new("MI-HAOHAO", "Mì Hảo Hảo tôm chua cay", 5, 4500), new("GIAY-PULPPY", "Khăn giấy Pulppy 3 lớp", 2, 27000)],
                _ => [new("SNACK-LAYS", "Khoai tây Lay's vị tự nhiên 90g", 2, 24000), new("NUOC-COCA", "Coca-Cola lon 330ml", 3, 11000)]
            };
            invoices.Add(new()
            {
                Id = Guid.NewGuid(), IsPreview = true,
                StoreName = "OraPOS · Chi nhánh Quận 1",
                StoreAddress = "128 Nguyễn Thị Minh Khai, TP. Hồ Chí Minh",
                InvoiceNo = $"HD-Q1-{date:yyyyMMdd}-{(128 - i):D6}", OrderNo = $"DH-{(1028 - i):D6}",
                IssuedAt = date, BuyerName = names[i % names.Length], BuyerPhone = phones[i % phones.Length],
                CashierName = i % 2 == 0 ? "Trần Bảo Ngọc" : "Nguyễn Tuấn Khang",
                PaymentMethod = payments[i % payments.Length], Items = items,
                Discount = i % 3 == 0 ? 5000 : 0, ReprintCount = i % 5 == 0 ? 1 : 0
            });
        }
        return invoices;
    }
}

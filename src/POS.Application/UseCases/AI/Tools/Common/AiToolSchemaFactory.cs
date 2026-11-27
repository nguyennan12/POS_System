using System.Text.Json;
using System.Text.Json.Serialization;
using POS.Application.Abstractions.AI;

namespace POS.Application.UseCases.AI.Tools.Common;

public static class AiToolSchemaFactory
{
    private static readonly JsonSerializerOptions SchemaSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] OrderStatusValues = ["Draft", "Confirmed", "Paid", "Cancelled"];
    private static readonly string[] PaymentMethodValues = ["Cash", "MoMo", "VietQR", "Card", "Points"];
    private static readonly string[] StockTransactionTypeValues = ["StockIn", "SaleOut", "Dispose", "Adjust"];
    private static readonly string[] PromotionTypeValues =
        ["PercentSku", "FixedSku", "BuyXGetY", "CartPercent", "CartFixed", "HappyHour"];

    public static AiToolDefinition CreateFunctionDefinition(
        string name,
        string description,
        object parametersSchema)
    {
        var element = JsonSerializer.SerializeToElement(parametersSchema, SchemaSerializerOptions);
        return new AiToolDefinition("function", name, description, element);
    }

    public static IReadOnlyList<AiToolDefinition> CreateCoreR1Definitions() =>
    [
        // 1. search_products
        CreateFunctionDefinition(
            AiToolNames.SearchProducts,
            "Tìm kiếm sản phẩm và SKU đang bán tại cửa hàng hiện tại theo tên, mã SKU, mã vạch hoặc thương hiệu.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["keyword"] = StringProp("Từ khóa tìm kiếm (tên sản phẩm, mã SKU, mã vạch hoặc thương hiệu).", minLength: 1, maxLength: 100),
                    ["limit"] = IntProp("Số kết quả tối đa trả về (mặc định 10, tối đa 20).", minimum: 1, maximum: 20, defaultValue: 10)
                },
                required: ["keyword"])),

        // 2. get_product_detail
        CreateFunctionDefinition(
            AiToolNames.GetProductDetail,
            "Xem thông tin chi tiết của một sản phẩm (danh mục, thương hiệu, đơn vị gốc, các biến thể SKU và quy đổi đơn vị).",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["productId"] = UuidProp("Mã định danh UUID của sản phẩm.")
                },
                required: ["productId"])),

        // 3. get_inventory
        CreateFunctionDefinition(
            AiToolNames.GetInventory,
            "Tra cứu số lượng tồn kho hiện tại và định mức tồn tối thiểu của một SKU tại cửa hàng hiện tại.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["skuId"] = UuidProp("Mã định danh UUID của SKU cần xem tồn kho.")
                },
                required: ["skuId"])),

        // 4. get_stock_alerts
        CreateFunctionDefinition(
            AiToolNames.GetStockAlerts,
            "Xem danh sách cảnh báo tồn kho thấp (MinStock) và lô hàng sắp hết hạn (NearExpiry) của cửa hàng hiện tại.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["nearExpiryDays"] = IntProp("Số ngày tới tính là sắp hết hạn (mặc định 30, từ 1 đến 365).", minimum: 1, maximum: 365, defaultValue: 30),
                    ["limit"] = IntProp("Số cảnh báo tối đa trả về (mặc định 20, tối đa 50).", minimum: 1, maximum: 50, defaultValue: 20)
                })),

        // 5. list_orders
        CreateFunctionDefinition(
            AiToolNames.ListOrders,
            "Liệt kê danh sách đơn hàng tại cửa hàng hiện tại theo trạng thái và khoảng ngày (tự động giới hạn theo quyền người dùng).",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["status"] = EnumProp("Trạng thái đơn hàng cần lọc.", OrderStatusValues),
                    ["from"] = DateProp("Ngày bắt đầu lọc theo chuẩn ISO YYYY-MM-DD."),
                    ["to"] = DateProp("Ngày kết thúc lọc theo chuẩn ISO YYYY-MM-DD."),
                    ["limit"] = IntProp("Số đơn hàng tối đa trả về (mặc định 20, tối đa 50).", minimum: 1, maximum: 50, defaultValue: 20)
                })),

        // 6. get_order_detail
        CreateFunctionDefinition(
            AiToolNames.GetOrderDetail,
            "Xem chi tiết một đơn hàng theo orderId (danh sách mặt hàng, số lượng, đơn giá, giảm giá, tổng tiền và trạng thái).",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["orderId"] = UuidProp("Mã định danh UUID của đơn hàng.")
                },
                required: ["orderId"])),

        // 7. search_invoices
        CreateFunctionDefinition(
            AiToolNames.SearchInvoices,
            "Tra cứu danh sách hóa đơn theo số hóa đơn/số điện thoại khách, mã đơn hàng, khoảng ngày hoặc phương thức thanh toán.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["search"] = StringProp("Từ khóa số hóa đơn (vd HD...) hoặc số điện thoại khách hàng.", maxLength: 100),
                    ["orderId"] = UuidProp("Mã định danh UUID của đơn hàng gốc."),
                    ["from"] = DateProp("Ngày phát hành từ (ISO YYYY-MM-DD)."),
                    ["to"] = DateProp("Ngày phát hành đến (ISO YYYY-MM-DD)."),
                    ["paymentMethod"] = EnumProp("Phương thức thanh toán.", PaymentMethodValues),
                    ["limit"] = IntProp("Số hóa đơn tối đa trả về (mặc định 20, tối đa 50).", minimum: 1, maximum: 50, defaultValue: 20)
                })),

        // 8. get_invoice_detail
        CreateFunctionDefinition(
            AiToolNames.GetInvoiceDetail,
            "Xem chi tiết nội dung một hóa đơn theo invoiceId.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["invoiceId"] = UuidProp("Mã định danh UUID của hóa đơn.")
                },
                required: ["invoiceId"])),

        // 9. get_revenue_report
        CreateFunctionDefinition(
            AiToolNames.GetRevenueReport,
            "Xem báo cáo doanh thu theo ngày của cửa hàng hiện tại trong khoảng thời gian từ ngày from đến ngày to.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["from"] = DateProp("Ngày bắt đầu báo cáo (ISO YYYY-MM-DD)."),
                    ["to"] = DateProp("Ngày kết thúc báo cáo (ISO YYYY-MM-DD).")
                },
                required: ["from", "to"])),

        // 10. get_top_selling_products
        CreateFunctionDefinition(
            AiToolNames.GetTopSellingProducts,
            "Xem danh sách sản phẩm/SKU bán chạy nhất tại cửa hàng hiện tại trong khoảng ngày chỉ định.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["from"] = DateProp("Ngày bắt đầu thống kê (ISO YYYY-MM-DD)."),
                    ["to"] = DateProp("Ngày kết thúc thống kê (ISO YYYY-MM-DD)."),
                    ["limit"] = IntProp("Số sản phẩm tối đa trả về (mặc định 10, tối đa 20).", minimum: 1, maximum: 20, defaultValue: 10)
                },
                required: ["from", "to"])),

        // 11. search_customers
        CreateFunctionDefinition(
            AiToolNames.SearchCustomers,
            "Tìm kiếm khách hàng theo số điện thoại, họ tên hoặc mã thẻ/mã vạch thành viên (cần cung cấp ít nhất một tiêu chí).",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["phone"] = StringProp("Số điện thoại khách hàng.", maxLength: 30),
                    ["name"] = StringProp("Tên khách hàng cần tìm.", maxLength: 100),
                    ["barcode"] = StringProp("Mã thẻ hoặc mã vạch thành viên.", maxLength: 100),
                    ["limit"] = IntProp("Số khách hàng tối đa trả về (mặc định 10, tối đa 20).", minimum: 1, maximum: 20, defaultValue: 10)
                })),

        // 12. get_customer_detail
        CreateFunctionDefinition(
            AiToolNames.GetCustomerDetail,
            "Xem thông tin hồ sơ giao dịch của một khách hàng theo customerId.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["customerId"] = UuidProp("Mã định danh UUID của khách hàng.")
                },
                required: ["customerId"])),

        // 13. get_customer_loyalty
        CreateFunctionDefinition(
            AiToolNames.GetCustomerLoyalty,
            "Tra cứu số dư điểm tích lũy và hạng thành viên hiện tại của khách hàng theo customerId.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["customerId"] = UuidProp("Mã định danh UUID của khách hàng.")
                },
                required: ["customerId"])),

        // 14. get_current_shift
        CreateFunctionDefinition(
            AiToolNames.GetCurrentShift,
            "Kiểm tra thông tin ca làm việc đang mở của nhân viên tại cửa hàng hiện tại (tùy chọn lọc theo máy thu ngân registerId).",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["registerId"] = UuidProp("Mã định danh UUID của máy thu ngân (tùy chọn).")
                })),

        // 15. validate_voucher
        CreateFunctionDefinition(
            AiToolNames.ValidateVoucher,
            "Kiểm tra tính hợp lệ và tính thử mức giảm giá của mã voucher trên một đơn hàng thật (không áp dụng hay lưu vào đơn).",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["code"] = StringProp("Mã voucher cần kiểm tra.", minLength: 1, maxLength: 50),
                    ["orderId"] = UuidProp("Mã định danh UUID của đơn hàng cần kiểm tra voucher.")
                },
                required: ["code", "orderId"])),

        // 16. get_dashboard_summary
        CreateFunctionDefinition(
            AiToolNames.GetDashboardSummary,
            "Xem tổng quan các chỉ số vận hành chính (dashboard) của cửa hàng hiện tại trong N ngày gần nhất.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["days"] = IntProp("Số ngày thống kê gần nhất (mặc định 7, từ 1 đến 90).", minimum: 1, maximum: 90, defaultValue: 7)
                })),

        // 17. get_slow_moving_products
        CreateFunctionDefinition(
            AiToolNames.GetSlowMovingProducts,
            "Xem danh sách mặt hàng chậm luân chuyển (lâu chưa phát sinh bán) tại cửa hàng hiện tại.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["days"] = IntProp("Số ngày không bán tối thiểu để xét chậm luân chuyển (mặc định 30, từ 1 đến 365).", minimum: 1, maximum: 365, defaultValue: 30),
                    ["limit"] = IntProp("Số dòng tối đa trả về (mặc định 20, tối đa 50).", minimum: 1, maximum: 50, defaultValue: 20)
                })),

        // 18. get_stock_history
        CreateFunctionDefinition(
            AiToolNames.GetStockHistory,
            "Xem lịch sử biến động tồn kho (nhập, bán, hủy, điều chỉnh) của một SKU tại cửa hàng hiện tại.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["skuId"] = UuidProp("Mã định danh UUID của SKU cần xem lịch sử kho."),
                    ["type"] = EnumProp("Loại giao dịch kho cần lọc.", StockTransactionTypeValues),
                    ["limit"] = IntProp("Số giao dịch tối đa trả về (mặc định 20, tối đa 100).", minimum: 1, maximum: 100, defaultValue: 20)
                },
                required: ["skuId"])),

        // 19. get_expiring_batches
        CreateFunctionDefinition(
            AiToolNames.GetExpiringBatches,
            "Tra cứu danh sách các lô hàng còn tồn có hạn sử dụng trước hoặc bằng ngày chỉ định.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["skuId"] = UuidProp("Mã định danh UUID của SKU cần lọc (tùy chọn)."),
                    ["expiryBefore"] = DateProp("Mốc hạn sử dụng tối đa (ISO YYYY-MM-DD)."),
                    ["limit"] = IntProp("Số lô tối đa trả về (mặc định 20, tối đa 100).", minimum: 1, maximum: 100, defaultValue: 20)
                },
                required: ["expiryBefore"])),

        // 20. list_active_promotions
        CreateFunctionDefinition(
            AiToolNames.ListActivePromotions,
            "Liệt kê các chương trình khuyến mãi đang hoạt động tại cửa hàng hiện tại kèm điều kiện và đối tượng áp dụng.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["type"] = EnumProp("Loại chương trình khuyến mãi cần lọc.", PromotionTypeValues),
                    ["limit"] = IntProp("Số chương trình tối đa trả về (mặc định 20, tối đa 20).", minimum: 1, maximum: 20, defaultValue: 20)
                })),

        // 21. get_product_promotions
        CreateFunctionDefinition(
            AiToolNames.GetProductPromotions,
            "Tra cứu các chương trình ưu đãi tiềm năng đang áp dụng cho một SKU với số lượng dự kiến.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["skuId"] = UuidProp("Mã định danh UUID của SKU."),
                    ["quantity"] = NumberProp("Số lượng dự kiến mua (lớn hơn 0, mặc định 1).", exclusiveMinimum: 0, defaultValue: 1)
                },
                required: ["skuId"])),

        // 22. preview_order_promotions
        CreateFunctionDefinition(
            AiToolNames.PreviewOrderPromotions,
            "Tính thử khuyến mãi, giảm giá và gợi ý mua thêm (như BuyXGetY) cho một đơn hàng hiện có mà không sửa dữ liệu đơn.",
            ObjectSchema(
                new Dictionary<string, object>
                {
                    ["orderId"] = UuidProp("Mã định danh UUID của đơn hàng cần tính thử khuyến mãi.")
                },
                required: ["orderId"]))
    ];

    private static Dictionary<string, object> ObjectSchema(
        Dictionary<string, object> properties,
        string[]? required = null)
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };

        if (required is { Length: > 0 })
        {
            schema["required"] = required;
        }

        return schema;
    }

    private static Dictionary<string, object?> StringProp(
        string description,
        int? minLength = null,
        int? maxLength = null) =>
        new()
        {
            ["type"] = "string",
            ["description"] = description,
            ["minLength"] = minLength,
            ["maxLength"] = maxLength
        };

    private static Dictionary<string, object?> UuidProp(string description) =>
        new()
        {
            ["type"] = "string",
            ["format"] = "uuid",
            ["description"] = description
        };

    private static Dictionary<string, object?> DateProp(string description) =>
        new()
        {
            ["type"] = "string",
            ["format"] = "date",
            ["description"] = description
        };

    private static Dictionary<string, object?> EnumProp(string description, IReadOnlyList<string> values) =>
        new()
        {
            ["type"] = "string",
            ["enum"] = values,
            ["description"] = description
        };

    private static Dictionary<string, object?> IntProp(
        string description,
        int minimum,
        int maximum,
        int defaultValue) =>
        new()
        {
            ["type"] = "integer",
            ["description"] = description,
            ["minimum"] = minimum,
            ["maximum"] = maximum,
            ["default"] = defaultValue
        };

    private static Dictionary<string, object?> NumberProp(
        string description,
        decimal exclusiveMinimum,
        decimal defaultValue) =>
        new()
        {
            ["type"] = "number",
            ["description"] = description,
            ["exclusiveMinimum"] = exclusiveMinimum,
            ["default"] = defaultValue
        };
}

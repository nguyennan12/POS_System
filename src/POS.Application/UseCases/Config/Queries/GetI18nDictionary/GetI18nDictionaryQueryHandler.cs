using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Config;
using POS.Domain.Common;

namespace POS.Application.UseCases.Config.Queries.GetI18nDictionary;

public class GetI18nDictionaryQueryHandler(ITranslationRepository translations)
    : IQueryHandler<GetI18nDictionaryQuery, I18nDictionaryResponse>
{
    private static readonly Dictionary<string, Dictionary<string, string>> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nav.workspace"] = "KHÔNG GIAN LÀM VIỆC",
            ["nav.dashboard"] = "Tổng quan",
            ["nav.orders"] = "Đơn hàng & Hóa đơn",
            ["nav.products"] = "Sản phẩm & Danh mục",
            ["nav.inventory"] = "Quản lý Kho & Nhập hàng",
            ["nav.customers"] = "Khách hàng & Hội viên",
            ["nav.promotions"] = "Khuyến mãi & Voucher",
            ["nav.employees"] = "Nhân viên & Phân quyền",
            ["nav.reports"] = "Báo cáo & Thống kê",
            ["nav.settings"] = "Cài đặt hệ thống",
            ["header.pos_screen"] = "Màn hình Bán hàng",
            ["header.loading_stores"] = "Đang tải chi nhánh...",
            ["header.notifications"] = "Thông báo hệ thống",
            ["settings.title"] = "Cài đặt hệ thống",
            ["settings.subtitle"] = "Thiết lập cấu hình cửa hàng, máy in hóa đơn và tích hợp phần cứng",
            ["settings.language"] = "Ngôn ngữ giao diện",
            ["settings.language_desc"] = "Chọn ngôn ngữ và nhấn Áp dụng. Toàn bộ nhãn trên WinUI thay đổi tức thì – không cần build lại app.",
            ["settings.apply_language"] = "Áp dụng ngôn ngữ",
            ["settings.store_config"] = "Cấu hình cửa hàng",
            ["settings.store_config_desc"] = "Tải cấu hình SystemConfig của cửa hàng hiện tại từ server.",
            ["settings.load_config"] = "Tải cấu hình",
            ["settings.lang_switched"] = "Đã chuyển ngôn ngữ sang '{0}'.",
            ["breadcrumb.home"] = "Trang chủ",
        },
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nav.workspace"] = "WORKSPACE",
            ["nav.dashboard"] = "Dashboard",
            ["nav.orders"] = "Orders & Invoices",
            ["nav.products"] = "Products & Categories",
            ["nav.inventory"] = "Inventory & Stock In",
            ["nav.customers"] = "Customers & Members",
            ["nav.promotions"] = "Promotions & Vouchers",
            ["nav.employees"] = "Employees & Roles",
            ["nav.reports"] = "Reports & Analytics",
            ["nav.settings"] = "System Settings",
            ["header.pos_screen"] = "POS Terminal",
            ["header.loading_stores"] = "Loading stores...",
            ["header.notifications"] = "System Notifications",
            ["settings.title"] = "System Settings",
            ["settings.subtitle"] = "Configure store settings, receipt printers, and hardware integrations",
            ["settings.language"] = "Display Language",
            ["settings.language_desc"] = "Select language and click Apply. All labels on WinUI change instantly - without rebuilding the app.",
            ["settings.apply_language"] = "Apply Language",
            ["settings.store_config"] = "Store Configuration",
            ["settings.store_config_desc"] = "Load SystemConfig of current store from server.",
            ["settings.load_config"] = "Load Configuration",
            ["settings.lang_switched"] = "Switched language to '{0}'.",
            ["breadcrumb.home"] = "Home",
        }
    };

    public async Task<Result<I18nDictionaryResponse>> Handle(
        GetI18nDictionaryQuery query, CancellationToken cancellationToken)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (Defaults.TryGetValue(query.LanguageCode, out var defaultDict))
        {
            foreach (var kvp in defaultDict)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        var list = await translations.GetByLanguageAsync(query.LanguageCode, cancellationToken);
        foreach (var item in list)
        {
            dict[item.Key] = item.Value;
        }

        return new I18nDictionaryResponse(query.LanguageCode, dict);
    }
}

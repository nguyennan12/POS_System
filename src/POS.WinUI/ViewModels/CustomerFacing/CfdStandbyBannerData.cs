namespace POS.WinUI.ViewModels.CustomerFacing;

public record CfdBannerItem(string ImagePath, string Tag, string Title, string PriceBadge);

public static class CfdStandbyBannerData
{
    public static readonly CfdBannerItem[] PortraitBanners = new[]
    {
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_misedaap_laksa.jpg", "🍜 MÌ LAKSA", "Mi Sedaap Spicy Laksa", "18K"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_pringles_original.png", "🥔 PRINGLES", "Pringles Original Giòn Rụm", "Đồng Giá 39K"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_nescafe_caramel.png", "☕ NESCAFÉ", "Nescafé Caramel Macchiato Can", "Mua 2 Tặng 1"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_kitkat_oreo.jpg", "🍦 KEM KITKAT & OREO", "Kem Que KitKat & Oreo Mát Lạnh", "Giảm 25%"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_jin_ramen.png", "🍜 JIN RAMEN", "Mì Jin Ramen Hàn Quốc", "MUA 2 TẶNG 1")
    };

    public static readonly CfdBannerItem[] LandscapeBanners = new[]
    {
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_chinsu.jpg", "🌶️ SIÊU DEAL CHIN-SU", "Chin-Su Sriracha & Nam Ngư: 360h Sắc Vị Hà Nội", "Đồng Giá 35K"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_spicy_week.png", "🔥 SPICE UP YOUR WEEK", "Tuần Lễ Cay Nồng: Samyang & Nissin Cup Noodles", "-15% ALL ITEMS"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_paldo_teumsae.png", "🔥 PALDO TEUMSAE", "Mì Cay Nhất Hàn Quốc Paldo Teumsae", "Deal Cay 29K"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_oreo_ritz.png", "🍪 OREO & RITZ", "Oreo & Ritz Salted Caramel Vị Đậm Đà", "Đồng Giá 28K"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_lays_flavor.jpg", "🥔 LAY'S KHOAI TÂY", "Lay's Snack Khoai Tây Chiên Giòn Tan Đủ Vị", "Đồng Giá 22K"),
        new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_oishi_bido.jpg", "🎃 SNACK OISHI", "Bánh Phồng Oishi Bí Đỏ & Tôm Giòn Rụm", "Combo 25K")
    };

    public static readonly (CfdBannerItem Card1, CfdBannerItem Card2)[] SubPairs = new[]
    {
        (
            new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_chupachups_lollipop.png", "🍭 CHUPA CHUPS", "Kẹo Mút Đại Trân Châu Vui Nhộn", "MUA 2 TẶNG 1"),
            new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_chupachups_sour.png", "🍬 KẸO DẺO", "Kẹo Dẻo Chua Cay Sảng Khoái", "Đồng Giá 12K")
        ),
        (
            new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_maggi.png", "🎁 QUÀ MAGGI", "Maggi Nấm Hương & Dầu Hào", "Trúng Tủ Lạnh"),
            new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_sabritas.png", "⚡ COMBO SWITCH", "Ruffles & Cheetos Switch", "-20% Hôm Nay")
        ),
        (
            new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_mirinda_orange.png", "🍊 MIRINDA CAM", "Mirinda Cam Bùng Nổ Sảng Khoái", "Tặng Ly Đổi Màu"),
            new CfdBannerItem("pack://application:,,,/Resources/Assets/Banners/banner_dellycook.png", "🥫 SỐT GIA VỊ", "Sốt Nấu Delly Cook Đậm Đà", "MUA 1 TẶNG 1")
        )
    };
}

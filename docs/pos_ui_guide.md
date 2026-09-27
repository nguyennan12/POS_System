# OraPOS - UI/UX Design System & Style Guide (Single Source of Truth)

> **Tài liệu chuẩn duy nhất (Single Source of Truth)** cho toàn bộ thiết kế và triển khai giao diện (UI/UX) của ứng dụng Desktop **OraPOS** (`POS.WinUI`).  
> **Áp dụng cho:** Tất cả Developer, UI Designer và AI Assistant khi xây dựng, chỉnh sửa hoặc mở rộng màn hình XAML.

---

## 1. DESIGN OVERVIEW

### 1.1. Design Philosophy
- **Modern & Professional Retail**: Giao diện mang phong cách bán lẻ hiện đại, ấm áp nhưng chuyên nghiệp, hướng tới trải nghiệm thu ngân và quản lý mượt mà, trực quan.
- **Tactile & Touch-Friendly**: Tối ưu cho cả thao tác chuột lẫn màn hình cảm ứng POS tại quầy. Các phần tử tương tác (Numpad, Phím bấm, Input) có kích thước lớn, hiệu ứng phản hồi xúc giác rõ rệt (Tactile scale animation khi bấm, glow indicator).
- **High Contrast & Readability**: Phông chữ và màu sắc có độ tương phản cao (chữ Slate sẫm trên nền trắng/nhạt; chữ trắng trên nền màu chủ đạo) giúp nhân viên dễ đọc thông tin dưới ánh sáng quầy thu ngân.
- **Consistency First**: Mọi màn hình phải tuân thủ nghiêm ngặt bảng màu, typography, khoảng cách (spacing), bo góc (radius) và các component đã định nghĩa sẵn.

### 1.2. Visual Characteristics
| Đặc tính | Quy chuẩn | Mô tả chi tiết |
| :--- | :--- | :--- |
| **Overall Style** | Modern Light & Glass | Kết hợp nền sáng thanh lịch (`#F8FAFC`, `#FFFFFF`), điểm xuyết hiệu ứng kính mờ (Glassmorphism) trên các banner/badge |
| **Màu sắc chủ đạo** | Warm Orange Brand | Sắc cam ấm áp `#ED7A1C` tạo năng lượng và điểm nhấn thị giác rõ ràng cho các CTA chính |
| **Mức độ bo góc** | Medium to High Rounded | Bo góc từ `8px` (chấm PIN) đến `12px - 16px` (Button, Input, Badge) và `28px` (Main Card) |
| **Mức độ Shadow** | Layered Soft Shadow | Sử dụng DropShadow đa tầng với tông ấm `#1E0A02` hoặc trung tính `#0F172A` tạo độ nổi khối |
| **Mức độ Spacing** | Breathable (Thoáng đãng) | Khoảng cách phân lớp rõ ràng (`4px`, `8px`, `16px`, `24px`, `32px`, `48px`), tránh dồn ép dữ liệu |
| **Density (Mật độ)** | Comfortable Density | Chiều cao input/button tối thiểu từ `44px - 54px`, đảm bảo bấm chính xác trên màn hình cảm ứng |

---

## 2. COLOR SYSTEM (PALETTE & TOKENS)

Toàn bộ màu sắc được quản lý tập trung tại `src/POS.WinUI/Styles/Colors.xaml`. Tuyệt đối **không hardcode** mã màu HEX trong từng file XAML riêng lẻ.

### 2.1. Brand / Primary Colors
| Token Name | Resource Key | HEX | RGB | Mục đích sử dụng | Component mẫu |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Primary** | `BrandPrimaryColor` / `PrimaryBrush` | `#ED7A1C` | `237, 122, 28` | Màu thương hiệu chủ đạo, nút bấm hành động chính (CTA) | `PrimaryButtonStyle`, Logo, Icon |
| **Primary Hover** | `BrandHoverColor` / `PrimaryHoverBrush` | `#D6680D` | `214, 104, 13` | Trạng thái rê chuột lên nút chính | Hover của `PrimaryButtonStyle` |
| **Primary Pressed** | `BrandPressedColor` / `PrimaryPressedBrush` | `#B75304` | `183, 83, 4` | Trạng thái nhấn giữ nút chính | Pressed của `PrimaryButtonStyle` |
| **Primary Light** | `BrandLightColor` / `PrimaryLightBrush` | `#FFF7ED` | `255, 247, 237` | Nền nhạt cho hover phím số hoặc item được chọn | Hover của `NumpadButtonStyle` |
| **Primary Border** | `BrandBorderColor` / `PrimaryBorderBrush` | `#FDD8B3` | `253, 216, 179` | Viền nhạt khi hover/active | Viền hover Numpad, viền tab active |
| **Primary Glow** | `BrandGlowColor` | `#F38D38` | `243, 141, 56` | Hiệu ứng phát sáng (Shadow Effect) | Glow chấm PIN, Glow CTA |

### 2.2. Surfaces & Backgrounds
| Token Name | Resource Key | HEX / Alpha | Mục đích sử dụng | Component mẫu |
| :--- | :--- | :--- | :--- | :--- |
| **Card Background** | `CardBackgroundBrush` | `#FFFFFF` | Nền các thẻ Card nổi, Dialog, Modal, Bảng dữ liệu | Card Đăng nhập, Modal thanh toán |
| **Input Background** | `InputBackgroundBrush` | `#F8FAFC` | Nền ô nhập liệu, ô tìm kiếm, dropdown, phím số | `TextInputField`, `PasswordInputField` |
| **Input Border** | `InputBorderBrush` | `#E2E8F0` | Viền mặc định cho ô nhập liệu, phím số, divider | Viền Input, Numpad border |
| **Segmented Background** | `SegmentedBackgroundBrush` | `#F1F5F9` | Nền thanh chuyển tab segmented | Vùng chứa tab Mật khẩu / PIN |
| **Glass Overlay** | `GlassOverlayBrush` | `#10FFFFFF` (10% White) | Lớp phủ ánh sáng trên nền hình ảnh | Lớp phủ ảnh nền đăng nhập |
| **Badge Background** | `BadgeBackgroundBrush` | `#26FFFFFF` (15% White) | Nền cho các badge / tag trong suốt trên nền tối | Feature Badges ("Nhanh chóng", "Tin cậy") |
| **Badge Border** | `BadgeBorderBrush` | `#4DFFFFFF` (30% White) | Viền cho các badge / tag trong suốt | Viền Feature Badges |

### 2.3. Typography Colors
| Token Name | Resource Key | HEX | Mục đích sử dụng | Component mẫu |
| :--- | :--- | :--- | :--- | :--- |
| **Text Primary** | `TextPrimaryBrush` | `#0F172A` | Tiêu đề chính, văn bản quan trọng nhất, nhãn số tiền | Card Title ("Chào mừng trở lại"), Toast text |
| **Text Secondary** | `TextSecondaryBrush` | `#334155` | Nhãn form (Form Labels), phím số numpad | Label "Tên đăng nhập", Phím 1-9 |
| **Text Muted** | `TextMutedBrush` | `#64748B` | Mô tả phụ, subtext, tab chưa active | Subtitle card, Checkbox text, Inactive tab |
| **Text Placeholder** | `TextPlaceholderBrush` | `#94A3B8` | Chữ gợi ý trong ô nhập liệu, icon placeholder | Gợi ý "Nhập mật khẩu", icon khoá |
| **Text White** | `TextWhiteBrush` | `#FFFFFF` | Chữ hiển thị trên nền màu cam hoặc nền tối | Chữ nút chính, Hero Title "OraPOS" |
| **Text White Soft** | `TextWhiteSoftBrush` | `#F1F5F9` | Chữ phụ trên nền tối | Slogan "Hệ thống quản lý bán hàng..." |
| **Text White Muted** | `TextWhiteMutedBrush` | `#E2E8F0` | Chữ nhạt / thông tin phiên bản trên nền tối | Footer "Phiên bản v1.0.0", Network status |

### 2.4. Semantic / Status Colors
| Trạng thái | Resource Key | HEX | Mục đích sử dụng | Component mẫu |
| :--- | :--- | :--- | :--- | :--- |
| **Success** | `SuccessColor` / `SuccessBrush` | `#10B981` | Báo hiệu thành công, hoàn thành giao dịch | Icon Toast thành công, Nút thanh toán |
| **Success Glow** | `SuccessGlowColor` / `SuccessGlowBrush` | `#34D399` | Trạng thái Online, kết nối server ổn định | Chấm tròn xanh trong `NetworkStatusBar` |
| **Warning / Accent** | `GoldAccentColor` / `GoldAccentBrush` | `#FDE047` | Cảnh báo nhẹ, điểm nhấn tính năng, nút Thử lại | Icon badge, Nút "Thử lại" kết nối |
| **Danger / Error** | `DangerColor` / `DangerBrush` | `#DC2626` | Báo lỗi, thao tác huỷ / xoá | Icon lỗi, Chấm offline (`#EF4444`) |
| **Danger Text** | `DangerTextColor` / `DangerTextBrush` | `#B91C1C` | Chữ thông báo lỗi trong banner | Thông báo "Sai mật khẩu / mã PIN" |
| **Danger Background** | `DangerBgColor` / `DangerBgBrush` | `#FEF2F2` | Nền banner thông báo lỗi | Error Banner container |
| **Danger Border** | `DangerBorderColor` / `DangerBorderBrush` | `#FECACA` | Viền khung banner lỗi | Error Banner border |

---

## 3. TYPOGRAPHY SYSTEM

Typography sử dụng font chuẩn của hệ điều hành Windows qua Lepo WPF-UI (Mặc định: **Segoe UI** / **Segoe UI Variable**).

### 3.1. Type Hierarchy & Scale
| Hierarchy Level | Size | Weight | Line / Layout Role | Resource / Code Reference |
| :--- | :--- | :--- | :--- | :--- |
| **Display (Hero)** | `82px` | `Black` (900) | Tên thương hiệu lớn trên màn hình chào đón / Auth | `FontSize="82" FontWeight="Black"` |
| **Page Title** | `28px` | `Bold` (700) | Tiêu đề màn hình chính, tiêu đề card lớn | `FontSize="28" FontWeight="Bold"` |
| **Hero Subtitle** | `20px` | `Medium` (500) | Câu slogan, khẩu hiệu chính của trang | `FontSize="20" FontWeight="Medium"` |
| **Numpad Number** | `20px` | `SemiBold` (600) | Số hiển thị trên bàn phím số cảm ứng | `NumpadButtonStyle` (`FontSize="20"`) |
| **Section Title** | `16px - 18px`| `Bold` (700) | Tiêu đề khối giỏ hàng, bảng tổng tiền | `FontSize="16" FontWeight="Bold"` |
| **Primary Button Text** | `15px` | `Bold` (700) | Nhãn nút hành động chính, tab segmented | `PrimaryButtonStyle` (`FontSize="15"`) |
| **Input Text / Body** | `14px` | `Regular` (400) | Nội dung người dùng gõ vào input, text trong toast | `FontSize="14"`, `TextInputField` |
| **Form Label** | `13px` | `SemiBold` (600) | Nhãn phía trên mỗi ô nhập liệu | `FontSize="13" FontWeight="SemiBold"` |
| **Badge / Subtext** | `13px` | `SemiBold` / `Medium` | Chữ trong badge tính năng, trạng thái network | Feature Badges, `NetworkStatusBar` |
| **Micro / Helper Text** | `12px` | `Regular` / `Bold` | Chữ hướng dẫn dưới chân trang, nhãn nhỏ | `FontSize="12"` |

---

## 4. SPACING & SIZING TOKENS

### 4.1. Spacing Scale
| Token | Giá trị | Phạm vi áp dụng |
| :--- | :--- | :--- |
| `spacing-xs` | `4px` | Padding trong của segmented container, khoảng cách giữa các phần tử cực nhỏ |
| `spacing-sm` | `8px` | Khoảng cách giữa Label và Input, margin giữa Icon và Text |
| `spacing-md` | `12px` - `16px` | Padding trong Input (`12,0`), khoảng cách giữa các ô form (`16px`), padding badge |
| `spacing-lg` | `20px` - `26px` | Khoảng cách giữa các khối chức năng (Form sections, title to content, header margin) |
| `spacing-xl` | `32px` - `48px` | Padding bao ngoài màn hình (`Margin="48,32"`), padding của Card chính (`Padding="36,46"`) |

### 4.2. Specific Component Dimensions
- **Standard Input Height**: `50px` (Chiều cao chuẩn cho `TextInputField`, `PasswordInputField`)
- **Primary Action Button Height**: `48px` - `50px`
- **Numpad Button Height**: `54px`
- **Segmented Tab Button Height**: `44px`
- **Badge Height / Padding**: `Padding="14,8"`
- **Status Indicator Dot**: `Width="9" Height="9"`
- **PIN Dot Indicator**: `Width="16" Height="16"`

---

## 5. BORDER RADIUS & STROKES

### 5.1. Radius Hierarchy
| Token | Giá trị | Phạm vi & Component áp dụng |
| :--- | :--- | :--- |
| `radius-sm` | `8px` | Chấm hiển thị mã PIN (`PinDotStyle`) |
| `radius-md` | `10px` - `12px` | Segmented Tab Button inner (`10px`), `PrimaryButtonStyle` (`12px`), `TextInputField` (`12px`), Error Banner (`12px`), Dropdown Wrapper (`12px`) |
| `radius-lg` | `14px` - `16px` | Phím Numpad (`14px`), Vỏ bọc Segmented (`14px`), Feature Badges (`16px`), Logo Icon Box (`16px`), Toast Notification (`16px`) |
| `radius-xl` | `28px` | Thẻ Card chính (`Card Background Border`), Modal bao ngoài |
| `radius-full` | `50%` / Ellipse | Chấm mạng Online/Offline (`9x9`), Vòng tròn icon Toast (`28x28 CornerRadius="14"`) |

### 5.2. Border Widths & Colors
- **Mặc định (Default)**: `BorderThickness="1"`, `BorderBrush="{StaticResource InputBorderBrush}"` (`#E2E8F0`).
- **Nổi bật (Focus/Active)**: `BorderBrush="{StaticResource PrimaryBorderBrush}"` (`#FDD8B3`) hoặc `PrimaryAccentBrush` (`#ED7A1C`).
- **Cảnh báo lỗi (Error)**: `BorderThickness="1"`, `BorderBrush="{StaticResource DangerBorderBrush}"` (`#FECACA`).
- **Hiệu ứng kính (Glass)**: `BorderThickness="1"`, `BorderBrush="{StaticResource BadgeBorderBrush}"` (`#4DFFFFFF`).

---

## 6. ELEVATION & SHADOWS

Chỉ sử dụng shadow cho các bề mặt nổi có chủ đích để tạo chiều sâu trực quan:

| Token | Thông số DropShadowEffect | Đối tượng sử dụng |
| :--- | :--- | :--- |
| `shadow-glow` | `BlurRadius="8" Color="#ED7A1C" Opacity="0.5" ShadowDepth="0"` | Chấm PIN active, Đèn mạng (`#34D399` / `#EF4444` Opacity 0.8) |
| `shadow-sm` | `BlurRadius="8" Color="#ED7A1C" Opacity="0.35" ShadowDepth="2" Direction="270"` | Tab Segmented đang chọn, Nút hành động nổi |
| `shadow-md` | `BlurRadius="28" Color="#0F172A" Opacity="0.14" ShadowDepth="6" Direction="270"` | `ToastNotification`, Dropdown menu xổ xuống |
| `shadow-lg` | `BlurRadius="40" Color="#1E0A02" Opacity="0.25" ShadowDepth="15" Direction="270"` | Card chính đăng nhập, Popup Dialog modal |

---

## 7. COMPONENT SPECIFICATIONS

### 7.1. Button Components
1. **Primary Button (`PrimaryButtonStyle`)**:
   - **Height**: `48px` - `50px`, **CornerRadius**: `12px`.
   - **Background**: `PrimaryBrush` (`#ED7A1C`), **Foreground**: `White`, **FontWeight**: `Bold`, **FontSize**: `15px`.
   - **Hover**: Background chuyển sang `PrimaryHoverBrush` (`#D6680D`).
   - **Pressed**: Background chuyển sang `PrimaryPressedBrush` (`#B75304`).
   - **Disabled**: Background `InputBorderBrush` (`#E2E8F0`), Foreground `TextPlaceholderBrush` (`#94A3B8`).
   - **Loading**: Hiện `ui:ProgressRing` (trắng, `20x20`) bên cạnh chữ "ĐANG XỬ LÝ...".

2. **Numpad Button (`NumpadButtonStyle`)**:
   - **Height**: `54px`, **CornerRadius**: `14px`, **Border**: `1px` `#E2E8F0`.
   - **Background**: `InputBackgroundBrush` (`#F8FAFC`), **Foreground**: `TextSecondaryBrush` (`#334155`), **FontSize**: `20px`.
   - **Tactile Feedback**: Khi nhấn (`IsPressed="True"`), co lại `ScaleTransform` `0.93` tạo độ nhún thực tế.
   - **Hover**: Background `PrimaryLightBrush` (`#FFF7ED`), Border `PrimaryBorderBrush` (`#FDD8B3`), Foreground `PrimaryBrush`.

3. **Segmented Tab Button (`SegmentedTabButtonStyle`)**:
   - **Height**: `44px`, **CornerRadius**: `10px`.
   - **Inactive**: Background `Transparent`, Foreground `TextMutedBrush` (`#64748B`).
   - **Active**: Background `PrimaryBrush` (`#ED7A1C`), Foreground `White`, kèm `shadow-sm` glow cam.

4. **Ghost / Link Button**:
   - Background `Transparent`, Border `0`, Foreground `PrimaryBrush` (`#ED7A1C`), `FontWeight="SemiBold"`. Dùng cho "Quên mật khẩu?", "Thử lại".

### 7.2. Input Components
1. **Text Input (`TextInputField`)**:
   - Tọa lạc tại: `POS.WinUI.Views.Shared.TextInputField`.
   - **Height**: `50px`, **CornerRadius**: `12px`, **Background**: `#F8FAFC`, **Border**: `1px` `#E2E8F0`.
   - **Cấu trúc**: Icon bên trái (`ui:SymbolIcon`, `FontSize="18"`, `TextPlaceholderBrush`), ô TextBox trong suốt, nhãn Placeholder tự động ẩn/hiện.
   - **Properties**: `Icon` (`SymbolRegular`), `PlaceholderText` (`string`), `Text` (`string, TwoWay`).

2. **Password Input (`PasswordInputField`)**:
   - Tọa lạc tại: `POS.WinUI.Views.Auth.Components.PasswordInputField`.
   - **Height**: `50px`, **CornerRadius**: `12px`, **Background**: `#F8FAFC`, **Border**: `1px` `#E2E8F0`.
   - **Cấu trúc**: Icon khoá (`LockClosed24`) trái + PasswordBox/TextBox toggle + Nút mắt (`Eye24` / `EyeOff24`) phải.

3. **PIN Input & Pad (`PinInput`)**:
   - Tọa lạc tại: `POS.WinUI.Views.Auth.Components.PinInput`.
   - Gồm 6 chấm tròn PIN (`PinDotStyle`, `16x16`, `CornerRadius="8"`) và lưới Numpad 3x4 (1-9, Xóa hết, 0, Backspace).

### 7.3. Feedback & Status Components
1. **Toast Notification (`ToastNotification`)**:
   - Tọa lạc tại: `POS.WinUI.Views.Shared.ToastNotification`.
   - **Position**: `VerticalAlignment="Top"`, `Margin="0,24,0,0"`, `Panel.ZIndex="999"`.
   - **Animation**: Tích hợp Storyboard `CubicEase` trượt xuống (`Y: -40 -> 0`) khi xuất hiện và mờ dần khi đóng.
   - **Style**: Nền trắng, bo tròn `16px`, viền `#E2E8F0`, bóng đổ `shadow-md`, biểu tượng dấu kiểm xanh `#10B981`.

2. **Error Banner**:
   - Nền `DangerBgColor` (`#FEF2F2`), viền `DangerBorderColor` (`#FECACA`), bo góc `12px`, padding `14,10`.
   - Biểu tượng `ErrorCircle24` màu `#DC2626` bên trái, nội dung lỗi màu `#B91C1C` bên phải.

3. **Network Status Bar (`NetworkStatusBar`)**:
   - Tọa lạc tại: `POS.WinUI.Views.Shared.NetworkStatusBar`.
   - Chấm tròn `9x9` phát sáng: Xanh ngọc `#34D399` khi Online, Đỏ `#EF4444` khi Offline. Nút bấm "Thử lại" màu vàng `#FDE047` tự động xuất hiện khi mất mạng.

---

## 8. APPLICATION LAYOUT & SHELL ARCHITECTURE

### 8.1. Shell Architecture (`MainWindow.xaml`)
Ứng dụng sử dụng kiến trúc **Single Fluent Window**:
```text
MainWindow (ui:FluentWindow - MinWidth: 1000, MinHeight: 680, Size: 1200x800)
 ├── Row 0: ui:TitleBar (Tích hợp title, logo và các nút min/max/close chuẩn Windows Fluent)
 └── Row 1: ContentControl (Content="{Binding CurrentView}")
```
- Điều hướng toàn quyền thông qua `INavigationService.NavigateTo<TView>()`, không tạo nhiều cửa sổ rời rạc.

### 8.2. Screen Layout Patterns

#### Pattern A: Auth / Split Hero Layout (Màn hình Đăng nhập)
```text
Grid (Background: Login Background Image + Glass Overlay)
 ├── ToastNotification (ZIndex: 999, Top Center)
 └── 2-Column Grid (Margin: 48, 32)
      ├── Left Column (*): Brand Hero (Logo, Slogan, Feature Badges, Version, Network Status)
      └── Right Column (480px): Floating White Card (Radius: 28px, Header, Segmented Tab, Form, CTA)
```

#### Pattern B: POS Cashier Main Screen (Màn hình Bán hàng chính - Quy chuẩn tương lai)
```text
Grid (Shell Content Area)
 ├── Top Bar: Store Info, Cashier Name, Shift Status, Search Product Bar
 └── Main Content Area (2 Columns)
      ├── Left Column (Product Grid / Catalog & Quick Categories):
      │    ├── Category Pills (Horizontal Scroll)
      │    └── Product Card Grid (WrapPanel / UniformGrid)
      └── Right Column (Cart & Bill Panel - 380px to 420px):
           ├── Customer Info Bar (Search / Quick Add Member)
           ├── Cart Item List (DataGrid / ScrollViewer)
           ├── Payment Summary (Subtotal, Discount, VAT, Grand Total)
           └── Action Buttons (F1 Thanh toán, F2 Tách hoá đơn, F4 Huỷ)
```

#### Pattern C: Management / List Page (Màn hình Quản lý Sản phẩm / Kho / Khách hàng)
```text
Grid (Padding: 24)
 ├── Header: Page Title + Description + Primary Action Button ("+ Thêm mới")
 ├── Filter & Search Bar: TextInputField (Search) + Dropdown Filters + Export Button
 ├── Data Table: Styled DataGrid with Alternating Row Colors
 └── Footer: Pagination & Total Record Count
```

---

## 9. ICONOGRAPHY & ASSETS

### 9.1. Icon Library
- **Thư viện chuẩn duy nhất**: `WPF-UI` Fluent Icons (`xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`, thẻ `<ui:SymbolIcon Symbol="..." />`).
- **Tuyệt đối không** nhúng thêm thư viện ngoài như FontAwesome hay Material Icons.

### 9.2. Bảng ánh xạ Icon thường dùng
| Chức năng | Symbol Name | Size khuyến nghị |
| :--- | :--- | :--- |
| **Cửa hàng / Chi nhánh** | `BuildingShop24` | `16px` (dropdown), `26px` (logo) |
| **Tài khoản / Khách hàng** | `Person24` | `18px` |
| **Khoá / Mật khẩu** | `LockClosed24` | `18px` |
| **Hiện mật khẩu** | `Eye24` | `18px` |
| **Ẩn mật khẩu** | `EyeOff24` | `18px` |
| **Lỗi / Cảnh báo** | `ErrorCircle24` | `18px` |
| **Thành công / Xác nhận** | `Checkmark24` | `16px` |
| **Mũi tên tiếp tục** | `ArrowRight24` | `16px` |
| **Xoá lùi / Backspace** | `Backspace24` | `20px` |
| **Làm mới / Thử lại** | `ArrowClockwise24` | `14px` |
| **Tính năng Nhanh chóng** | `Flash24` | `16px` |
| **Tính năng Thông minh** | `Sparkle24` | `16px` |
| **Tính năng Tin cậy** | `ShieldCheckmark24` | `16px` |
| **Wifi / Trạng thái kết nối**| `Wifi124` | `14px` |

---

## 10. MVVM & WPF/WPFUI CODE CONVENTIONS

### 10.1. Naming Conventions
- **Views**: Đặt trong `src/POS.WinUI/Views/<Feature>/<Name>View.xaml` (Ví dụ: `LoginView.xaml`, `PosMainView.xaml`).
- **ViewModels**: Đặt trong `src/POS.WinUI/ViewModels/<Feature>/<Name>ViewModel.cs` (Ví dụ: `LoginViewModel.cs`).
- **Shared Components**: Đặt trong `src/POS.WinUI/Views/Shared/<ComponentName>.xaml` (Ví dụ: `TextInputField.xaml`).
- **Styles**: Đặt trong `src/POS.WinUI/Styles/` với hậu tố `Style` (Ví dụ: `PrimaryButtonStyle`, `NumpadButtonStyle`).
- **Brushes**: Định nghĩa trong `Colors.xaml` với hậu tố `Brush` (Ví dụ: `PrimaryBrush`, `InputBackgroundBrush`).
- **Converters**: Đặt trong `src/POS.WinUI/Converters/` và đăng ký tại `Converters.xaml` (Ví dụ: `BoolToVis`, `NullOrEmptyToVis`).

### 10.2. Reusable Shared Assets Registry
| Tên Resource / Component | Vị trí tập tin | Mục đích & Cách tái sử dụng |
| :--- | :--- | :--- |
| `PrimaryButtonStyle` | `Styles/Styles.xaml` | Nút hành động chính (`Style="{StaticResource PrimaryButtonStyle}"`) |
| `NumpadButtonStyle` | `Styles/Styles.xaml` | Phím số cảm ứng có hiệu ứng nhấn nhún |
| `SegmentedTabButtonStyle` | `Styles/Styles.xaml` | Nút tab chọn chế độ |
| `PinDotStyle` | `Styles/Styles.xaml` | Chấm hiển thị trạng thái số PIN |
| `TextInputField` | `Views/Shared/TextInputField.xaml` | Ô nhập text chuẩn kèm icon và placeholder |
| `PasswordInputField` | `Views/Auth/Components/PasswordInputField.xaml` | Ô nhập mật khẩu có sẵn nút ẩn/hiện |
| `PinInput` | `Views/Auth/Components/PinInput.xaml` | Cụm 6 chấm PIN + bàn phím số 3x4 |
| `NetworkStatusBar` | `Views/Shared/NetworkStatusBar.xaml` | Thanh trạng thái kết nối server và nút retry |
| `ToastNotification` | `Views/Shared/ToastNotification.xaml` | Toast thông báo thành công có hoạt cảnh trượt mượt mà |
| `BoolToVis` | `Styles/Converters.xaml` | Chuyển đổi `bool` sang `Visibility.Visible/Collapsed` |
| `InverseBoolToVis` | `Styles/Converters.xaml` | Chuyển đổi `!bool` sang `Visibility.Visible/Collapsed` |
| `NullOrEmptyToVis` | `Styles/Converters.xaml` | Ẩn/hiện dựa trên chuỗi rỗng hoặc `null` |

---

## 11. DOs AND DON'Ts

| DO (Nên làm) | DON'T (Tuyệt đối không làm) |
| :--- | :--- |
| ✅ Luôn kiểm tra và tái sử dụng `TextInputField`, `PrimaryButtonStyle`, `Colors.xaml`. | ❌ Không tự gõ mã màu HEX (`#FF8800`, `#123456`) trong từng file XAML. |
| ✅ Sử dụng `ui:SymbolIcon` từ WPF-UI. | ❌ Không thêm các thư viện Icon bên ngoài (FontAwesome, MaterialDesign). |
| ✅ Sử dụng `INavigationService` để điều hướng màn hình. | ❌ Không tự ý tạo `new Window().Show()` rời rạc làm vỡ cấu trúc FluentWindow. |
| ✅ Bọc các ô nhập liệu bằng chiều cao chuẩn (`50px`) và bo góc (`12px`). | ❌ Không tạo input vuông vức `Height="25"` gây khó thao tác cảm ứng. |
| ✅ Bind dữ liệu thông qua ViewModel với `CommunityToolkit.Mvvm`. | ❌ Không viết code xử lý nghiệp vụ/API trực tiếp trong code-behind (`.xaml.cs`). |
| ✅ Xử lý đầy đủ các trạng thái: Default, Hover, Pressed, Loading, Disabled, Error. | ❌ Không để nút bấm trơ khi người dùng click (thiếu hiệu ứng phản hồi). |

---

## 12. RULES FOR AI UI GENERATION

Khi tạo mới bất kỳ màn hình hoặc component XAML nào, AI **bắt buộc** phải tuân thủ nghiêm ngặt 15 nguyên tắc sau:

1. **Đọc Style Guide này trước**: Không tự đoán hay tự chế style mới nếu chưa đọc file này.
2. **Kiểm tra kho tài nguyên**: Luôn kiểm tra `Styles/Styles.xaml`, `Styles/Colors.xaml` và thư mục `Views/Shared/` trước khi viết XAML.
3. **Tuyệt đối không đổi màu hệ thống**: Chỉ dùng các Brush đã khai báo (`PrimaryBrush`, `TextPrimaryBrush`, `CardBackgroundBrush`...).
4. **Không tự ý thêm font**: Chỉ dùng font mặc định của WPF-UI (Segoe UI / Segoe UI Variable).
5. **Tuân thủ Spacing Scale**: Chỉ sử dụng các bước khoảng cách: `4`, `8`, `12`, `16`, `24`, `32`, `48`.
6. **Tuân thủ Border Radius**: Chỉ sử dụng các mức: `8px`, `10px`, `12px`, `14px`, `16px`, `28px`.
7. **Không tạo Duplicate Style**: Nếu một style đã có (như `PrimaryButtonStyle`), bắt buộc phải dùng `StaticResource`.
8. **Đảm bảo tính nhất quán (Visually Consistent)**: Giao diện mới phải trông giống như cùng một tác giả thiết kế với `LoginView.xaml`.
9. **Kích thước tối thiểu cho cảm ứng POS**: Các nút và ô bấm phải có chiều cao tối thiểu `44px - 50px`.
10. **Tách biệt Component**: Nếu một cụm UI xuất hiện ở >= 2 màn hình, tạo UserControl trong `Views/Shared/`.
11. **Sử dụng đúng Converter**: Dùng `BoolToVis`, `InverseBoolToVis`, `NullOrEmptyToVis` từ `Converters.xaml`.
12. **Không phá vỡ Shell**: Mọi view chính phải là `UserControl` để đưa vào `ContentControl` của `MainWindow`.
13. **Báo cáo lý do nếu cần style mới**: Nếu thực sự cần style mới, phải giải thích rõ tại sao các style hiện tại không đáp ứng được.
14. **Hỗ trợ xử lý lỗi trực quan**: Mọi form phải có vùng hiển thị Error Banner dùng `DangerBgBrush` + `DangerBorderBrush`.
15. **Kiểm tra nghiệm thu (Self-Check)**: Sau khi sinh mã XAML, đối chiếu lại với bảng Quick Reference dưới đây.

---

## 13. FUTURE IMPROVEMENTS & STANDARDIZATION PROPOSALS

Trong quá trình phát triển các Sprint tiếp theo (Màn hình bán hàng chính, Quản lý kho, Khách hàng, Báo cáo), nhóm phát triển đề xuất mở rộng chuẩn hoá như sau:

1. **Secondary & Danger Button Styles**:
   - Bổ sung `SecondaryButtonStyle` (Nền xám nhạt `#F1F5F9`, chữ xám đậm `#334155`, bo góc `12px`) cho các nút phụ như "Huỷ bỏ", "Đóng ca".
   - Bổ sung `DangerButtonStyle` (Nền đỏ `#DC2626`, chữ trắng) cho các nút nguy hiểm như "Huỷ đơn", "Xoá sản phẩm".
2. **Standard DataGrid Style**:
   - Định nghĩa `PosDataGridStyle` trong `Styles.xaml` với chiều cao dòng tối thiểu `44px`, màu dòng xen kẽ (`#FFFFFF` và `#F8FAFC`), header nền `#F1F5F9` chữ đậm.
3. **Numeric Keypad Dialog**:
   - Đóng gói `PinInput` thành `QuickNumericKeypadDialog` để tái sử dụng khi thu ngân nhập số lượng món hoặc số tiền khách đưa.

---

## 14. QUICK REFERENCE

Tra cứu nhanh các thông số thiết kế:

| Mục | Giá trị chuẩn |
| :--- | :--- |
| **Primary Color** | `#ED7A1C` (`PrimaryBrush`) |
| **Card Background** | `#FFFFFF` (`CardBackgroundBrush`) |
| **Input Background & Border** | `#F8FAFC` (`InputBackgroundBrush`) & `#E2E8F0` (`InputBorderBrush`) |
| **Text Primary & Secondary** | `#0F172A` (`TextPrimaryBrush`) & `#334155` (`TextSecondaryBrush`) |
| **Text Muted & Placeholder** | `#64748B` (`TextMutedBrush`) & `#94A3B8` (`TextPlaceholderBrush`) |
| **Error Colors** | Nền `#FEF2F2`, Viền `#FECACA`, Chữ `#B91C1C`, Icon `#DC2626` |
| **Success Color** | `#10B981` (`SuccessBrush`) |
| **Main Window Size** | Width: `1200`, Height: `800`, MinWidth: `1000`, MinHeight: `680` |
| **Input Field Height** | `50px`, CornerRadius `12px` |
| **Primary Button Height** | `48px` - `50px`, CornerRadius `12px` |
| **Numpad Button Height** | `54px`, CornerRadius `14px`, Scale `0.93` khi nhấn |
| **Card Corner Radius** | `28px` (Main floating card) |
| **Default Spacings** | `4px`, `8px`, `12px`, `16px`, `24px`, `32px`, `48px` |
| **Icon Library** | `WPF-UI` (`ui:SymbolIcon`) |

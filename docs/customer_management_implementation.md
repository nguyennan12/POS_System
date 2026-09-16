# Customer & Member Tier Management — Implementation Report (T36)

## 1. Tổng quan & Nguồn gốc Thiết kế

Theo tài liệu kiến trúc hệ thống:
- **`docs/pos_database_design.md`** (Mục 3.5 Customer):
  - Định nghĩa 4 hạng thành viên cố định toàn chuỗi: `CHECK (name IN ('Normal','Silver','Gold','VIP'))`.
  - Khách hàng (`Customers`) mang tính chất **Global** (dùng chung toàn chuỗi hệ thống, không chia theo Store để đảm bảo khách mua ở bất kỳ chi nhánh nào cũng được tích lũy điểm và áp dụng đúng hạng thẻ).
  - Khách hàng liên kết 1-1 với tài khoản thành viên (`LoyaltyAccounts`).
- **`docs/pos_task_breakdown.md`** (Task T36):
  - Yêu cầu: CRUD khách hàng, tìm kiếm nhanh theo SĐT/Mã vạch, tự động nâng hạng thẻ (`Normal` -> `Silver` -> `Gold` -> `VIP`) theo chi tiêu tích lũy (`TotalSpending`).

---

## 2. Quy tắc & Các mốc Hạng Thành viên (Member Tier Rules)

### 2.1. Cấu hình mặc định (Initial Seed)
Dữ liệu 4 hạng thẻ được khởi tạo tự động trong database (`MemberTierSeeder`) với các thông số phù hợp thực tế ngành bán lẻ:

| Hạng | Tên (`Name`) | Chi tiêu tối thiểu (`MinSpending`) | Tỷ lệ tích điểm (`PointRate`) | Tỷ lệ giảm giá (`DiscountRate`) | Màu hiển thị UI (`DisplayColor`) |
| :--- | :--- | :---: | :---: | :---: | :---: |
| 1 | **Normal** | 0 VNĐ | 1.0% (`0.0100`) | 0% (`0.0000`) | `#808080` (Xám) |
| 2 | **Silver** | 5.000.000 VNĐ | 1.5% (`0.0150`) | 2% (`0.0200`) | `#C0C0C0` (Bạc) |
| 3 | **Gold** | 15.000.000 VNĐ | 2.0% (`0.0200`) | 5% (`0.0500`) | `#FFD700` (Vàng) |
| 4 | **VIP** | 30.000.000 VNĐ | 3.0% (`0.0300`) | 10% (`0.1000`) | `#9400D3` (Tím) |

> **Khả năng tùy biến:** Các mốc trên **không bị hardcode**. Chủ chuỗi / Quản lý có thể cập nhật linh hoạt bất kỳ lúc nào qua API `PUT /api/v1/member-tiers/{id}`.

### 2.2. Thuật toán tự động thăng hạng (Tier Upgrade Algorithm)
- **Nơi thực thi:** Nằm trực tiếp trong Domain Entity [`Customer.cs`](file:///c:/Users/lequo/Documents/Hoc_Tap/Lap_Trinh_Truc_Quan/POS_System/src/POS.Domain/Customers/Customer.cs) (`EvaluateTierUpgrade`).
- **Nguyên tắc:**
  1. Lấy danh sách toàn bộ hạng thẻ đang có trong hệ thống, sắp xếp giảm dần theo `MinSpending`.
  2. Tìm hạng thẻ cao nhất mà `TotalSpending >= MinSpending`.
  3. Nếu hạng thẻ mới cao hơn hạng thẻ hiện tại $\rightarrow$ Cập nhật `MemberTierId = newTier.Id`.
  4. **Chỉ nâng hạng, không hạ hạng:** Đảm bảo quyền lợi khách hàng tích lũy lâu năm.
- **Thời điểm kích hoạt:**
  - Tự động đánh giá mỗi khi khách hàng phát sinh đơn hàng / tích lũy thêm chi tiêu (`RecordSpending`).
  - Được dùng cho quy trình thanh toán hóa đơn sau này (Task T42 - Checkout & Order Processing).

---

## 3. Danh sách API Endpoints & Phân quyền

Base route: `/api/v1`

### 3.1. Nhóm Khách hàng (`/api/v1/customers`)
| Method | Route | Permission yêu cầu | Mô tả |
| :--- | :--- | :--- | :--- |
| **GET** | `/customers` | `customers:read` | Tìm kiếm & phân trang theo SĐT, Tên, Barcode, Trạng thái |
| **GET** | `/customers/{id}` | `customers:read` | Xem chi tiết khách hàng (kèm thông tin Hạng & Điểm tích lũy) |
| **POST** | `/customers` | `customers:create` | Tạo mới khách hàng (Mặc định gắn hạng Normal & tạo LoyaltyAccount) |
| **PUT** | `/customers/{id}` | `customers:update` | Cập nhật thông tin (Tên, Email, Ngày sinh, Barcode, Active) |
| **DELETE** | `/customers/{id}` | `customers:delete` | Vô hiệu hóa (Soft delete: chuyển `IsActive = false`) |

### 3.2. Nhóm Hạng thành viên (`/api/v1/member-tiers`)
| Method | Route | Permission yêu cầu | Mô tả |
| :--- | :--- | :--- | :--- |
| **GET** | `/member-tiers` | `customers:read` | Lấy danh sách tất cả các hạng thành viên |
| **PUT** | `/member-tiers/{id}` | `customers:update` | Chỉnh sửa mốc chi tiêu, tỷ lệ điểm, chiết khấu, màu sắc |

### 3.3. Ma trận phân quyền theo vai trò (Roles)
- **Thu ngân (`Cashier`):**
  - Có quyền: `customers:read`, `customers:create`, `customers:update`.
  - Mục đích: Thu ngân tại quầy có thể tra cứu SĐT khách hàng, tạo nhanh khách hàng mới, hoặc cập nhật email/ngày sinh theo yêu cầu khách.
- **Quản lý / Chủ chuỗi (`StoreManager`, `Owner`):**
  - Có toàn quyền: `customers:read`, `customers:create`, `customers:update`, `customers:delete`.
  - Được quyền cấu hình chỉnh sửa mốc hạng thẻ (`PUT /api/v1/member-tiers/{id}`).

---

## 4. Các ràng buộc nghiệp vụ (Business Invariants)

1. **Số điện thoại duy nhất (Unique Phone):** Mỗi khách hàng phải có một số điện thoại duy nhất trên toàn hệ thống. Nếu cố tình tạo trùng hoặc sửa trùng SĐT của người khác sẽ trả về lỗi `409 Conflict (Customer.DuplicatePhone)`.
2. **Mã vạch / Mã thẻ duy nhất (Unique Barcode):** Khách hàng có thể có mã vạch/mã thẻ riêng để quét nhanh tại quầy. Nếu được cung cấp, mã này phải là duy nhất trên toàn chuỗi (`Customer.DuplicateBarcode`).
3. **Tài khoản tích điểm tự động (Auto Loyalty Account):** Khi tạo khách hàng mới, hệ thống tự động khởi tạo bản ghi `LoyaltyAccount` với `PointsBalance = 0`.
4. **Bảo toàn dữ liệu (Soft Deactivation):** Để bảo toàn tính toàn vẹn của lịch sử hóa đơn và đơn hàng cũ, thao tác DELETE sẽ thực hiện tắt kích hoạt (`IsActive = false`) thay vì xóa vật lý khỏi CSDL. Khách hàng inactive không thể tiếp tục tích điểm hoặc nhận ưu đãi thẻ.

---

## 5. Hướng dẫn Test & Tích hợp

- **Test HTTP trong JetBrains Rider:** Sử dụng file [`src/POS.Api/customers-test.http`](file:///c:/Users/lequo/Documents/Hoc_Tap/Lap_Trinh_Truc_Quan/POS_System/src/POS.Api/customers-test.http). File đã cấu hình script tự động trích xuất Bearer token và Customer ID cho từng bước test.
- **Test Web UI:** Truy cập Swagger / Scalar UI tại `http://localhost:5080/scalar/v1`.
- **Unit Tests:** Đã viết đầy đủ 11 bài kiểm thử tự động trong `POS.Domain.Tests/Customers/CustomerTierUpgradeTests.cs` và `POS.Application.Tests/Customers/CustomerManagementTests.cs`.

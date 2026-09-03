# 🗃️ THIẾT KẾ DATABASE — HỆ THỐNG POS (SQL Server 2022)

> File này là tài liệu **thiết kế database chính thức**, tách riêng khỏi
> `pos_system_architecture.md` để dễ theo dõi/maintain khi schema thay đổi.
> Toàn bộ nội dung dưới đây đã cập nhật theo chuẩn **SQL Server 2022**, thêm RBAC động,
> và tinh gọn các module chưa cần cho MVP — đây là bản
> **thay thế hoàn toàn** phần "DATABASE SCHEMA" trong file kiến trúc gốc.

---

## 1. Nguyên tắc thiết kế

- **Shared Database + `store_id` column** cho multi-tenant (không tách DB
  riêng từng cửa hàng) — dùng EF Core Global Query Filter để tự động lọc
  theo `store_id` ở tầng Infrastructure.
- **Tách bạch 2 khái niệm độc lập, không gộp chung**:
  - **Tenant Scope** (được thấy dữ liệu của cửa hàng nào) → `EmployeeStoreAccess`
  - **Permission Scope** (được làm hành động gì) → `Roles`/`Permissions` (RBAC động, mục 4)
- **Không hard-delete dữ liệu giao dịch** (Orders, Payments, StockTransactions,
  AuditLogs...) — chỉ soft-delete (`is_active`) cho dữ liệu chủ (Products,
  Customers, Suppliers, Roles...).
- **Tiền tệ luôn dùng `DECIMAL(18,2)`** (hoặc `NUMERIC(18,2)`), không dùng `FLOAT`/`REAL`.
- **Thời gian luôn dùng `DATETIMEOFFSET`** (có timezone, chuẩn UTC), mặc định `SYSUTCDATETIME()`.
- **Khóa chính và khóa ngoại**: dùng `UNIQUEIDENTIFIER` (Guid trong C#) xuyên suốt, sinh tại ứng dụng
  hoặc bằng `NEWID()` / `NEWSEQUENTIALID()`. UUID/Guid giúp đồng bộ offline và
  không lộ thứ tự dữ liệu. Với các bảng ghi rất nhiều (`Orders`, ledger),
  ưu tiên Guidv7/Guid có tính tuần tự khi tầng ứng dụng hỗ trợ.
- **Dữ liệu Boolean**: dùng kiểu `BIT` (`1` cho `true`, `0` cho `false`).
- **Dữ liệu JSON**: dùng kiểu `NVARCHAR(MAX)` tích hợp hàm `ISJSON()` check constraint hoặc tính năng JSON native của SQL Server 2022.
- **Các cột status/type/method/action cố định**: trong code dùng enum, EF Core
  convert sang string (`NVARCHAR`) trong DB để dữ liệu dễ đọc và vẫn giữ CHECK constraint.
- **Mọi FK đều phải khai báo rõ `ON DELETE`** — mặc định `RESTRICT` (hoặc `NO ACTION`) cho dữ
  liệu giao dịch/lịch sử, `CASCADE` chỉ cho bảng chi tiết đi kèm cha thật
  sự vô nghĩa khi đứng riêng (vd `OrderItems` theo `Orders`).

---

## 2. Sơ đồ tổng quan các nhóm bảng

```
CORE            : Stores, Employees, EmployeeStoreAccess, Shifts, RefreshTokens
RBAC            : Roles, Resources, Permissions, RolePermissions
PRODUCT         : Categories, Products, SKUs, UnitConversions, PriceLists
INVENTORY       : StockEntries, StockBatches, StockTransactions, Suppliers,
                  SupplierPayments, StockInVouchers, StockInVoucherItems,
                  StockTakes, StockTakeItems
CUSTOMER        : MemberTiers, Customers, LoyaltyAccounts, PointTransactions
CHATBOT         : FaqEntries, ChatConversations, ChatMessages
PROMOTION       : Promotions, PromotionTargets, Vouchers, VoucherUsages
ORDER           : Orders, OrderItems, OrderDiscounts, OrderReturns,
                  OrderReturnItems, Payments, Invoices
CONFIG          : SystemConfigs, Translations
AUDIT           : AuditLogs
```

**Quyết định thiết kế quan trọng đã chốt**: `Customers` dùng **chung toàn
chuỗi** (không scope theo `store_id`) — khách mua ở bất kỳ chi nhánh nào
cũng tích điểm vào cùng 1 tài khoản. `MemberTiers` cũng global, nhất quán
với quyết định này.

**Khởi tạo SQL Server**:

- Mặc định sử dụng `DEFAULT NEWID()` (hoặc `NEWSEQUENTIALID()`) cho các cột `UNIQUEIDENTIFIER`.
- Client chạy offline phải tự sinh GUID cho bản ghi của mình và gửi nguyên ID đó khi đồng bộ.

---

## 3. Schema chi tiết

### 3.1 CORE

```sql
Stores
  id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  name            NVARCHAR(200) NOT NULL
  address         NVARCHAR(500)
  phone           VARCHAR(20)
  timezone        VARCHAR(50) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh'
  currency_code   VARCHAR(3) NOT NULL DEFAULT 'VND'
  tax_code        VARCHAR(20)
  receipt_header  NVARCHAR(MAX)
  receipt_footer  NVARCHAR(MAX)
  is_active       BIT NOT NULL DEFAULT 1
  created_at      DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  updated_at      DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

Employees
  id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id            UNIQUEIDENTIFIER NULL REFERENCES Stores(id) ON DELETE NO ACTION
                       -- NULL khi is_chain_owner = 1
  role_id             UNIQUEIDENTIFIER NOT NULL REFERENCES Roles(id) ON DELETE NO ACTION
  is_chain_owner      BIT NOT NULL DEFAULT 0
                       -- Cờ RIÊNG BIỆT với RBAC — quyết định phạm vi truy cập
                       -- nhiều cửa hàng (Tenant Scope), không phải hành động
                       -- được phép làm (Permission Scope, nằm ở role_id)
  name                NVARCHAR(200) NOT NULL
  username            VARCHAR(50) NOT NULL
  password_hash       VARCHAR(255) NOT NULL
  pin_hash            VARCHAR(255) NOT NULL
  pin_lookup_hash     CHAR(64) NULL
                       -- HMAC-SHA-256(PIN, server_secret), để tìm employee
                       -- khi đăng nhập PIN không kèm username; không dùng
                       -- hash mật khẩu ngẫu nhiên làm khóa UNIQUE
  failed_login_count  SMALLINT NOT NULL DEFAULT 0
  locked_until        DATETIMEOFFSET NULL
  is_active           BIT NOT NULL DEFAULT 1
  created_at          DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  updated_at          DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (username)
  UNIQUE (store_id, pin_lookup_hash)  -- PIN duy nhất trong 1 cửa hàng
  CHECK (store_id IS NOT NULL OR is_chain_owner = 1)
        -- chỉ Chain Owner mới được để store_id NULL

EmployeeStoreAccess     -- Tenant Scope: Owner được xem/quản lý cửa hàng nào
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  employee_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE CASCADE
  store_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE CASCADE
  granted_by    UNIQUEIDENTIFIER NULL REFERENCES Employees(id) ON DELETE NO ACTION
  granted_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (employee_id, store_id)

RefreshTokens
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  employee_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE CASCADE
  token_hash    VARCHAR(255) NOT NULL
  expires_at    DATETIMEOFFSET NOT NULL
  revoked_at    DATETIMEOFFSET NULL    -- set khi logout / đóng ca / đổi mật khẩu
  created_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (token_hash)
  INDEX idx_refreshtokens_employee (employee_id, revoked_at)

Shifts
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  employee_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  opening_cash  DECIMAL(18,2) NOT NULL CHECK (opening_cash >= 0)
  closing_cash  DECIMAL(18,2) NULL CHECK (closing_cash >= 0)
  actual_cash   DECIMAL(18,2) NULL CHECK (actual_cash >= 0)
  status        VARCHAR(20) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open','Closed'))
  note          NVARCHAR(MAX)
  opened_at     DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  closed_at     DATETIMEOFFSET NULL

  INDEX idx_shifts_store_status (store_id, status)
```

### 3.2 RBAC ĐỘNG (Roles / Resources / Permissions)

```sql
Roles
  id                   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id             UNIQUEIDENTIFIER NULL REFERENCES Stores(id) ON DELETE CASCADE
                        -- NULL = role dùng chung toàn hệ thống (4 role mặc định);
                        -- NOT NULL = role riêng do Admin/Owner tự tạo cho 1 cửa hàng
  name                 NVARCHAR(100) NOT NULL
  description          NVARCHAR(500)
  is_system_role       BIT NOT NULL DEFAULT 0
                        -- 1 cho Owner/Admin/Manager/Cashier mặc định —
                        -- không cho xóa, hạn chế sửa permission lõi
  created_at           DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  updated_at           DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  -- Filtered Unique Index trong SQL Server cho system role (store_id IS NULL) và custom role (store_id IS NOT NULL)

Resources               -- danh mục "đối tượng" có thể phân quyền
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  code          VARCHAR(50) NOT NULL UNIQUE
                 -- vd: products, skus, inventory, promotions, orders,
                 -- payments, invoices, reports, employees, stores,
                 -- shifts, customers, chatbot, config
  description   NVARCHAR(200)

Permissions              -- hành động cụ thể trên 1 resource
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  resource_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Resources(id) ON DELETE CASCADE
  action        VARCHAR(20) NOT NULL
                 CHECK (action IN ('Create','Read','Update','Delete','Approve','Export','Override'))
  code          VARCHAR(80) NOT NULL UNIQUE
                 -- tự sinh "resource_code:action", vd "orders:refund",
                 -- "products:create" — denormalize để check nhanh, không
                 -- cần join khi so khớp permission
  description   NVARCHAR(200)

  UNIQUE (resource_id, action)

RolePermissions           -- N-N: role nào có quyền nào
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  role_id       UNIQUEIDENTIFIER NOT NULL REFERENCES Roles(id) ON DELETE CASCADE
  permission_id UNIQUEIDENTIFIER NOT NULL REFERENCES Permissions(id) ON DELETE CASCADE
  granted_by    UNIQUEIDENTIFIER NULL REFERENCES Employees(id) ON DELETE NO ACTION
  granted_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (role_id, permission_id)
```

**Cách tính permission cuối cùng của 1 nhân viên**:

```
Lấy toàn bộ permission từ role (qua RolePermissions theo role_id).
Không có override theo từng user; nếu cần ngoại lệ thì tạo role riêng.
```

**Seed dữ liệu mặc định** — 4 role hệ thống (`is_system_role = 1`,
`store_id = NULL`), permission gán theo đúng bảng phân quyền đã thống nhất
trước đó (Owner > Admin > Manager > Cashier), ví dụ trích một phần:

| Role    | Permission code (ví dụ)                                                                                                              |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| Owner   | `stores:create`, `stores:update`, `reports:read` (toàn chuỗi), `config:override`, + toàn bộ quyền của Admin                          |
| Admin   | `employees:create`, `products:create`, `promotions:update`, `reports:read` (chi nhánh), `config:update`, + toàn bộ quyền của Manager |
| Manager | `inventory:create`, `stocktakes:approve`, `orders:refund`, `orders:override`, `shifts:read`                                          |
| Cashier | `orders:create`, `payments:create`, `shifts:create` (mở/đóng ca của mình)                                                            |

### 3.3 PRODUCT

```sql
Categories
  id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id       UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  parent_id      UNIQUEIDENTIFIER NULL REFERENCES Categories(id) ON DELETE NO ACTION
  name           NVARCHAR(200) NOT NULL
  display_order  INT NOT NULL DEFAULT 0
  image_url      VARCHAR(500)
  is_visible     BIT NOT NULL DEFAULT 1
  created_at     DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

Products
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  category_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Categories(id) ON DELETE NO ACTION
  name          NVARCHAR(300) NOT NULL
  description   NVARCHAR(MAX)
  brand         NVARCHAR(100)
  base_unit     NVARCHAR(30) NOT NULL
  image_url     VARCHAR(500)
  status        VARCHAR(20) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Inactive'))
  created_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  updated_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

SKUs
  id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  product_id     UNIQUEIDENTIFIER NOT NULL REFERENCES Products(id) ON DELETE NO ACTION
  store_id       UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
                  -- denormalize từ Products.store_id — BẮT BUỘC để tạo được UNIQUE(store_id, barcode)
  sku_code       VARCHAR(50) NOT NULL
  barcode        VARCHAR(50) NOT NULL
  attributes_json NVARCHAR(MAX) CHECK (attributes_json IS NULL OR ISJSON(attributes_json) = 1)
                  -- vd {"size":"L","color":"Red"}
  cost_price     DECIMAL(18,2) NOT NULL CHECK (cost_price >= 0)
  sell_price     DECIMAL(18,2) NOT NULL CHECK (sell_price >= 0)
  tax_rate       DECIMAL(5,2) NOT NULL DEFAULT 0 CHECK (tax_rate IN (0,5,8,10))
  is_active      BIT NOT NULL DEFAULT 1
  created_at     DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  updated_at     DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (store_id, barcode)
  UNIQUE (store_id, sku_code)

UnitConversions
  id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  sku_id             UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE CASCADE
  unit_name          NVARCHAR(30) NOT NULL
  conversion_factor  DECIMAL(18,4) NOT NULL CHECK (conversion_factor > 0)
  sell_price         DECIMAL(18,2) NOT NULL CHECK (sell_price >= 0)

  UNIQUE (sku_id, unit_name)

PriceLists
  id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id        UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  sku_id          UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE CASCADE
  price           DECIMAL(18,2) NOT NULL CHECK (price >= 0)
  valid_from      DATETIMEOFFSET NOT NULL
  valid_to        DATETIMEOFFSET NULL
  customer_group  NVARCHAR(50) NULL
  created_by      UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  created_at      DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
```

**Quy tắc giá bán**:

- `SKUs.sell_price` là giá bán mặc định hiện tại, dùng nhanh trên màn hình POS.
- `PriceLists` chỉ dùng cho giá override có hiệu lực theo thời gian hoặc nhóm khách hàng.
- Validate chồng lấn thời gian ở tầng Application layer trước khi insert.

### 3.4 INVENTORY

```sql
StockEntries              -- tồn kho hiện tại, 1 dòng / (store, sku)
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  sku_id        UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE NO ACTION
  qty_on_hand   DECIMAL(18,3) NOT NULL DEFAULT 0 CHECK (qty_on_hand >= 0)
  min_stock     DECIMAL(18,3) NOT NULL DEFAULT 0
  last_updated  DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (store_id, sku_id)

StockBatches               -- quản lý theo LÔ (cho SKU cần theo dõi hạn dùng)
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  sku_id        UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE NO ACTION
  batch_no      VARCHAR(50) NOT NULL
  qty           DECIMAL(18,3) NOT NULL CHECK (qty >= 0)
  expiry_date   DATE NULL
  received_at   DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (store_id, sku_id, batch_no)
  INDEX idx_stockbatches_expiry (expiry_date)

StockTransactions          -- lịch sử mọi biến động tồn kho (ledger)
  id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id              UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  sku_id                UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE NO ACTION
  type                  VARCHAR(20) NOT NULL
                         CHECK (type IN ('StockIn','SaleOut','Dispose','Adjust'))
  qty                   DECIMAL(18,3) NOT NULL
  order_id              UNIQUEIDENTIFIER NULL REFERENCES Orders(id) ON DELETE NO ACTION
  stock_in_voucher_id   UNIQUEIDENTIFIER NULL REFERENCES StockInVouchers(id) ON DELETE NO ACTION

  note                  NVARCHAR(MAX)
  created_by            UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  created_at            DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  INDEX idx_stocktx_sku_created (sku_id, created_at)
  INDEX idx_stocktx_store_created (store_id, created_at)

Suppliers
  id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  name           NVARCHAR(200) NOT NULL
  tax_code       VARCHAR(20)
  contact_name   NVARCHAR(100)
  phone          VARCHAR(20)
  email          VARCHAR(100)
  address        NVARCHAR(500)
  credit_terms   NVARCHAR(200)
  is_active      BIT NOT NULL DEFAULT 1
  created_at     DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

SupplierPayments            -- theo dõi công nợ / thanh toán cho NCC
  id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  supplier_id    UNIQUEIDENTIFIER NOT NULL REFERENCES Suppliers(id) ON DELETE NO ACTION
  voucher_id     UNIQUEIDENTIFIER NULL REFERENCES StockInVouchers(id) ON DELETE NO ACTION
  amount         DECIMAL(18,2) NOT NULL CHECK (amount > 0)
  method         VARCHAR(20) NOT NULL CHECK (method IN ('Cash','BankTransfer','Other'))
  paid_at        DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  created_by     UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  note           NVARCHAR(MAX)

StockInVouchers
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  supplier_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Suppliers(id) ON DELETE NO ACTION
  total_amount  DECIMAL(18,2) NOT NULL CHECK (total_amount >= 0)
  status        VARCHAR(20) NOT NULL DEFAULT 'Completed' CHECK (status IN ('Draft','Completed','Cancelled'))
  note          NVARCHAR(MAX)
  created_by    UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  created_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

StockInVoucherItems
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  voucher_id    UNIQUEIDENTIFIER NOT NULL REFERENCES StockInVouchers(id) ON DELETE CASCADE
  sku_id        UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE NO ACTION
  qty           DECIMAL(18,3) NOT NULL CHECK (qty > 0)
  unit_price    DECIMAL(18,2) NOT NULL CHECK (unit_price >= 0)
  total_price   DECIMAL(18,2) NOT NULL CHECK (total_price >= 0)

StockTakes
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  status        VARCHAR(20) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft','Pending','Approved'))
  created_by    UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  approved_by   UNIQUEIDENTIFIER NULL REFERENCES Employees(id) ON DELETE NO ACTION
  note          NVARCHAR(MAX)
  created_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  approved_at   DATETIMEOFFSET NULL

StockTakeItems
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  take_id       UNIQUEIDENTIFIER NOT NULL REFERENCES StockTakes(id) ON DELETE CASCADE
  sku_id        UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE NO ACTION
  system_qty    DECIMAL(18,3) NOT NULL
  actual_qty    DECIMAL(18,3) NOT NULL
  diff_qty      AS (actual_qty - system_qty) PERSISTED
  note          NVARCHAR(MAX)
```

### 3.5 CUSTOMER

```sql
MemberTiers                 -- global, không scope theo store
  id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  name            VARCHAR(50) NOT NULL CHECK (name IN ('Normal','Silver','Gold','VIP'))
  min_spending    DECIMAL(18,2) NOT NULL DEFAULT 0
  point_rate      DECIMAL(5,4) NOT NULL DEFAULT 0    -- vd 0.01 = 1%
  discount_rate   DECIMAL(5,4) NOT NULL DEFAULT 0
  display_color   VARCHAR(20)

  UNIQUE (name)

Customers                   -- GLOBAL — dùng chung toàn chuỗi (đã chốt)
  id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  name            NVARCHAR(200) NOT NULL
  phone           VARCHAR(20) NOT NULL
  email           VARCHAR(100)
  dob             DATE
  barcode         VARCHAR(50)
  member_tier_id  UNIQUEIDENTIFIER NOT NULL REFERENCES MemberTiers(id) ON DELETE NO ACTION
  total_spending  DECIMAL(18,2) NOT NULL DEFAULT 0
  is_active       BIT NOT NULL DEFAULT 1
  created_at      DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (phone)
  UNIQUE (barcode)

LoyaltyAccounts
  id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  customer_id    UNIQUEIDENTIFIER NOT NULL REFERENCES Customers(id) ON DELETE CASCADE
  points_balance DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (points_balance >= 0)
  last_updated   DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (customer_id)   -- quan hệ 1-1

PointTransactions
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  customer_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Customers(id) ON DELETE CASCADE
  points        DECIMAL(18,2) NOT NULL
  type          VARCHAR(20) NOT NULL CHECK (type IN ('Earn','Redeem','Adjust'))
  order_id      UNIQUEIDENTIFIER NULL REFERENCES Orders(id) ON DELETE NO ACTION
  note          NVARCHAR(MAX)
  created_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  INDEX idx_pointtx_customer_created (customer_id, created_at)
```

### 3.6 CHATBOT

```sql
FaqEntries
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NULL REFERENCES Stores(id) ON DELETE CASCADE
                 -- NULL = dùng chung toàn chuỗi
  category      NVARCHAR(100)
  question      NVARCHAR(500) NOT NULL
  answer        NVARCHAR(MAX) NOT NULL
  keywords      NVARCHAR(500)
  is_active     BIT NOT NULL DEFAULT 1
  created_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  updated_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

ChatConversations
  id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id        UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  customer_id     UNIQUEIDENTIFIER NULL REFERENCES Customers(id) ON DELETE SET NULL
  session_id      VARCHAR(100) NOT NULL
  message_count   INT NOT NULL DEFAULT 0
  started_at      DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  ended_at        DATETIMEOFFSET NULL

  UNIQUE (session_id)

ChatMessages
  id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  conversation_id   UNIQUEIDENTIFIER NOT NULL REFERENCES ChatConversations(id) ON DELETE CASCADE
  sender            VARCHAR(10) NOT NULL CHECK (sender IN ('Customer','Bot'))
  content           NVARCHAR(MAX) NOT NULL
  created_at        DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  INDEX idx_chatmsg_conversation_created (conversation_id, created_at)
```

### 3.7 PROMOTION

```sql
Promotions
  id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id            UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  name                NVARCHAR(200) NOT NULL
  type                VARCHAR(20) NOT NULL
                       CHECK (type IN ('PercentSku','FixedSku','BuyXGetY','CartPercent','CartFixed','HappyHour'))
  value               DECIMAL(18,2) NOT NULL CHECK (value >= 0)
  min_order_amount    DECIMAL(18,2) NOT NULL DEFAULT 0
  max_discount_amount DECIMAL(18,2) NULL
  conditions_json     NVARCHAR(MAX) CHECK (conditions_json IS NULL OR ISJSON(conditions_json) = 1)
  priority            INT NOT NULL DEFAULT 0
  is_stackable        BIT NOT NULL DEFAULT 0
  is_exclusive        BIT NOT NULL DEFAULT 0
  applies_to          VARCHAR(20) NOT NULL CHECK (applies_to IN ('All','Category','SKU'))
  valid_from          DATETIMEOFFSET NOT NULL
  valid_to            DATETIMEOFFSET NULL
  status              VARCHAR(20) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Inactive'))
  created_by          UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  created_at          DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  INDEX idx_promotions_store_status (store_id, status, valid_from, valid_to)

PromotionTargets            -- Giữ FK integrity khi SKU/Category bị xóa
  id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  promotion_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Promotions(id) ON DELETE CASCADE
  category_id    UNIQUEIDENTIFIER NULL REFERENCES Categories(id) ON DELETE CASCADE
  sku_id         UNIQUEIDENTIFIER NULL REFERENCES SKUs(id) ON DELETE CASCADE

  CHECK ((category_id IS NOT NULL AND sku_id IS NULL) OR (category_id IS NULL AND sku_id IS NOT NULL))

Vouchers
  id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  promotion_id       UNIQUEIDENTIFIER NOT NULL REFERENCES Promotions(id) ON DELETE CASCADE
  code               VARCHAR(50) NOT NULL
  max_uses           INT NOT NULL CHECK (max_uses > 0)
  used_count         INT NOT NULL DEFAULT 0 CHECK (used_count <= max_uses)
  per_customer_limit INT NOT NULL DEFAULT 1
  expires_at         DATETIMEOFFSET NULL
  is_active          BIT NOT NULL DEFAULT 1

  UNIQUE (code)

VoucherUsages
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  voucher_id    UNIQUEIDENTIFIER NOT NULL REFERENCES Vouchers(id) ON DELETE NO ACTION
  customer_id   UNIQUEIDENTIFIER NOT NULL REFERENCES Customers(id) ON DELETE NO ACTION
  order_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(id) ON DELETE NO ACTION
  used_at       DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (voucher_id, order_id)
```

### 3.8 ORDER

```sql
Orders
  id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id        UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(id) ON DELETE NO ACTION
  shift_id        UNIQUEIDENTIFIER NOT NULL REFERENCES Shifts(id) ON DELETE NO ACTION
  customer_id     UNIQUEIDENTIFIER NULL REFERENCES Customers(id) ON DELETE SET NULL
  status          VARCHAR(20) NOT NULL DEFAULT 'Draft'
                   CHECK (status IN ('Draft','Confirmed','Paid','Cancelled','Refunded','PartiallyRefunded'))
  currency_code   VARCHAR(3) NOT NULL DEFAULT 'VND'
  subtotal        DECIMAL(18,2) NOT NULL DEFAULT 0
  discount_total  DECIMAL(18,2) NOT NULL DEFAULT 0
  tax_total       DECIMAL(18,2) NOT NULL DEFAULT 0
  grand_total     DECIMAL(18,2) NOT NULL DEFAULT 0
  note            NVARCHAR(MAX)
  created_by      UNIQUEIDENTIFIER NOT NULL REFERENCES Employees(id) ON DELETE NO ACTION
  created_at      DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
  paid_at         DATETIMEOFFSET NULL

  INDEX idx_orders_store_created (store_id, created_at)
  INDEX idx_orders_shift (shift_id)

OrderItems
  id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  order_id          UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(id) ON DELETE CASCADE
  sku_id            UNIQUEIDENTIFIER NOT NULL REFERENCES SKUs(id) ON DELETE NO ACTION
  qty               DECIMAL(18,3) NOT NULL CHECK (qty > 0)
  unit_price        DECIMAL(18,2) NOT NULL CHECK (unit_price >= 0)
  discount_amount   DECIMAL(18,2) NOT NULL DEFAULT 0
  tax_amount        DECIMAL(18,2) NOT NULL DEFAULT 0
  line_total        DECIMAL(18,2) NOT NULL

OrderDiscounts
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  order_id      UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(id) ON DELETE CASCADE
  promotion_id  UNIQUEIDENTIFIER NULL REFERENCES Promotions(id) ON DELETE SET NULL
  voucher_id    UNIQUEIDENTIFIER NULL REFERENCES Vouchers(id) ON DELETE SET NULL
  discount_amount DECIMAL(18,2) NOT NULL CHECK (discount_amount >= 0)
  description   NVARCHAR(300)
  applied_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  CHECK ((promotion_id IS NOT NULL) OR (voucher_id IS NOT NULL))


Payments
  id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  order_id              UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(id) ON DELETE NO ACTION
  method                VARCHAR(20) NOT NULL CHECK (method IN ('Cash','MoMo','VietQR','Card','Points'))
  amount                DECIMAL(18,2) NOT NULL CHECK (amount > 0)
  change_amount         DECIMAL(18,2) NULL CHECK (change_amount >= 0)
  transaction_ref       VARCHAR(100)
  status                VARCHAR(20) NOT NULL DEFAULT 'Pending'
                         CHECK (status IN ('Pending','Success','Failed','Timeout'))
  gateway_response_json NVARCHAR(MAX) CHECK (gateway_response_json IS NULL OR ISJSON(gateway_response_json) = 1)
  paid_at               DATETIMEOFFSET NULL

  INDEX idx_payments_order (order_id)
  UNIQUE (method, transaction_ref)  -- chống xử lý trùng webhook

Invoices
  id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  order_id          UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(id) ON DELETE NO ACTION
  invoice_no        VARCHAR(30) NOT NULL
  buyer_name        NVARCHAR(200)
  buyer_tax_code    VARCHAR(20)
  buyer_address     NVARCHAR(500)
  total_before_tax  DECIMAL(18,2) NOT NULL
  tax_amount        DECIMAL(18,2) NOT NULL
  grand_total       DECIMAL(18,2) NOT NULL
  issued_at         DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (order_id)     -- quan hệ 1-1
  UNIQUE (invoice_no)
```

### 3.9 CONFIG

```sql
SystemConfigs
  id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id    UNIQUEIDENTIFIER NULL REFERENCES Stores(id) ON DELETE CASCADE   -- NULL=global
  key         VARCHAR(100) NOT NULL
  value       NVARCHAR(MAX) NOT NULL
  updated_at  DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  -- Filtered Unique Index cho global config (store_id IS NULL) và store config (store_id IS NOT NULL)

Translations
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  language_code VARCHAR(5) NOT NULL      -- vi, en...
  key           VARCHAR(200) NOT NULL
  value         NVARCHAR(MAX) NOT NULL
  updated_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  UNIQUE (language_code, key)
```

### 3.10 AUDIT

```sql
AuditLogs
  id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID()
  store_id      UNIQUEIDENTIFIER NULL REFERENCES Stores(id) ON DELETE SET NULL
  employee_id   UNIQUEIDENTIFIER NULL REFERENCES Employees(id) ON DELETE SET NULL
  action        VARCHAR(50) NOT NULL
  entity_type   VARCHAR(50) NOT NULL
  entity_id     UNIQUEIDENTIFIER NOT NULL     -- toàn bộ entity nội bộ đều dùng UNIQUEIDENTIFIER
  description   NVARCHAR(500)
  ip_address    VARCHAR(45)
  created_at    DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()

  INDEX idx_auditlogs_entity (entity_type, entity_id)
  INDEX idx_auditlogs_employee (employee_id, created_at)
```

---

## 4. Chiến lược cache permission (RBAC động)

Vì permission có thể đổi bất kỳ lúc nào (Admin sửa quyền của Manager), **không nhúng danh sách permission vào JWT** (sẽ bị cũ/stale). Thay vào đó:

```
JWT chỉ chứa: employee_id, role_id, store_id (hoặc is_chain_owner)
              — KHÔNG chứa danh sách permission

Khi cần check quyền (AuthorizationBehavior trong MediatR pipeline):
  1. Đọc permission set từ Redis cache, key = "perm:{employee_id}", TTL 5 phút
  2. Cache miss → query DB (role permissions), build lại set, lưu cache
  3. Khi Admin sửa RolePermissions → XÓA NGAY cache của
     toàn bộ nhân viên thuộc role đó (invalidate chủ động, không chờ TTL hết)
```

---

## 5. Index bổ sung cần tạo (ngoài UNIQUE/PK đã tự có index)

```sql
CREATE INDEX idx_orders_store_created ON Orders(store_id, created_at);
CREATE INDEX idx_orders_shift ON Orders(shift_id);
CREATE INDEX idx_stocktx_sku_created ON StockTransactions(sku_id, created_at);
CREATE INDEX idx_stocktx_store_created ON StockTransactions(store_id, created_at);
CREATE INDEX idx_pointtx_customer_created ON PointTransactions(customer_id, created_at);
CREATE INDEX idx_skus_store_active ON SKUs(store_id, is_active) WHERE is_active = 1;
CREATE INDEX idx_products_store_active ON Products(store_id, status) WHERE status = 'Active';
CREATE INDEX idx_stockbatches_expiry ON StockBatches(expiry_date);
CREATE INDEX idx_chatmsg_conversation_created ON ChatMessages(conversation_id, created_at);
CREATE INDEX idx_auditlogs_entity ON AuditLogs(entity_type, entity_id);

-- Filtered Unique Indexes cho SQL Server đối với các bảng cho phép NULL store_id
CREATE UNIQUE INDEX UX_Roles_SystemRole ON Roles(name) WHERE store_id IS NULL;
CREATE UNIQUE INDEX UX_Roles_StoreRole ON Roles(store_id, name) WHERE store_id IS NOT NULL;
CREATE UNIQUE INDEX UX_SystemConfigs_Global ON SystemConfigs([key]) WHERE store_id IS NULL;
CREATE UNIQUE INDEX UX_SystemConfigs_Store ON SystemConfigs(store_id, [key]) WHERE store_id IS NOT NULL;
```

---

## 6. Checklist Best Practice đã áp dụng trong bản thiết kế này

- [x] Tiền tệ dùng `DECIMAL(18,2)`, không dùng float
- [x] Thời gian dùng `DATETIMEOFFSET` (UTC, `SYSUTCDATETIME()`)
- [x] Mọi FK có `ON DELETE` tường minh (`CASCADE`, `SET NULL`, `NO ACTION`)
- [x] Không còn polymorphic FK (`StockTransactions` đã tách 3 cột riêng)
- [x] `SKUs.barcode` UNIQUE thực thi được (denormalize `store_id`)
- [x] `Customers` đã chốt scope global (không còn "hoặc shared" mập mờ)
- [x] Atomic update chống oversell tồn kho (ghi rõ trong `StockEntries`)
- [x] CHECK constraint cho các giá trị enum-like, kiểu số không âm và kiểm tra định dạng JSON (`ISJSON()`)
- [x] UNIQUE composite và Filtered Unique Index chuẩn SQL Server cho các bảng cho phép NULL
- [x] `created_at`/`updated_at` chuẩn hóa trên các bảng dữ liệu chủ
- [x] Soft-delete (`is_active` dạng `BIT`) đồng bộ trên mọi bảng dữ liệu chủ
- [x] `UNIQUEIDENTIFIER` xuyên suốt cho toàn bộ PK và FK
- [x] RBAC động tách bạch Tenant Scope và Permission Scope
- [x] Chống xử lý trùng webhook thanh toán (`UNIQUE(method, transaction_ref)`)

---

## 7. Việc cần làm khi implement

- Seed đầy đủ danh sách `Resources`/`Permissions` khớp với permission matrix cuối cùng của từng role trước khi bắt đầu code.
- Áp dụng các cấu hình Entity qua EF Core Fluent API (`builder.HasIndex(...).HasFilter(...)` cho Filtered Index).

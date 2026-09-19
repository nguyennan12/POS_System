# 📋 BẢNG PHÂN RÃ CÔNG VIỆC & KẾ HOẠCH SPRINT (POS SYSTEM)

> **Tài liệu**: Task Breakdown & Sprint Recommendation chuẩn Agile/Scrum  
> **Dự án**: POS-System (.NET 10 Web API + WinUI / WinForms + PostgreSQL 17 + Redis)  
> **Mục đích**: Sử dụng trực tiếp để quản lý và theo dõi tiến độ trên Trello / Jira  
> **Quy chuẩn mã Task**: Đánh số tuần tự từ **T01** đến **T63** (T01 – T10: Đã hoàn thành; T11 – T63: Cần triển khai)

---

# PART 1 — SYSTEM ANALYSIS

### 1.1 Project Overview

Hệ thống **POS-System** là giải pháp phần mềm quản trị bán lẻ và chuỗi cửa hàng đa chi nhánh kết hợp:

- **Cloud Backend Server**: ASP.NET Core Web API (.NET 10) chạy container hóa Docker trên nền PostgreSQL 17 + Redis 7 + Grafana Stack (Loki, Prometheus, Grafana).
- **Client Desktop App**: WinUI / WinForms (.NET 10) cài đặt trực tiếp tại các quầy thu ngân (POS terminal), kết nối với Cloud API qua REST (HTTPS/JSON) và SignalR (Realtime payment).

### 1.2 Architecture Pattern

- **Backend Architecture**: **Clean Architecture + DDD + CQRS (MediatR)**:
  - `POS.Contracts`: Định nghĩa DTO Request / Response công khai (dạng `record` bất biến).
  - `POS.Domain`: Chứa Rich Domain Entities, Value Objects, Domain Services thuần túy, Result/Error pattern.
  - `POS.Application`: Use Cases (Commands, Queries, Handlers, FluentValidation, Pipeline Behaviors, Repository Abstractions).
  - `POS.Infrastructure`: EF Core 10 / PostgreSQL 17, Entity Configurations, Repository Implementations, External Adapters.
  - `POS.Api`: Controllers tiếp nhận HTTP request, dispatch qua `ISender`, chuẩn hóa phản hồi `ApiResponse<T>`.
- **Client Desktop Architecture**: **Model-View-Presenter (MVP)** kết hợp Dependency Injection:
  - `Views`: WinForms/WinUI triển khai các `IXxxView` interfaces (thụ động, không logic nghiệp vụ).
  - `Presenters`: Điều phối luồng giữa View và ApiClient.
  - `ApiClients`: Gọi REST API qua `HttpClient` + `Polly` (resilience/retry).

### 1.3 Technology Stack Hiện Hữu

- **Runtime**: .NET 10 (C# 13)
- **Web Framework**: ASP.NET Core Web API, MediatR, FluentValidation, Scalar API Documentation
- **Database & Cache**: PostgreSQL 17, Entity Framework Core 10, Redis 7 (`StackExchange.Redis`)
- **Observability**: Serilog, Grafana Loki, Prometheus Metrics, Grafana Dashboard
- **Desktop UI**: WinUI / WinForms (.NET 10), MaterialSkin 2
- **DevOps**: Docker, Docker Compose (Môi trường Dev có Hot Reload + Môi trường Production tối ưu build)

### 1.4 Baseline Hiện Tại (T01 – T10 COMPLETED)

1. Cấu trúc Solution đa project chuẩn Clean Architecture (`POS.sln`).
2. Hạ tầng Docker Compose cho Dev & Production (PostgreSQL, Redis, API, Prometheus, Loki, Grafana).
3. Cơ chế tự động chạy EF Core Migration khi khởi động API (`MigrationExtensions`).
4. Toàn bộ `POS.Contracts/V1` với đầy đủ DTOs cho 16 domains.
5. Toàn bộ `POS.Domain` Entities, Enums, Value Objects và EF Configurations.
6. Pipeline Behaviors: `LoggingBehavior`, `ValidationBehavior`, `GlobalExceptionHandler`.
7. Feature mẫu End-to-End hoàn chỉnh: `Stores` (Tạo và xem chi tiết Cửa hàng từ Backend đến WinUI Client).

---

# PART 2 — FEATURE / MODULE BREAKDOWN

```text
POS-System
│
├── EPIC 00: Project Foundation, Baseline Infrastructure & Sample Slice (COMPLETED)
│   ├── Feature 0.1: Solution & Project Setup
│   │   └── [ARCH] Setup Clean Architecture Multi-project Solution (.NET 10) (T01)
│   ├── Feature 0.2: Docker Containerization & Local Environments
│   │   └── [DEVOPS] Setup Docker Compose Dev & Production Stacks (T02)
│   ├── Feature 0.3: Database Context & Migration Automation
│   │   ├── [DB] Setup AppDbContext, Fluent Entity Configurations & Migrations (T03)
│   │   └── [BE] Implement Automatic EF Core Migration Runner on App Startup (T04)
│   ├── Feature 0.4: Observability, Logging & Error Handling
│   │   ├── [INFRA] Setup Serilog with Grafana Loki Sink & Prometheus Metrics (T05)
│   │   └── [API] Implement Global Exception Handler & Scalar API Docs (T06)
│   ├── Feature 0.5: CQRS Pipeline & Public Contracts
│   │   ├── [BE] Implement MediatR Behaviors & Result Pattern (T07)
│   │   └── [CONTRACTS] Implement Complete V1 API DTOs Specification (T10)
│   └── Feature 0.6: Reference End-to-End Vertical Slice (Store Feature)
│       ├── [BE/API] Implement Store Baseline (CreateStore, GetStoreDetail, Controller) (T08)
│       └── [FE] Setup WinUI Base Client, SessionService & Store Management Screen (T09)
│
├── EPIC 01: Core Architecture, Authentication & RBAC
│   ├── Feature 1.1: Authentication & Token Management
│   │   ├── [DB] Seed System Roles and Initial Permissions (T11)
│   │   ├── [BE] Implement Password and PIN Authentication Use Cases (T12)
│   │   ├── [BE] Implement Token Refresh and Revocation Mechanism (T13)
│   │   ├── [API] Implement Auth Endpoints (T14)
│   │   └── [FE] Implement Login Form (Dual Mode: Password & PIN) (T15)
│   ├── Feature 1.2: Dynamic RBAC & Store Authorization
│   │   ├── [BE] Implement Permission Authorization Behavior with Redis Cache (T16)
│   │   ├── [BE] Implement Role & Permission Management Use Cases (T17)
│   │   ├── [API] Implement RBAC Management Endpoints (T18)
│   │   └── [FE] Implement Role & Permission Configuration Form
│   └── Feature 1.3: Employee & Store Access Management
│       ├── [BE] Complete Store Management (Update, Status, Assign Admin) (T19)
│       ├── [BE] Implement Employee Management (CRUD, Lock, PIN Reset) (T20)
│       ├── [ARCH/BE] Extract Store Access Control into Policy Pattern (T63)
│       └── [FE] Implement Employee Management Screen (T21)
│
├── EPIC 02: Product Catalog & Pricing Management
│   ├── Feature 2.1: Category Tree Management
│   │   ├── [DB] Seed Default Store Category Tree (T62)
│   │   ├── [BE] Implement Category Hierarchy Queries and Mutations (T22)
│   │   ├── [API] Implement Categories Endpoints (T23)
│   │   └── [FE] Implement Category Tree View Screen
│   ├── Feature 2.2: Product & SKU Variant Management
│   │   ├── [BE] Implement Product & Multi-SKU Creation with Barcode Uniqueness (T24)
│   │   ├── [BE] Implement Unit Conversion and Price List Rules (T25)
│   │   ├── [API] Implement Products, SKUs and Barcode Query Endpoints (T26)
│   │   └── [FE] Implement Product & SKU Management Screen (T27)
│   └── Feature 2.3: Bulk Product Data Import
│       └── [BE/API] Implement Bulk Product Excel Import via ClosedXML (T28)
│
├── EPIC 03: Inventory, Supplier & Shift Operations
│   ├── Feature 3.1: Supplier & Debt Tracking
│   │   └── [BE/API] Implement Supplier Management & Debt Tracking Use Cases (T29)
│   ├── Feature 3.2: Stock-In & Inventory Ledger
│   │   ├── [BE/API] Implement Stock-In Voucher Creation & Stock Transaction Ledger (T30)
│   │   ├── [BE/API] Implement Stock Entries Realtime Queries, Min-Stock & Alerts (T31)
│   │   └── [FE] Implement Inventory & Stock-In WinUI Screens (T33)
│   ├── Feature 3.3: Stock Taking (Kiểm kê kho)
│   │   └── [BE/API] Implement Stock Take Process & Variance Approval (T32)
│   └── Feature 3.4: Shift Work & Cash Drawer Reconciliation
│       ├── [BE/API] Implement Shift Work Use Cases & Endpoints (T34)
│       └── [FE] Implement Open/Close Shift WinUI Dialogs (T35)
│
├── EPIC 04: Promotion Engine & Customer Loyalty
│   ├── Feature 4.1: Customer & Member Tier Management
│   │   └── [BE/API] Implement Customer CRUD & Member Tier Upgrade Rules (T36)
│   ├── Feature 4.2: Loyalty Point Transactions
│   │   ├── [BE/API] Implement Loyalty Point Accrual & Redemption Use Cases (T37)
│   │   └── [FE] Implement Customer Management & Loyalty WinUI Form (T38)
│   ├── Feature 4.3: Promotion Engine (Core Calculation)
│   │   ├── [DOMAIN] Implement PromotionEngine with Stacking & Exclusive Rules (T39)
│   │   └── [TEST] Comprehensive Unit Tests for Promotion Engine (T40)
│   └── Feature 4.4: Voucher Code Management
│       └── [BE/API] Implement Promotion & Voucher CRUD Use Cases & Validation Query (T41)
│
├── EPIC 05: Order Lifecycle, Split Payment & Billing
│   ├── Feature 5.1: Order Management & Cart Realtime Evaluation
│   │   ├── [BE] Implement Order Core Handlers (Create, Add Item, Recalculate Promo) (T42)
│   │   ├── [BE] Implement Order Checkout & Status Progression Use Cases (T43)
│   │   ├── [API] Implement Orders Controller Endpoints (T44)
│   │   └── [FE] Implement Main POS Cashier Screen (frmSalesMain) (T50)
│   ├── Feature 5.2: Multi-Method & Split Payment Processing
│   │   ├── [BE] Implement Split Payment Coordinator & Status Aggregation Handler (T45)
│   │   ├── [INFRA] Implement MoMo and VietQR Payment Adapters with Webhook (T46)
│   │   ├── [INFRA/API] Implement SignalR PaymentHub for Realtime QR Payment Results (T47)
│   │   └── [FE] Implement Payment Selection & Dynamic QR Dialog (frmPaymentDialog) (T51)
│   └── Feature 5.3: Invoicing & Receipt Printing
│       ├── [BE/API] Implement Sequential Invoice Generation Use Case (T48)
│       ├── [INFRA] Implement Thermal Receipt Printing (ESCPOS) & PDF Export (T49)
│       ├── [FE] Implement Invoice Viewer & Reprint Screen (frmInvoiceView) (T52)
│       └── [BE] Implement Automatic Inventory Deduction & Loyalty Point Trigger on Paid (T53)
│
└── EPIC 06: CRM, AI Chatbot, Reports & Operations
    ├── Feature 6.1: Customer Feedback & AI Chatbot
    │   └── [BE/API] Implement AI Chatbot Session & Message Provider (OpenAI/Gemini) (T54)
    ├── Feature 6.2: Analytics, Dashboard & Reporting
    │   ├── [BE/API] Implement Real-time Executive Dashboard Metrics Query (T55)
    │   ├── [BE/API] Implement Multi-dimensional Revenue & Inventory Reports (T56)
    │   └── [FE] Implement WinUI Executive Dashboard & Report Visualizer (T57)
    ├── Feature 6.3: System Configuration & Multi-language (i18n)
    │   └── [BE/FE] Implement System Configuration & Multi-language (i18n) Engine (T58)
    └── Feature 6.4: System Hardening & DevOps
        ├── [TEST] End-to-End Integration Tests for Complete Sales Cycle (T59)
        └── [DEVOPS] Configure Production Docker Stack (Nginx SSL + Loki/Prometheus) (T60)
```

---

# PART 3 — TASK LIST

| ID      | Task Name                                                                   | Type     | Layer     | Status        | Priority      | Dependency         |
| :------ | :-------------------------------------------------------------------------- | :------- | :-------- | :------------ | :------------ | :----------------- |
| **T01** | Setup Clean Architecture Multi-project Solution (.NET 10)                   | REFACTOR | ARCH      | **COMPLETED** | P0 - Critical | -                  |
| **T02** | Setup Docker Compose Dev (Hot-Reload) & Production Stacks                   | DEVOPS   | DEVOPS    | **COMPLETED** | P0 - Critical | T01                |
| **T03** | Setup AppDbContext, Fluent Entity Configurations & Schema Migrations        | FEATURE  | DB        | **COMPLETED** | P0 - Critical | T01                |
| **T04** | Implement Automatic EF Core Migration Runner on App Startup                 | FEATURE  | BE        | **COMPLETED** | P0 - Critical | T03                |
| **T05** | Setup Serilog Loki Sink, Prometheus Metrics & Grafana Config                | DEVOPS   | INFRA     | **COMPLETED** | P1 - High     | T02                |
| **T06** | Implement Global Exception Handler & Scalar API Documentation               | FEATURE  | BE/API    | **COMPLETED** | P0 - Critical | T01                |
| **T07** | Implement MediatR Pipeline Behaviors (Logging, Validation) & Result Pattern | FEATURE  | BE        | **COMPLETED** | P0 - Critical | T01                |
| **T08** | Implement Reference Vertical Slice: Store Management Core                   | FEATURE  | BE/API    | **COMPLETED** | P1 - High     | T07                |
| **T09** | Setup WinUI Client Foundation, DI, SessionService & Store Screen            | FEATURE  | FE        | **COMPLETED** | P1 - High     | T08                |
| **T10** | Define Complete V1 Public API Contracts (Requests & Responses)              | FEATURE  | CONTRACTS | **COMPLETED** | P0 - Critical | T01                |
| **T11** | Seed System Roles, Resources & Default Permissions                          | FEATURE  | DB        | MISSING       | P0 - Critical | T03                |
| **T12** | Implement Password & PIN Login Use Cases (BCrypt & HMAC Lookup)             | FEATURE  | BE        | MISSING       | P0 - Critical | T11                |
| **T13** | Implement Refresh Token Rotation & Session Revocation                       | FEATURE  | BE        | MISSING       | P0 - Critical | T12                |
| **T14** | Implement Authentication Controller Endpoints                               | FEATURE  | BE/API    | MISSING       | P0 - Critical | T12, T13           |
| **T15** | Implement WinUI Login Screen with Tabbed Password & PIN Pad                 | FEATURE  | FE        | MISSING       | P0 - Critical | T14, T09           |
| **T16** | Implement Dynamic RBAC Authorization Pipeline Behavior with Redis           | FEATURE  | BE        | MISSING       | P1 - High     | T11, T12           |
| **T17** | Implement Role & Permission Management Commands/Queries                     | FEATURE  | BE        | MISSING       | P1 - High     | T16                |
| **T18** | Implement RBAC Controller Endpoints                                         | FEATURE  | BE/API    | MISSING       | P1 - High     | T17                |
| **T19** | Complete Store Management (Update, Status, Assign Admin)                    | FEATURE  | BE/API    | PARTIAL       | P1 - High     | T16, T08           |
| **T20** | Implement Employee CRUD, Lock & Reset PIN Use Cases                         | FEATURE  | BE/API    | MISSING       | P1 - High     | T16, T19           |
| **T21** | Implement Employee Management WinUI Screen                                  | FEATURE  | FE        | MISSING       | P2 - Medium   | T20                |
| **T22** | Implement Category Tree CRUD & Query Handlers                               | FEATURE  | BE        | MISSING       | P1 - High     | T07                |
| **T23** | Implement Categories Controller Endpoints                                   | FEATURE  | BE/API    | MISSING       | P1 - High     | T22                |
| **T24** | Implement Product & SKU Management Use Cases (Barcode Uniqueness)           | FEATURE  | BE        | MISSING       | P1 - High     | T22                |
| **T25** | Implement Unit Conversions & Price Lists Handlers                           | FEATURE  | BE        | MISSING       | P2 - Medium   | T24                |
| **T26** | Implement Products, SKUs & Barcode Query Controller Endpoints               | FEATURE  | BE/API    | MISSING       | P1 - High     | T24, T25           |
| **T27** | Implement Product & Category Management WinUI Forms                         | FEATURE  | FE        | MISSING       | P1 - High     | T23, T26           |
| **T28** | Implement Bulk Product Excel Import via ClosedXML                           | FEATURE  | BE/API    | MISSING       | P2 - Medium   | T24                |
| **T29** | Implement Supplier Management & Debt Tracking Use Cases                     | FEATURE  | BE/API    | MISSING       | P2 - Medium   | T07                |
| **T30** | Implement Stock-In Voucher Creation & Stock Transaction Ledger              | FEATURE  | BE/API    | MISSING       | P1 - High     | T24, T29           |
| **T31** | Implement Stock Entries Realtime Queries, Min-Stock & Expiry Alerts         | FEATURE  | BE/API    | MISSING       | P1 - High     | T24, T30           |
| **T32** | Implement Stock Take (Kiểm kê) & Variance Approval Use Cases                | FEATURE  | BE/API    | MISSING       | P2 - Medium   | T31                |
| **T33** | Implement Inventory & Stock-In WinUI Screens                                | FEATURE  | FE        | MISSING       | P1 - High     | T30, T31, T32      |
| **T34** | Implement Shift Work (Open Shift, Close Shift & Cash Reconciliation)        | FEATURE  | BE/API    | MISSING       | P1 - High     | T12                |
| **T35** | Implement Open/Close Shift WinUI Dialogs                                    | FEATURE  | FE        | MISSING       | P1 - High     | T34                |
| **T36** | Implement Customer CRUD & Member Tier Upgrade Rules                         | FEATURE  | BE/API    | MISSING       | P1 - High     | T07                |
| **T37** | Implement Loyalty Point Accrual & Redemption Use Cases                      | FEATURE  | BE/API    | MISSING       | P1 - High     | T36                |
| **T38** | Implement Customer Management & Loyalty WinUI Form                          | FEATURE  | FE        | MISSING       | P2 - Medium   | T36, T37           |
| **T39** | Implement Promotion Engine Domain Service (Stacking & Priority Rules)       | FEATURE  | DOMAIN    | MISSING       | P0 - Critical | T24                |
| **T40** | Add Comprehensive Unit Tests for Promotion Engine                           | TEST     | TEST      | MISSING       | P0 - Critical | T39                |
| **T41** | Implement Promotion & Voucher CRUD Use Cases & Validation Query             | FEATURE  | BE/API    | MISSING       | P1 - High     | T39                |
| **T42** | Implement Order Core Handlers (Create, Add Item, Recalculate Promo)         | FEATURE  | BE        | MISSING       | P0 - Critical | T34, T39, T41      |
| **T43** | Implement Order Checkout & Status Progression Use Cases                     | FEATURE  | BE        | MISSING       | P0 - Critical | T42                |
| **T44** | Implement Orders Controller Endpoints                                       | FEATURE  | BE/API    | MISSING       | P0 - Critical | T42, T43           |
| **T45** | Implement Split Payment Coordinator & Status Aggregation Handler            | FEATURE  | BE        | MISSING       | P0 - Critical | T43                |
| **T46** | Implement MoMo & VietQR Payment Adapters with Webhook Verification          | FEATURE  | INFRA     | MISSING       | P1 - High     | T45                |
| **T47** | Implement SignalR PaymentHub for Realtime QR Payment Results                | FEATURE  | INFRA/API | MISSING       | P1 - High     | T45, T46           |
| **T48** | Implement Sequential Invoice Generation Use Case                            | FEATURE  | BE/API    | MISSING       | P1 - High     | T43, T45           |
| **T49** | Implement Thermal Receipt Printing (ESCPOS) & PDF Export (QuestPDF)         | FEATURE  | INFRA     | MISSING       | P1 - High     | T48                |
| **T50** | Implement POS Cashier Main Screen (`frmSalesMain`)                          | FEATURE  | FE        | MISSING       | P0 - Critical | T26, T41, T44      |
| **T51** | Implement Payment Selection & Dynamic QR Dialog (`frmPaymentDialog`)        | FEATURE  | FE        | MISSING       | P0 - Critical | T45, T47, T50      |
| **T52** | Implement Invoice Viewer & Reprint Screen (`frmInvoiceView`)                | FEATURE  | FE        | MISSING       | P2 - Medium   | T48, T49           |
| **T53** | Implement Automatic Inventory Deduction & Loyalty Point Trigger on Paid     | FEATURE  | BE        | MISSING       | P0 - Critical | T31, T37, T45      |
| **T54** | Implement AI Chatbot Session & Message Provider (OpenAI/Gemini)             | FEATURE  | BE/API    | MISSING       | P2 - Medium   | T24                |
| **T55** | Implement Real-time Executive Dashboard Metrics Query                       | FEATURE  | BE/API    | MISSING       | P1 - High     | T43, T31           |
| **T56** | Implement Multi-dimensional Revenue & Inventory Reports                     | FEATURE  | BE/API    | MISSING       | P1 - High     | T43, T31           |
| **T57** | Implement WinUI Executive Dashboard & Report Visualizer                     | FEATURE  | FE        | MISSING       | P1 - High     | T55, T56           |
| **T58** | Implement System Configuration & Multi-language (i18n) Engine               | FEATURE  | BE/FE     | MISSING       | P2 - Medium   | T07                |
| **T59** | End-to-End Integration Tests for Complete Sales Cycle                       | TEST     | TEST      | MISSING       | P0 - Critical | T35, T50, T51, T53 |
| **T60** | Configure Production Docker Stack (Nginx SSL + Loki/Prometheus)             | DEVOPS   | DEVOPS    | MISSING       | P1 - High     | T14, T02           |
| **T61** | Apply `IRequirePermission` to API Commands                                    | FEATURE  | BE        | MISSING       | P1 - High     | T16, T17           |
| **T62** | Seed Default Store Category Tree                                        | FEATURE  | DB        | MISSING       | P1 - High     | T03, T08           |
| **T63** | Extract Store Access Control into Policy Pattern                         | REFACTOR | BE        | MISSING       | P1 - High     | T16, T17, T19      |

---

# PART 4 — DETAILED TASK SPECIFICATION

---

## 🟢 NHÓM CÁC TASK ĐÃ CÓ SẴN (COMPLETED)

### T01 — [ARCH][FEATURE] Setup Clean Architecture Multi-project Solution (.NET 10)

- **Status:** **COMPLETED** | **Priority:** P0 - Critical | **Suggested Role:** ARCH / BE
- **Description:** Khởi tạo Solution `POS.sln` trên .NET 10 SDK và phân chia project theo đúng mô hình Clean Architecture: `POS.Domain`, `POS.Application`, `POS.Infrastructure`, `POS.Api`, `POS.Contracts`, `POS.WinUI`. Thiết lập project references theo đúng chiều phụ thuộc: API -> Application -> Domain; Infrastructure -> Application + Domain; Contracts độc lập.
- **Source / Evidence:** File `POS.sln` và các project files `.csproj` hiện hữu trong thư mục `src/`.
- **Dependencies:** None
- **API Contracts:** `POS.Contracts.dll` (Contracts Assembly Foundation)
- **Acceptance Criteria:** Toàn bộ solution build thành công không có circular dependencies trên .NET 10.
- **Estimated Size:** M

---

### T02 — [DEVOPS] Setup Docker Compose Dev (Hot-Reload) & Production Stacks

- **Status:** **COMPLETED** | **Priority:** P0 - Critical | **Suggested Role:** DEVOPS
- **Description:** Xây dựng cấu hình container hóa gồm 2 môi trường:
  1. `docker/dev/docker-compose.yml`: Chạy PostgreSQL 17 (`localhost:5432`), Redis 7 (`localhost:6379`), API hỗ trợ volume mount hot-reload, Prometheus (`localhost:9090`), Grafana (`localhost:3000`).
  2. `docker/production/docker-compose.yml`: Multi-stage build image tối ưu cho production.
- **Source / Evidence:** Thư mục `docker/dev/` và `docker/production/` kèm các file `.env.example`, `Dockerfile`.
- **Dependencies:** T01
- **API Contracts:** N/A (Docker Infrastructure Stack)
- **Acceptance Criteria:** Chạy `docker compose up -d` khởi động đầy đủ các dịch vụ mà không phát sinh lỗi cổng hoặc phân quyền.
- **Estimated Size:** M

---

### T03 — [DB][FEATURE] Setup AppDbContext, Fluent Entity Configurations & Schema Migrations

- **Status:** **COMPLETED** | **Priority:** P0 - Critical | **Suggested Role:** DB / BE
- **Description:** Thiết lập `AppDbContext` trong `POS.Infrastructure` với đầy đủ `DbSet<T>` cho 28 bảng dữ liệu. Viết tách biệt các EntityTypeConfiguration theo chuẩn Fluent API cho toàn bộ các domain (Core, RBAC, Product, Inventory, Customer, Promotion, Order, Config, Audit). Chạy migration ban đầu và điều chỉnh cascade paths.
- **Source / Evidence:** `src/POS.Infrastructure/Persistence/AppDbContext.cs`, `AppDbContext.DbSets.cs`, thư mục `Configurations/` và các file trong `Migrations/`.
- **Dependencies:** T01
- **API Contracts:** N/A (EF Core Persistence & Schema Configurations)
- **Acceptance Criteria:** EF Core Snapshot phản ánh chính xác schema PostgreSQL 17 theo tài liệu `pos_database_design.md`.
- **Estimated Size:** L

---

### T04 — [BE][FEATURE] Implement Automatic EF Core Migration Runner on App Startup

- **Status:** **COMPLETED** | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Hiện thực `IMigrationService` và extension method `ApplyMigrationsAsync()` trong `POS.Api/Extensions/MigrationExtensions.cs`. Khi ứng dụng API khởi động, tự động kiểm tra và áp dụng các file migration chưa apply vào database mà không cần chạy lệnh `dotnet ef database update` thủ công.
- **Source / Evidence:** `src/POS.Api/Extensions/MigrationExtensions.cs` & `src/POS.Infrastructure/Persistence/MigrationService.cs`.
- **Dependencies:** T03
- **API Contracts:** N/A (Startup Migration Automation)
- **Acceptance Criteria:** Khởi chạy container API lần đầu tự động tạo đầy đủ bảng và quan hệ trên PostgreSQL.
- **Estimated Size:** S

---

### T05 — [DEVOPS][INFRA] Setup Serilog Loki Sink, Prometheus Metrics & Grafana Config

- **Status:** **COMPLETED** | **Priority:** P1 - High | **Suggested Role:** DEVOPS / BE
- **Description:** Cấu hình Serilog đẩy log trực tiếp về Grafana Loki qua HTTP (`SerilogLokiConfiguration.cs`), tích hợp middleware thu thập metrics Prometheus tại endpoint `/metrics`, và chuẩn bị file cấu hình `prometheus.yml`.
- **Source / Evidence:** `src/POS.Infrastructure/Logging/SerilogLokiConfiguration.cs`, `docker/prometheus/prometheus.yml`, `src/POS.Api/Program.cs`.
- **Dependencies:** T02
- **API Contracts:** N/A (Serilog Loki Sink & Prometheus Metrics Endpoint `/metrics`)
- **Acceptance Criteria:** Log ghi từ ASP.NET Core xuất hiện trên Loki; Prometheus cào dữ liệu metrics thành công.
- **Estimated Size:** S

---

### T06 — [BE/API][FEATURE] Implement Global Exception Handler & Scalar API Documentation

- **Status:** **COMPLETED** | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Thiết lập `GlobalExceptionHandler` bắt toàn bộ lỗi ngoại lệ chưa xử lý và trả về JSON theo chuẩn RFC 7807 ProblemDetails / ApiResponse format. Tích hợp thư viện `Scalar.AspNetCore` tại đường dẫn `/scalar/v1` thay thế Swagger UI truyền thống.
- **Source / Evidence:** `src/POS.Api/Exceptions/GlobalExceptionHandler.cs`, `src/POS.Api/Extensions/OpenApiExtensions.cs`, `src/POS.Api/Program.cs`.
- **Dependencies:** T01
- **API Contracts:** `ApiError`, `ApiResponse<T>` (từ `POS.Contracts.V1.Common`)
- **Acceptance Criteria:** Mọi unhandled exception trả về HTTP 500 JSON có cấu trúc; giao diện Scalar hiển thị đầy đủ tài liệu API.
- **Estimated Size:** S

---

### T07 — [BE][FEATURE] Implement MediatR Pipeline Behaviors & Result Pattern

- **Status:** **COMPLETED** | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Hiện thực kiến trúc xử lý Command/Query qua MediatR:
  1. `ValidationBehavior<TRequest, TResponse>`: Bắt lỗi FluentValidation và trả về `ValidationResult` bọc trong `Result<T>`.
  2. `LoggingBehavior<TRequest, TResponse>`: Tự động ghi log thời gian xử lý của từng Command/Query.
  3. Cấu trúc `Result`, `Result<T>`, `Error`, `ICommand`, `IQuery`, `IUnitOfWork`.
- **Source / Evidence:** `src/POS.Application/Common/Behaviors/`, `src/POS.Domain/Common/Result.cs`, `src/POS.Application/Abstractions/Messaging/`.
- **Dependencies:** T01
- **API Contracts:** `ApiResponse<T>`, `PagedRequest`, `PagedResponse<T>` (từ `POS.Contracts.V1.Common`)
- **Acceptance Criteria:** Handler không cần throw exception cho lỗi nghiệp vụ; request sai validation tự động dừng trước khi vào Handler.
- **Estimated Size:** M

---

### T08 — [BE/API][FEATURE] Implement Reference Vertical Slice: Store Management Core

- **Status:** **COMPLETED** | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Xây dựng tính năng chuẩn làm mẫu (Reference Implementation) cho toàn bộ team:
  - Domain: `Store.cs` với encapsulated properties và business methods (`UpdateInfo`, `Deactivate`).
  - Application: `CreateStoreCommand`, `CreateStoreCommandHandler`, `CreateStoreCommandValidator`, `GetStoreDetailQuery`, `GetStoreDetailQueryHandler`, `IStoreRepository`.
  - Infrastructure: `StoreRepository.cs`, `StoreConfiguration.cs`.
  - API: `StoresController.cs` (`POST`, `GET /{id}`), `StoreMapping.cs`.
- **Source / Evidence:** `src/POS.Application/UseCases/Stores/`, `src/POS.Api/Controllers/StoresController.cs`, `src/POS.Infrastructure/Persistence/Repositories/StoreRepository.cs`.
- **Dependencies:** T07
- **API Contracts:** `CreateStoreRequest` (Request); `StoreResponse`, `StoreDetailResponse` (Response) (từ `POS.Contracts.V1.Stores`)
- **Acceptance Criteria:** Gọi API `POST /api/v1/stores` tạo cửa hàng thành công (HTTP 201) và `GET /api/v1/stores/{id}` lấy chi tiết chính xác.
- **Estimated Size:** M

---

### T09 — [FE][FEATURE] Setup WinUI Client Foundation, DI, SessionService & Store Screen

- **Status:** **COMPLETED** | **Priority:** P1 - High | **Suggested Role:** FE
- **Description:** Khởi tạo ứng dụng WinUI/WinForms (.NET 10) với Microsoft Extensions DI, cấu hình `appsettings.json`, xây dựng `BaseApiClient` với Polly retry, `SessionService` quản lý trạng thái phiên, và triển khai màn hình tham chiếu `frmStoreList.cs`, `frmStoreEdit.cs` cùng `StorePresenter` và `StoreApiClient`.
- **Source / Evidence:** `src/POS.WinUI/Program.cs`, `src/POS.WinUI/ApiClients/StoreApiClient.cs`, `src/POS.WinUI/Forms/Stores/`.
- **Dependencies:** T08
- **API Contracts:** `StoreApiClient` tương tác qua `CreateStoreRequest`, `StoreResponse`, `StoreDetailResponse`, `ApiResponse<T>`
- **Acceptance Criteria:** App WinUI khởi chạy, nạp danh sách cửa hàng và mở dialog tạo cửa hàng thành công qua REST API.
- **Estimated Size:** M

---

### T10 — [CONTRACTS][FEATURE] Define Complete V1 Public API Contracts

- **Status:** **COMPLETED** | **Priority:** P0 - Critical | **Suggested Role:** ARCH / BE
- **Description:** Định nghĩa đầy đủ toàn bộ các DTO Request / Response công khai (dạng `record`) cho tất cả 16 modules: Auth, Categories, Chatbot, Common, Config, Customers, Employees, Inventory, Invoices, Orders, Payments, Products, Promotions, Rbac, Reports, Shifts, Stores.
- **Source / Evidence:** Thư mục `src/POS.Contracts/V1/` với hơn 40 files C#.
- **Dependencies:** T01
- **API Contracts:** Toàn bộ 16 Domain DTO Namespaces trong `POS.Contracts.V1.*`
- **Acceptance Criteria:** Các DTO bất biến, độc lập hoàn toàn với EF Core và Domain entity, sẵn sàng làm hợp đồng giao tiếp giữa API và WinUI.
- **Estimated Size:** L

---

## 🟡 NHÓM CÁC TASK TIẾP THEO CẦN TRIỂN KHAI (MISSING / PARTIAL)

### T11 — [DB][FEATURE] Seed System Roles, Resources & Default Permissions

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** DB / BE
- **Description:** Khởi tạo dữ liệu ban đầu cho các bảng `Roles`, `Resources`, `Permissions`, và `RolePermissions` trong PostgreSQL.
- **Why:** Phân quyền, tạo nhân viên và đăng nhập phụ thuộc vào 4 role hệ thống (`Owner`, `Admin`, `Manager`, `Cashier`) và các mã permission chuẩn.
- **Source / Evidence:** `doc/pos_database_design.md` mục 3.2 và `pos_system_architecture.md` mục 8.4.
- **Dependencies:** T03
- **API Contracts:** `PermissionResponse`, `ResourceResponse`, `RoleResponse` (từ `POS.Contracts.V1.Rbac`)
- **Acceptance Criteria:**
  - 4 Role hệ thống được tạo với `is_system_role = 1` và `store_id = NULL`.
  - Toàn bộ Resource chuẩn được seed.
  - Các Permission `code` dạng `resource:action` được gán chính xác cho 4 Role.
- **Estimated Size:** S

---

### T12 — [BE][FEATURE] Implement Password & PIN Login Use Cases

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Hiện thực `LoginWithPasswordCommand` (Username/Password khớp BCrypt) và `LoginWithPinCommand` (PIN 6 số tính HMAC-SHA256 lookup theo Store và kiểm tra BCrypt). Khóa tài khoản 15 phút nếu sai quá 5 lần.
- **Why:** Cửa ngõ xác thực duy nhất cho nhân viên tại quầy và văn phòng.
- **Source / Evidence:** `doc/pos_system_architecture.md` mục 8.1, 8.2 & `doc/pos_database_design.md` mục 3.1.
- **Dependencies:** T11
- **API Contracts:**
  - Request: `LoginRequest(string Username, string Password)`, `PinLoginRequest(Guid StoreId, string Pin)`
  - Response: `AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, CurrentUserResponse User)` (từ `POS.Contracts.V1.Auth`)
- **Acceptance Criteria:** Trả về Access Token (8h); tăng `failed_login_count` khi sai; khóa 15 phút khi đạt 5 lần sai; reset count khi đúng.
- **Estimated Size:** M

---

### T13 — [BE][FEATURE] Implement Refresh Token Rotation & Session Revocation

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Cơ chế cấp phát và làm mới Access Token bằng Refresh Token (thời hạn 30 ngày) lưu trong bảng `RefreshTokens`. Hỗ trợ revoke token khi logout hoặc đóng ca.
- **Dependencies:** T12
- **API Contracts:**
  - Request: `RefreshTokenRequest(string RefreshToken)`, `LogoutRequest(string RefreshToken)`
  - Response: `AuthResponse` (từ `POS.Contracts.V1.Auth`)
- **Acceptance Criteria:** Thu hồi token cũ và sinh token mới khi Refresh; `LogoutCommand` gán `revoked_at`.
- **Estimated Size:** S

---

### T14 — [BE/API][FEATURE] Implement Authentication Controller Endpoints

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Xây dựng `AuthController` với các endpoints: `POST /auth/login`, `POST /auth/pin`, `POST /auth/refresh`, `POST /auth/logout`, `GET /auth/me`.
- **Dependencies:** T12, T13
- **API Contracts:**
  - Request: `LoginRequest`, `PinLoginRequest`, `RefreshTokenRequest`, `LogoutRequest`
  - Response: `AuthResponse`, `CurrentUserResponse` (từ `POS.Contracts.V1.Auth`), bọc trong `ApiResponse<T>`
- **Acceptance Criteria:** Map đúng DTOs `POS.Contracts.V1.Auth`, trả về `ApiResponse<AuthResponse>` và `ApiResponse<CurrentUserResponse>`.
- **Estimated Size:** S

---

### T15 — [FE][FEATURE] Implement WinUI Login Screen with Tabbed Password & PIN Pad

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** FE
- **Description:** Xây dựng màn hình đăng nhập WinUI (`frmLogin.cs`) hỗ trợ Tab 1: Username & Password; Tab 2: Bàn phím số cảm ứng PIN Pad (0-9). Lưu JWT vào `SessionService`.
- **Dependencies:** T14, T09
- **API Contracts:** `AuthApiClient` gửi `LoginRequest` / `PinLoginRequest`, nhận `ApiResponse<AuthResponse>` và lưu `CurrentUserResponse` vào Session
- **Acceptance Criteria:** Giao diện MaterialSkin hiển thị trực quan; mở đúng màn hình Bán hàng hoặc Dashboard theo Role.
- **Estimated Size:** M

---

### T16 — [BE][FEATURE] Implement Dynamic RBAC Authorization Pipeline Behavior with Redis

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Triển khai `AuthorizationBehavior` trong MediatR pipeline. Kiểm tra Permission yêu cầu dựa trên `employee_id` từ `ICurrentUser` kết hợp Redis Cache (`perm:{employee_id}`, TTL 5 phút).
- **Dependencies:** T11, T12
- **API Contracts:** `PermissionResponse`, `RoleDetailResponse` (từ `POS.Contracts.V1.Rbac`)
- **Acceptance Criteria:** Trả về lỗi 403 Forbidden nếu thiếu quyền; tự động query DB khi cache miss.
- **Estimated Size:** M

---

### T17 — [BE][FEATURE] Implement Role & Permission Management Commands/Queries

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Use cases quản lý Role và phân quyền: `GetRolesQuery`, `CreateRoleCommand`, `UpdateRolePermissionsCommand`. Xóa cache Redis của nhân viên khi cập nhật quyền của Role.
- **Dependencies:** T16
- **API Contracts:**
  - Request: `CreateRoleRequest`, `UpdateRoleRequest`, `UpdateRolePermissionsRequest`
  - Response: `RoleResponse`, `RoleDetailResponse`, `PermissionResponse`, `ResourceResponse` (từ `POS.Contracts.V1.Rbac`)
- **Acceptance Criteria:** Không cho sửa system role; cập nhật quyền atomicity; xóa cache Redis ngay lập tức.
- **Estimated Size:** M

---

### T18 — [BE/API][FEATURE] Implement RBAC Controller Endpoints

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Xây dựng `RolesController` cung cấp các API: `/roles`, `/roles/{id}/permissions`, `/resources`, `/permissions`.
- **Dependencies:** T17
- **API Contracts:**
  - Request: `CreateRoleRequest`, `UpdateRoleRequest`, `UpdateRolePermissionsRequest`
  - Response: `RoleResponse`, `RoleDetailResponse`, `PermissionResponse`, `ResourceResponse` (từ `POS.Contracts.V1.Rbac`), bọc trong `ApiResponse<T>`
- **Acceptance Criteria:** Caller phải có quyền quản trị; trả về HTTP 200 kèm danh sách quyền.
- **Estimated Size:** S

---

### T19 — [BE/API][FEATURE] Complete Store Management (Update, Status, Assign Admin)

- **Status:** PARTIAL | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Bổ sung các use case còn thiếu vào `StoresController`: `GetAllStoresQuery`, `UpdateStoreCommand`, `UpdateStoreStatusCommand`, `AssignAdminToStoreCommand`, `GrantOwnerAccessCommand`.
- **Dependencies:** T16, T08
- **API Contracts:**
  - Request: `CreateStoreRequest`, `UpdateStoreRequest`, `UpdateStoreStatusRequest`, `StoreAdminAssignmentRequest`, `StoreOwnerAccessRequest`
  - Response: `StoreResponse`, `StoreDetailResponse` (từ `POS.Contracts.V1.Stores`), bọc trong `ApiResponse<T>`
- **Acceptance Criteria:** Quản lý đầy đủ vòng đời cửa hàng và quyền truy cập đa cửa hàng cho Chủ chuỗi.
- **Estimated Size:** M

---

### T20 — [BE/API][FEATURE] Implement Employee CRUD, Lock & Reset PIN Use Cases

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Xây dựng `EmployeesController` và các use case: Tạo nhân viên, Sửa hồ sơ, Khóa tài khoản, Đặt lại mật khẩu / mã PIN.
- **Dependencies:** T16, T19
- **API Contracts:**
  - Request: `EmployeeFilterRequest`, `CreateEmployeeRequest`, `UpdateEmployeeRequest`, `LockEmployeeRequest`, `ResetPasswordRequest`, `ResetPinRequest`
  - Response: `EmployeeResponse`, `EmployeeDetailResponse`, `LoginHistoryResponse` (từ `POS.Contracts.V1.Employees`), bọc trong `ApiResponse<T>` / `PagedResponse<T>`
- **Acceptance Criteria:** `username` duy nhất toàn hệ thống, `pin` duy nhất trong cửa hàng, mã hóa BCrypt.
- **Estimated Size:** M

---

### T21 — [FE][FEATURE] Implement Employee Management WinUI Screen

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** FE
- **Description:** Xây dựng `frmEmployeeList.cs` và `frmEmployeeEdit.cs`, hỗ trợ gán Role, gán Store và nút đặt lại mã PIN nhanh.
- **Dependencies:** T20
- **API Contracts:** `EmployeeApiClient` -> `EmployeeFilterRequest`, `CreateEmployeeRequest`, `UpdateEmployeeRequest`, `LockEmployeeRequest`, `ResetPinRequest` / Nhận `EmployeeResponse`, `EmployeeDetailResponse`
- **Acceptance Criteria:** Danh sách có phân trang và lọc; form nhập liệu validate đầy đủ định dạng PIN.
- **Estimated Size:** M

---

### T22 — [BE][FEATURE] Implement Category Tree CRUD & Query Handlers

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Xây dựng các Use Cases cho Danh mục sản phẩm: `GetCategoriesTreeQuery` (cây cha-con), `CreateCategoryCommand`, `UpdateCategoryCommand`, `DeleteCategoryCommand`.
- **Dependencies:** T07
- **API Contracts:**
  - Request: `CreateCategoryRequest(string Name, string? Description, Guid? ParentId, int DisplayOrder)`, `UpdateCategoryRequest(...)`
  - Response: `CategoryResponse(Guid Id, string Name, string? Description, Guid? ParentId, int DisplayOrder, bool IsActive, int ProductCount, List<CategoryResponse> SubCategories)` (từ `POS.Contracts.V1.Categories`)
- **Acceptance Criteria:** Hỗ trợ danh mục đa cấp (`parent_id`); chặn xóa nếu đang có sản phẩm con.
- **Estimated Size:** S

---

### T23 — [BE/API][FEATURE] Implement Categories Controller Endpoints

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Tạo `CategoriesController` với các API: `GET /categories`, `POST /categories`, `PUT /categories/{id}`, `DELETE /categories/{id}`.
- **Dependencies:** T22
- **API Contracts:**
  - Request: `CreateCategoryRequest`, `UpdateCategoryRequest`
  - Response: `CategoryResponse` (từ `POS.Contracts.V1.Categories`), bọc trong `ApiResponse<T>`
- **Acceptance Criteria:** Trả về cây danh mục phân cấp bọc trong `ApiResponse<T>`.
- **Estimated Size:** S

---

### T24 — [BE][FEATURE] Implement Product & SKU Management Use Cases

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Triển khai `CreateProductCommand`, `UpdateProductCommand`, `CreateSkuCommand`, `UpdateSkuCommand`, `SearchProductsQuery`. Đảm bảo `barcode` và `sku_code` duy nhất trong cùng `store_id`.
- **Dependencies:** T22
- **API Contracts:**
  - Request: `ProductFilterRequest`, `CreateProductRequest`, `UpdateProductRequest`, `CreateSkuRequest`, `UpdateSkuRequest`
  - Response: `ProductSummaryResponse`, `ProductDetailResponse`, `SkuResponse`, `SkuDetailResponse` (từ `POS.Contracts.V1.Products`)
- **Acceptance Criteria:** Validate giá bán, giá vốn `>= 0`, thuế suất VAT hợp lệ (0, 5, 8, 10). Soft-delete SKU.
- **Estimated Size:** M

---

### T25 — [BE][FEATURE] Implement Unit Conversions & Price Lists Handlers

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** BE
- **Description:** Nghiệp vụ quy đổi đơn vị tính (`UnitConversions`) và Bảng giá theo thời gian/nhóm khách (`PriceLists`).
- **Dependencies:** T24
- **API Contracts:**
  - Request: `CreateUnitConversionRequest`, `UpdateUnitConversionRequest`, `CreatePriceListRequest`
  - Response: `UnitConversionResponse`, `PriceListResponse` (từ `POS.Contracts.V1.Products`)
- **Acceptance Criteria:** `conversion_factor > 0`; kiểm tra không chồng lấn khoảng thời gian hiệu lực của bảng giá.
- **Estimated Size:** S

---

### T26 — [BE/API][FEATURE] Implement Products, SKUs & Barcode Query Controller Endpoints

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Tạo `ProductsController` và `SkusController`, đặc biệt endpoint: `GET /skus/barcode/{code}` (quét mã vạch trả về SKU, giá và tồn kho khả dụng).
- **Dependencies:** T24, T25
- **API Contracts:**
  - Request: `ProductFilterRequest`
  - Response: `ProductSummaryResponse`, `ProductDetailResponse`, `SkuResponse`, `SkuDetailResponse`, `SkuBarcodeLookupResponse` (từ `POS.Contracts.V1.Products`), bọc trong `ApiResponse<T>` / `PagedResponse<T>`
- **Acceptance Criteria:** Response quét barcode < 50ms; trả về HTTP 404 nếu không tìm thấy mã vạch.
- **Estimated Size:** M

---

### T27 — [FE][FEATURE] Implement Product & Category Management WinUI Forms

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** FE
- **Description:** Xây dựng màn hình danh sách sản phẩm (`frmProductList.cs`) và form chi tiết (`frmProductEdit.cs`), hỗ trợ thêm nhiều biến thể SKU và in tem mã vạch.
- **Dependencies:** T23, T26
- **API Contracts:** `ProductApiClient` -> `ProductFilterRequest`, `CreateProductRequest`, `UpdateProductRequest`, `CreateSkuRequest` / Nhận `ProductSummaryResponse`, `ProductDetailResponse`, `SkuDetailResponse`, `CategoryResponse`
- **Acceptance Criteria:** Tìm kiếm nhanh, lọc theo danh mục; thêm biến thể màu/size động.
- **Estimated Size:** L

---

### T28 — [BE/API][FEATURE] Implement Bulk Product Excel Import via ClosedXML

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** BE
- **Description:** Xây dựng service nhập dữ liệu hàng loạt từ file Excel (`ClosedXML`), API `POST /products/bulk-import`.
- **Dependencies:** T24
- **API Contracts:**
  - Request/DTO: `BulkImportProductRow` (dòng dữ liệu Excel)
  - Response: `BulkImportResultResponse(int TotalRows, int SuccessCount, int FailedCount, List<string> Errors)` (từ `POS.Contracts.V1.Products`)
- **Acceptance Criteria:** Validate từng dòng dữ liệu, trả về danh sách chi tiết các dòng bị lỗi nếu có.
- **Estimated Size:** M

---

### T29 — [BE/API][FEATURE] Implement Supplier Management & Debt Tracking Use Cases

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** BE
- **Description:** Xây dựng Use Cases và Controller quản lý Nhà cung cấp (`Suppliers`) và Sổ cái thanh toán công nợ NCC (`SupplierPayments`).
- **Dependencies:** T07
- **API Contracts:**
  - Request: `SupplierFilterRequest`, `CreateSupplierRequest`, `UpdateSupplierRequest`, `CreateSupplierPaymentRequest`
  - Response: `SupplierResponse`, `SupplierPaymentResponse` (từ `POS.Contracts.V1.Inventory`), bọc trong `ApiResponse<T>` / `PagedResponse<T>`
- **Acceptance Criteria:** CRUD NCC; ghi nhận thanh toán tiền mặt/chuyển khoản cho NCC.
- **Estimated Size:** M

---

### T30 — [BE/API][FEATURE] Implement Stock-In Voucher Creation & Stock Transaction Ledger

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Nghiệp vụ Nhập kho: `CreateStockInVoucherCommand`, `CompleteStockInVoucherCommand`. Tự động tăng `qty_on_hand`, tạo `StockTransactions` loại `StockIn` và tính giá vốn bình quân.
- **Dependencies:** T24, T29
- **API Contracts:**
  - Request: `StockInVoucherFilterRequest`, `CreateStockInVoucherRequest`, `StockInVoucherItemRequest`, `UpdateStockInVoucherStatusRequest`
  - Response: `StockInVoucherSummaryResponse`, `StockInVoucherDetailResponse`, `StockInVoucherItemResponse`, `StockTransactionResponse` (từ `POS.Contracts.V1.Inventory`)
- **Acceptance Criteria:** Thực thi trong cùng một database transaction an toàn.
- **Estimated Size:** M

---

### T31 — [BE/API][FEATURE] Implement Stock Entries Realtime Queries, Min-Stock & Expiry Alerts

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Hiện thực `GetInventorySummaryQuery`, `GetStockAlertsQuery`, `DisposeStockCommand` (xuất hủy). Quản lý tồn theo Lô và Hạn dùng (`StockBatches`).
- **Dependencies:** T24, T30
- **API Contracts:**
  - Request: `InventoryFilterRequest`, `BatchFilterRequest`, `DisposeStockRequest`
  - Response: `StockEntryResponse`, `StockAlertResponse`, `StockBatchResponse`, `StockTransactionResponse` (từ `POS.Contracts.V1.Inventory`)
- **Acceptance Criteria:** Cảnh báo khi `qty_on_hand <= min_stock` hoặc hàng cận ngày hết hạn.
- **Estimated Size:** M

---

### T32 — [BE/API][FEATURE] Implement Stock Take (Kiểm kê) & Variance Approval Use Cases

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** BE
- **Description:** Nghiệp vụ kiểm kê kho: Tạo phiếu kiểm kê, quét đếm thực tế, Quản lý duyệt chênh lệch để tự động cân bằng tồn kho (`Adjust`).
- **Dependencies:** T31
- **API Contracts:**
  - Request: `CreateStockTakeRequest`, `StockTakeItemUpdateRequest`, `UpdateStockTakeItemsRequest`
  - Response: `StockTakeSummaryResponse`, `StockTakeDetailResponse`, `StockTakeItemResponse` (từ `POS.Contracts.V1.Inventory`)
- **Acceptance Criteria:** Tự động tính `diff_qty = actual_qty - system_qty`; chỉ Quản lý mới có quyền phê duyệt.
- **Estimated Size:** M

---

### T33 — [FE][FEATURE] Implement Inventory & Stock-In WinUI Screens

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** FE
- **Description:** Xây dựng màn hình WinUI: `frmStockIn.cs` (nhập kho), `frmStockTake.cs` (kiểm kê), `frmStockAlert.cs` (cảnh báo tồn kho).
- **Dependencies:** T30, T31, T32
- **API Contracts:** `InventoryApiClient` -> `CreateStockInVoucherRequest`, `CreateStockTakeRequest`, `StockInVoucherDetailResponse`, `StockTakeDetailResponse`, `StockAlertResponse`, `StockEntryResponse`
- **Acceptance Criteria:** Quét mã vạch nhập hàng nhanh; hiển thị màu sắc cảnh báo trực quan.
- **Estimated Size:** L

---

### T34 — [BE/API][FEATURE] Implement Shift Work Use Cases & Endpoints

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Quản lý ca làm việc: Mở ca (`opening_cash`), Đóng ca (kiểm đếm `actual_cash`, đối soát chênh lệch doanh thu tiền mặt/thẻ/QR).
- **Dependencies:** T12
- **API Contracts:**
  - Request: `ShiftFilterRequest`, `OpenShiftRequest(Guid StoreId, decimal OpeningCash, string? Note)`, `CloseShiftRequest(decimal ActualCash, string? Note)`
  - Response: `ShiftResponse`, `ShiftSummaryResponse` (từ `POS.Contracts.V1.Shifts`)
- **Acceptance Criteria:** Không cho mở ca mới nếu đang có ca mở; tính chính xác số tiền chênh lệch két.
- **Estimated Size:** M

---

### T35 — [FE][FEATURE] Implement Open/Close Shift WinUI Dialogs

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** FE
- **Description:** Hộp thoại `frmOpenShift.cs` (nhập quỹ đầu ca) và `frmCloseShift.cs` (nhập tiền đếm thực tế, đối soát và in biên bản giao ca).
- **Dependencies:** T34
- **API Contracts:** `ShiftApiClient` -> `OpenShiftRequest`, `CloseShiftRequest` / Nhận `ShiftResponse`, `ShiftSummaryResponse`
- **Acceptance Criteria:** Chặn không cho vào màn hình bán hàng nếu chưa mở ca; cảnh báo lệch két tiền.
- **Estimated Size:** M

---

### T36 — [BE/API][FEATURE] Implement Customer CRUD & Member Tier Upgrade Rules

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Quản lý Khách hàng toàn chuỗi: CRUD khách hàng, tìm kiếm theo SĐT/Mã vạch, tự động nâng hạng thẻ (`Normal` -> `Silver` -> `Gold` -> `VIP`) theo chi tiêu tích lũy.
- **Dependencies:** T07
- **API Contracts:**
  - Request: `CustomerFilterRequest`, `CreateCustomerRequest`, `UpdateCustomerRequest`, `UpdateMemberTierRequest`
  - Response: `CustomerSummaryResponse`, `CustomerDetailResponse`, `MemberTierResponse` (từ `POS.Contracts.V1.Customers`)
- **Acceptance Criteria:** SĐT khách hàng duy nhất; tự động nâng hạng khi đạt ngưỡng chi tiêu.
- **Estimated Size:** M

---

### T37 — [BE/API][FEATURE] Implement Loyalty Point Accrual & Redemption Use Cases

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Quản lý điểm thưởng: Tích điểm, Tiêu điểm đổi tiền giảm giá, Điều chỉnh điểm trong `PointTransactions`.
- **Dependencies:** T36
- **API Contracts:**
  - Request: `LoyaltyTransactionFilterRequest`, `AdjustPointsRequest(int Points, string Reason)`
  - Response: `LoyaltyAccountResponse`, `PointTransactionResponse` (từ `POS.Contracts.V1.Customers`)
- **Acceptance Criteria:** `points_balance >= 0`; ghi nhận đầy đủ nhật ký biến động điểm.
- **Estimated Size:** S

---

### T38 — [FE][FEATURE] Implement Customer Management & Loyalty WinUI Form

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** FE
- **Description:** Màn hình tìm kiếm nhanh khách hàng tại quầy (`frmCustomerLookup.cs`) và thêm/sửa khách hàng (`frmCustomerEdit.cs`).
- **Dependencies:** T36, T37
- **API Contracts:** `CustomerApiClient` -> `CustomerFilterRequest`, `CreateCustomerRequest`, `UpdateCustomerRequest`, `AdjustPointsRequest` / Nhận `CustomerSummaryResponse`, `CustomerDetailResponse`, `LoyaltyAccountResponse`
- **Acceptance Criteria:** Tìm kiếm SĐT tức thời (Debounce 300ms); cho phép tạo nhanh khách mới tại quầy.
- **Estimated Size:** M

---

### T39 — [DOMAIN][FEATURE] Implement Promotion Engine Domain Service

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Domain Service thuần túy `PromotionEngine`: Thuật toán đánh giá điều kiện, Stacking Rules (Priority, Exclusive, Stackable) cho 6 loại KM (`PercentSku`, `FixedSku`, `BuyXGetY`, `CartPercent`, `CartFixed`, `HappyHour`).
- **Dependencies:** T24
- **API Contracts:** Domain DTOs khớp cấu trúc `POS.Contracts.V1.Promotions.*` & `POS.Contracts.V1.Orders.OrderDiscountResponse`
- **Acceptance Criteria:** Không phụ thuộc DB/HTTP; không bao giờ giảm giá vượt quá giá trị đơn hàng; tốc độ < 5ms.
- **Estimated Size:** L

---

### T40 — [TEST] Add Comprehensive Unit Tests for Promotion Engine

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** TEST / BE
- **Description:** Viết bộ test `PromotionEngineTests.cs` kiểm thử hàng trăm kịch bản phối hợp khuyến mãi, combo, voucher độc quyền và cộng dồn.
- **Dependencies:** T39
- **API Contracts:** Unit test suites đánh giá hợp đồng giảm giá theo `PromotionSummaryResponse` & `OrderDiscountResponse`
- **Acceptance Criteria:** Code coverage của `PromotionEngine` đạt > 95%; 100% test cases pass.
- **Estimated Size:** M

---

### T41 — [BE/API][FEATURE] Implement Promotion & Voucher CRUD Use Cases & Validation Query

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** CRUD Khuyến mãi, CRUD Voucher và Query kiểm tra tính hợp lệ của mã giảm giá (`ValidateVoucherQuery`).
- **Dependencies:** T39
- **API Contracts:**
  - Request: `PromotionFilterRequest`, `CreatePromotionRequest`, `UpdatePromotionRequest`, `VoucherFilterRequest`, `CreateVoucherRequest`, `UpdateVoucherRequest`, `ValidateVoucherRequest`
  - Response: `PromotionSummaryResponse`, `PromotionDetailResponse`, `VoucherResponse`, `ValidateVoucherResponse` (từ `POS.Contracts.V1.Promotions`)
- **Acceptance Criteria:** Mã voucher duy nhất toàn chuỗi; kiểm tra giới hạn lượt dùng (`max_uses`).
- **Estimated Size:** M

---

### T42 — [BE][FEATURE] Implement Order Core Handlers (Create, Add Item, Recalculate Promo)

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Luồng giỏ hàng: Tạo đơn theo ca, Thêm SKU vào giỏ -> Gọi `PromotionEngine` -> Tự động cập nhật giảm giá, thuế và tổng tiền.
- **Dependencies:** T34, T39, T41
- **API Contracts:**
  - Request: `CreateOrderRequest`, `AddOrderItemRequest`, `ApplyVoucherRequest`
  - Response: `OrderSummaryResponse`, `OrderDetailResponse`, `OrderItemResponse`, `OrderDiscountResponse` (từ `POS.Contracts.V1.Orders`)
- **Acceptance Criteria:** Tự động tính toán lại giỏ hàng tức thời mỗi khi quét thêm sản phẩm.
- **Estimated Size:** L

---

### T43 — [BE][FEATURE] Implement Order Checkout & Status Progression Use Cases

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** `CheckoutOrderCommand`: Đơn hàng chỉ chuyển trạng thái `Paid` khi tổng các payment thành công bằng đúng `grand_total`. Hủy đơn yêu cầu quyền Manager.
- **Dependencies:** T42
- **API Contracts:**
  - Request: `CheckoutOrderRequest`, `CancelOrderRequest`
  - Response: `CheckoutResponse`, `OrderDetailResponse`, `ReceiptDataResponse` (từ `POS.Contracts.V1.Orders`)
- **Acceptance Criteria:** Đơn thiếu tiền giữ trạng thái chờ thanh toán tiếp; thanh toán đủ chuyển `Paid` và ghi nhận `paid_at`.
- **Estimated Size:** M

---

### T44 — [BE/API][FEATURE] Implement Orders Controller Endpoints

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Xây dựng `OrdersController` cung cấp toàn bộ API bán hàng: tạo đơn, thêm item, áp voucher, checkout, hủy đơn.
- **Dependencies:** T42, T43
- **API Contracts:**
  - Request: `OrderFilterRequest`, `CreateOrderRequest`, `AddOrderItemRequest`, `ApplyVoucherRequest`, `CheckoutOrderRequest`, `CancelOrderRequest`
  - Response: `OrderSummaryResponse`, `OrderDetailResponse`, `CheckoutResponse` (từ `POS.Contracts.V1.Orders`), bọc trong `ApiResponse<T>`
- **Acceptance Criteria:** Response bọc trong `ApiResponse<OrderResponse>`; tốc độ API thêm item < 100ms.
- **Estimated Size:** M

---

### T45 — [BE][FEATURE] Implement Split Payment Coordinator & Status Aggregation Handler

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Xử lý thanh toán kết hợp nhiều phương thức (`Cash`, `MoMo`, `VietQR`, `Card`, `Points`) trên cùng một đơn hàng, tính tiền thừa cho tiền mặt.
- **Dependencies:** T43
- **API Contracts:**
  - Request: `PaymentSplitRequest(string PaymentMethod, decimal Amount, string? TransactionRef, string? Note)`
  - Response: `OrderPaymentResponse` (từ `POS.Contracts.V1.Orders`), `PaymentStatusResponse` (từ `POS.Contracts.V1.Payments`)
- **Acceptance Criteria:** Chống trùng lặp giao dịch (`method`, `transaction_ref`); tính toán chính xác tiền thối.
- **Estimated Size:** M

---

### T46 — [INFRA][FEATURE] Implement MoMo & VietQR Payment Adapters with Webhook Verification

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE / INFRA
- **Description:** Tích hợp `MoMoPaymentAdapter` (sinh mã QR, xác thực Webhook chữ ký số HMAC-SHA256) và `VietQRAdapter` (sinh QR chuẩn NAPAS).
- **Dependencies:** T45
- **API Contracts:**
  - Request: `GenerateQrPaymentRequest`, `MomoWebhookRequest`, `VietQrWebhookRequest`
  - Response: `QrPaymentResponse`, `MomoWebhookResponse`, `VietQrWebhookResponse`, `PaymentStatusResponse` (từ `POS.Contracts.V1.Payments`)
- **Acceptance Criteria:** Sinh đúng mã QR động; từ chối mọi webhook giả mạo không đúng chữ ký.
- **Estimated Size:** L

---

### T47 — [INFRA/API][FEATURE] Implement SignalR PaymentHub for Realtime QR Payment Results

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE / INFRA
- **Description:** Xây dựng SignalR `PaymentHub`: Khi Webhook MoMo/VietQR xác nhận thành công, server push event `PaymentSuccess` về máy thu ngân tức thời.
- **Dependencies:** T45, T46
- **API Contracts:** SignalR Event Hub Payload mang `PaymentStatusResponse` / `QrPaymentResponse` (từ `POS.Contracts.V1.Payments`)
- **Acceptance Criteria:** Máy thu ngân nhận kết quả thanh toán trong < 500ms mà không cần polling.
- **Estimated Size:** M

---

### T48 — [BE/API][FEATURE] Implement Sequential Invoice Generation Use Case

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Tự động sinh Hóa đơn khi đơn chuyển `Paid`. Sinh mã hóa đơn tăng dần duy nhất: `HD-{StoreCode}-{YYYYMMDD}-{Sequence}`.
- **Dependencies:** T43, T45
- **API Contracts:**
  - Request: `InvoiceFilterRequest`
  - Response: `InvoiceSummaryResponse`, `InvoiceDetailResponse`, `InvoiceItemResponse` (từ `POS.Contracts.V1.Invoices`), bọc trong `ApiResponse<T>` / `PagedResponse<T>`
- **Acceptance Criteria:** Mã hóa đơn duy nhất toàn chuỗi, không bị nhảy số trùng khi nhiều quầy thanh toán đồng thời.
- **Estimated Size:** M

---

### T49 — [INFRA][FEATURE] Implement Thermal Receipt Printing (ESCPOS) & PDF Export (QuestPDF)

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE / INFRA
- **Description:** Xuất hóa đơn ra file PDF (`QuestPDF`) và in hóa đơn nhiệt khổ 80mm/58mm tự động cắt giấy (`ESCPOS.NET`).
- **Dependencies:** T48
- **API Contracts:** `ReceiptDataResponse` (từ `POS.Contracts.V1.Orders`), `InvoicePdfResponse` (từ `POS.Contracts.V1.Invoices`)
- **Acceptance Criteria:** In hóa đơn tiếng Việt Unicode chuẩn, không vỡ layout; xuất PDF tải về nhanh chóng.
- **Estimated Size:** M

---

### T50 — [FE][FEATURE] Implement POS Cashier Main Screen (frmSalesMain)

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** FE
- **Description:** Xây dựng giao diện màn hình bán hàng chính cho thu ngân: Quét barcode, giỏ hàng, tra cứu khách hàng, tổng tiền và các phím tắt (F1-F12).
- **Dependencies:** T26, T41, T44
- **API Contracts:** `OrderApiClient` -> `CreateOrderRequest`, `AddOrderItemRequest`, `ApplyVoucherRequest` / Nhận `OrderDetailResponse`, `SkuBarcodeLookupResponse`, `ValidateVoucherResponse`, `CustomerSummaryResponse`
- **Acceptance Criteria:** Quét mã vạch tự động nhảy dòng hàng < 100ms; hỗ trợ thao tác hoàn toàn bằng bàn phím.
- **Estimated Size:** L

---

### T51 — [FE][FEATURE] Implement Payment Selection & Dynamic QR Dialog (frmPaymentDialog)

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** FE
- **Description:** Hộp thoại thanh toán: Chọn Tiền mặt (gợi ý mệnh giá, tính tiền thừa), Hiện QR MoMo/VietQR, Chia tiền Split Payment, Lắng nghe SignalR tự động chốt đơn.
- **Dependencies:** T45, T47, T50
- **API Contracts:** `PaymentApiClient` -> `GenerateQrPaymentRequest`, `PaymentSplitRequest`, `CheckoutOrderRequest` / Nhận `QrPaymentResponse`, `PaymentStatusResponse`, `CheckoutResponse`
- **Acceptance Criteria:** Tự động hoàn tất đơn và in hóa đơn khi khách quét QR thành công trên điện thoại.
- **Estimated Size:** L

---

### T52 — [FE][FEATURE] Implement Invoice Viewer & Reprint Screen (frmInvoiceView)

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** FE
- **Description:** Màn hình tra cứu lịch sử hóa đơn bán lẻ và in lại khi cần (đóng dấu "BẢN SAO / REPRINT").
- **Dependencies:** T48, T49
- **API Contracts:** `InvoiceApiClient` -> `InvoiceFilterRequest` / Nhận `InvoiceSummaryResponse`, `InvoiceDetailResponse`, `InvoicePdfResponse`, `ReceiptDataResponse`
- **Acceptance Criteria:** Tìm kiếm hóa đơn cũ theo mã hoặc SĐT; phát lệnh in lại nhanh chóng.
- **Estimated Size:** S

---

### T53 — [BE][FEATURE] Implement Automatic Inventory Deduction & Loyalty Point Trigger on Paid

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** BE
- **Description:** Event Handler kích hoạt ngay khi đơn hàng `Paid`: Tự động trừ tồn kho `qty_on_hand`, tạo `StockTransactions` loại `SaleOut` và cộng điểm thành viên.
- **Dependencies:** T31, T37, T45
- **API Contracts:** Domain Event Trigger cập nhật DTOs `StockTransactionResponse`, `StockAlertResponse`, `PointTransactionResponse`
- **Acceptance Criteria:** Cập nhật kho và điểm thưởng trong cùng Unit of Work; kích hoạt cảnh báo nếu kho xuống dưới MinStock.
- **Estimated Size:** M

---

### T54 — [BE/API][FEATURE] Implement AI Chatbot Session & Message Provider

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** BE
- **Description:** Trợ lý ảo AI Chatbot: Quản lý Session, giới hạn số tin nhắn, kết nối OpenAI/Gemini API tra cứu thông tin sản phẩm và chính sách cửa hàng.
- **Dependencies:** T24
- **API Contracts:**
  - Request: `CreateChatSessionRequest`, `SendChatMessageRequest`
  - Response: `ChatSessionResponse`, `ChatMessageResponse`, `ChatMessageItemResponse`, `ChatHistoryResponse` (từ `POS.Contracts.V1.Chatbot`)
- **Acceptance Criteria:** Lưu vết hội thoại; tự động fallback khi câu hỏi nằm ngoài phạm vi; kiểm soát chi phí API.
- **Estimated Size:** M

---

### T55 — [BE/API][FEATURE] Implement Real-time Executive Dashboard Metrics Query

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** API `GET /reports/dashboard`: Tính toán doanh thu ngày, số đơn hoàn tất, AOV, biểu đồ doanh thu 7-30 ngày, top 5 bán chạy.
- **Dependencies:** T43, T31
- **API Contracts:**
  - Response: `DashboardSummaryResponse(decimal TodayRevenue, int TodayOrders, decimal AverageOrderValue, decimal RevenueGrowthPercentage, List<DailyRevenueChartItem> RevenueChart, List<TopSellingProductItem> TopSellingProducts, DashboardAlerts Alerts)` (từ `POS.Contracts.V1.Reports`)
- **Acceptance Criteria:** Truy vấn tổng hợp SQL tối ưu phản hồi < 200ms; hỗ trợ xem đa chi nhánh cho Chain Owner.
- **Estimated Size:** M

---

### T56 — [BE/API][FEATURE] Implement Multi-dimensional Revenue & Inventory Reports

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Báo cáo doanh thu đa chiều (theo nhân viên, ca, phương thức thanh toán), báo cáo hàng chậm luân chuyển, xuất file Excel (`ClosedXML`).
- **Dependencies:** T43, T31
- **API Contracts:**
  - Request: `RevenueReportFilterRequest`, `TopSellingReportFilterRequest`, `InventoryReportFilterRequest`, `ExportReportRequest`
  - Response: `RevenueReportResponse`, `InventoryReportResponse`, `SlowMovingItemResponse`, `ShiftReportResponse`, `ExportReportJobResponse`, `ExportJobStatusResponse` (từ `POS.Contracts.V1.Reports`)
- **Acceptance Criteria:** Xuất file Excel định dạng bảng đẹp mắt, chuẩn font tiếng Việt, có dòng tổng cộng.
- **Estimated Size:** L

---

### T57 — [FE][FEATURE] Implement WinUI Executive Dashboard & Report Visualizer

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** FE
- **Description:** Xây dựng màn hình Dashboard tổng quan (`frmDashboard.cs`) và màn hình xem báo cáo (`frmReport.cs`) có bộ lọc ngày tháng và nút xuất file Excel.
- **Dependencies:** T55, T56
- **API Contracts:** `ReportApiClient` -> `RevenueReportFilterRequest`, `ExportReportRequest` / Nhận `DashboardSummaryResponse`, `RevenueReportResponse`, `InventoryReportResponse`
- **Acceptance Criteria:** Biểu đồ hiển thị sắc nét, nút xuất Excel hoạt động trơn tru.
- **Estimated Size:** L

---

### T58 — [BE/FE][FEATURE] Implement System Configuration & Multi-language (i18n) Engine

- **Status:** MISSING | **Priority:** P2 - Medium | **Suggested Role:** BE / FE
- **Description:** Cơ chế cấu hình hệ thống (`SystemConfigs`) và chuyển đổi ngôn ngữ giao diện tức thời (`vi.json`, `en.json`) không cần build lại app.
- **Dependencies:** T07
- **API Contracts:**
  - Request: `UpdateStoreConfigRequest`, `UpdateI18nDictionaryRequest`
  - Response: `StoreConfigResponse`, `I18nDictionaryResponse` (từ `POS.Contracts.V1.Config`)
- **Acceptance Criteria:** Toàn bộ text nhãn trên WinUI thay đổi tức thì khi đổi ngôn ngữ trong cài đặt.
- **Estimated Size:** M

---

### T59 — [TEST] End-to-End Integration Tests for Complete Sales Cycle

- **Status:** MISSING | **Priority:** P0 - Critical | **Suggested Role:** TEST
- **Description:** Kịch bản kiểm thử tích hợp tự động toàn diện: Mở ca -> Quét hàng -> Áp khuyến mãi -> Thanh toán -> In bill -> Trừ kho -> Tích điểm -> Đóng ca đối soát.
- **Dependencies:** T35, T50, T51, T53
- **API Contracts:** Toàn bộ chuỗi Request/Response DTOs từ Login -> Shift -> Catalog -> Order -> Payment -> Invoice -> Inventory -> Shift Close
- **Acceptance Criteria:** Bộ test chạy tự động pass 100%; số dư tồn kho và quỹ két khớp chính xác tuyệt đối.
- **Estimated Size:** L

---

### T60 — [DEVOPS] Configure Production Docker Stack (Nginx SSL + Loki/Prometheus)

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** DEVOPS
- **Description:** Hoàn thiện hạ tầng triển khai Production: Nginx Reverse Proxy với SSL Let's Encrypt, cấu hình bảo mật production, kết nối Grafana Dashboard giám sát trực tiếp.
- **Dependencies:** T14, T02
- **API Contracts:** N/A (Production Docker Compose, Reverse Proxy SSL, Prometheus Metrics Scraper, Grafana Dashboard)
- **Acceptance Criteria:** Khởi chạy bằng 1 lệnh `docker compose`, bảo mật HTTPS, giám sát được lỗi API thời gian thực.
- **Estimated Size:** M

---

### T61 — [BE][FEATURE] Apply `IRequirePermission` to API Commands

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** BE
- **Description:** Rà soát toàn bộ command xử lý nghiệp vụ cho các API thuộc phạm vi của mình đảm nhiệm và cho command implement `IRequirePermission`. Mỗi command phải khai báo `RequiredPermission` theo đúng resource/action, để `AuthorizationBehavior` kiểm tra quyền trước khi handler thực thi; không tự kiểm tra JWT hoặc role trực tiếp trong handler.
- **Dependencies:** T16, T17, T19, T22, T24, T29, T36, T41, T42, T45
- **API Contracts:** Không thay đổi public API contract; sử dụng các permission code dạng `resource:action` đã seed trong `SystemPermissions`.
- **Acceptance Criteria:**
  - Tất cả command create/update/delete/approve/override thuộc các API trong phạm vi đều implement `IRequirePermission` và khai báo permission phù hợp.
  - Permission được ánh xạ đúng theo resource, ví dụ `categories:create`, `categories:update`, `products:delete`, `suppliers:create`, `orders:approve`.
  - `AuthorizationBehavior` được chạy trước handler; request chưa xác thực nhận `401 Unauthorized`, user thiếu quyền nhận `403 Forbidden`.
- **Estimated Size:** L

---

### T62 — [DB][FEATURE] Seed Default Store Category Tree

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** DB / BE
- **Description:** Xây dựng `CategorySeeder` để khởi tạo cây danh mục mẫu cho cửa hàng mặc định. Seeder tạo 7 category gốc và 22 category con, tổng cộng 29 category, theo đúng quan hệ `ParentId` và `StoreId`.
- **Dependencies:** T03, T08
- **Seed Data:**
  - `Đồ uống`: `Nước suối`, `Nước ngọt`, `Cà phê và trà`.
  - `Thực phẩm khô`: `Mì và cháo`, `Đồ hộp`, `Gia vị`, `Ngũ cốc, hạt ăn liền`.
  - `Bánh kẹo và ăn vặt`: `Bánh snack`, `Kẹo`, `Socola`.
  - `Sữa và dinh dưỡng`: `Sữa nước`, `Sữa chua`, `Sữa bột, sữa hạt`.
  - `Chăm sóc cá nhân`: `Dầu gội`, `Vệ sinh răng miệng`, `Chăm sóc da`.
  - `Kem & đồ đông lạnh`: `Kem que, kem hộp`, `Đá viên`, `Thực phẩm đông lạnh (chả giò, xúc xích)`.
  - `Đồ dùng gia đình nhỏ`: `Túi rác, màng bọc thực phẩm`, `Khăn giấy, giấy vệ sinh`, `Bao cao su, sản phẩm sức khỏe`.
- **API Contracts:** N/A (Infrastructure database seeding; không thay đổi public API contract)
- **Acceptance Criteria:**
  - Seeder chạy sau `StoreAndEmployeeSeeder` để sử dụng đúng `StoreId` của cửa hàng mặc định.
  - Tạo chính xác 29 category gồm 7 category gốc và 22 category con; category con tham chiếu đúng `ParentId`.
  - Tất cả category có `IsVisible = true`, `ImageUrl = null`, `DisplayOrder` ổn định và thuộc cùng một `StoreId`.
  - Seeder idempotent: chạy lại không tạo bản ghi trùng hoặc tạo thêm category mới.
- **Estimated Size:** M

---

### T63 — [ARCH/BE][REFACTOR] Extract Store Access Control into Policy Pattern

- **Status:** MISSING | **Priority:** P1 - High | **Suggested Role:** ARCH / BE
- **Description:** Tách logic kiểm tra quyền Store và RBAC khỏi handler bằng Policy Pattern để dễ mock khi test và dễ thay đổi cách phân quyền.
- **Implementation:**
  - Tạo `IStoreAccessPolicy` trong `POS.Application/Abstractions/Auth/`.
  - Tạo `StoreAccessPolicy` để gom logic từ `StoreManagementAccess` và `RoleAccessControl`.
  - Inject policy vào các Store/RBAC handlers thay cho việc gọi static class.
  - Đăng ký `IStoreAccessPolicy` với lifetime `Scoped` trong `DependencyInjection.cs`.
  - Xóa `StoreManagementAccess.cs` và `RoleAccessControl.cs` sau khi refactor.
- **Dependencies:** T16, T17, T19
- **API Contracts:** Không thay đổi public API contract; chỉ thay đổi dependency và implementation boundary trong Application layer.
- **Acceptance Criteria:**
  - Không còn handler nào gọi `StoreManagementAccess` hoặc `RoleAccessControl`.
  - Giữ nguyên kết quả kiểm tra `Unauthorized`, `Forbidden`, Owner và quyền truy cập Store/RBAC.
  - Unit test có thể mock `IStoreAccessPolicy` bằng `Substitute.For<IStoreAccessPolicy>()`.
  - Solution build thành công và toàn bộ test pass.
- **Estimated Size:** L

---

# PART 5 — SPRINT RECOMMENDATION (BACKEND-FIRST & BALANCED FULLSTACK)

Kế hoạch tổng thể gồm **Sprint 0 (Giai đoạn nền tảng - ĐÃ HOÀN THÀNH)** và **5 Sprint chính thức (10 tuần tiếp theo)** theo mô hình **Backend-First** (Sprint 1–3 hoàn thiện Backend; Sprint 4–5 hoàn thiện WinForms UI và E2E Test), phân chia cân bằng **2–3 tasks / người / sprint** cho 5 thành viên:

```text
SPRINT 0: BASELINE INFRASTRUCTURE & REFERENCE SLICE [COMPLETED]
├── T01: Clean Architecture Solution Setup (.NET 10)
├── T02: Docker Compose Dev & Prod Stacks
├── T03: AppDbContext, Fluent Configurations & Initial Migrations
├── T04: Auto Migration Runner on App Startup
├── T05: Serilog Loki Sink & Prometheus Setup
├── T06: Global Exception Handler & Scalar Docs
├── T07: MediatR Behaviors & Result Pattern
├── T08: Reference Store Management Slice (Backend)
├── T09: WinUI Client Skeleton & Store Screen (Frontend)
└── T10: V1 API Public Contracts Specification
```

---

### 📊 BẢNG MA TRẬN PHÂN CHIA SPRINT CHO 5 THÀNH VIÊN (2 – 3 TASK / NGƯỜI / SPRINT)

| Sprint | Leader (14 tasks) | Member A (10 tasks) | Member B (7 tasks) | Member C (8 tasks) | Member D (11 tasks) |
| :---: | :---: | :---: | :---: | :---: | :---: |
| **Sprint 1** *(Tuần 1–2)* | `T11, T12, T13` *(3 tasks)* | `T19, T20` *(2 tasks)* | `T29, T34` *(2 tasks)* | `T22, T23` *(2 tasks)* | `T36, T39` *(2 tasks)* |
| **Sprint 2** *(Tuần 3–4)* | `T14, T16, T17` *(3 tasks)* | `T45, T48` *(2 tasks)* | `T30, T31` *(2 tasks)* | `T24, T25` *(2 tasks)* | `T37, T40, T41` *(3 tasks)* |
| **Sprint 3** *(Tuần 5–6)* | `T18, T42, T43` *(3 tasks)* | `T46, T47` *(2 tasks)* | `T32` *(1 task L)* | `T26, T28` *(2 tasks)* | `T53, T54` *(2 tasks)* |
| **Sprint 4** *(Tuần 7–8)* | `T15, T44` *(2 tasks)* | `T21, T49` *(2 tasks)* | `T33` *(1 task L)* | `T27` *(1 task L)* | `T38, T55` *(2 tasks)* |
| **Sprint 5** *(Tuần 9–10)* | `T50, T59, T60` *(3 tasks)* | `T51, T52` *(2 tasks)* | `T35` *(1 task + Test)* | `T58` *(1 task M)* | `T56, T57` *(2 tasks)* |

---

### 🏃 SPRINT 1 (Tuần 1 – Tuần 2): Khởi tạo Nền tảng Core Backend & Danh mục Hàng hóa
- **Mục tiêu Sprint:** Khởi tạo dữ liệu hệ thống, phân quyền cơ bản, đăng nhập Password/PIN, quản lý Cửa hàng, Nhân viên, NCC, Ca làm việc, Danh mục và thuật toán Promotion lõi.
- **Phân bổ công việc:**
  - 👑 **Leader (3 tasks):** `T11` (Seed Roles & Perms DB), `T12` (Login Password & PIN BE), `T13` (Refresh Token Rotation BE).
  - 🅰️ **Member A (2 tasks):** `T19` (Complete Store Management BE/API), `T20` (Employee CRUD, Lock & Reset PIN BE).
  - 🅱️ **Member B (2 tasks):** `T29` (Supplier Management & Debt Tracking BE/API), `T34` (Shift Work Open/Close BE/API).
  - 🅲 **Member C (2 tasks):** `T22` (Category Tree CRUD BE), `T23` (Categories Controller Endpoints).
  - 🅳 **Member D (2 tasks):** `T36` (Customer CRUD & Tier Upgrade BE), `T39` (Promotion Engine Domain Service Core).
- **Kết quả nghiệm thu:** Đăng nhập được trên API; tạo được Cửa hàng, Nhân viên, Danh mục, Khách hàng; `PromotionEngine` tính giá cơ bản hoàn chỉnh.

---

### 🏃 SPRINT 2 (Tuần 3 – Tuần 4): RBAC Redis Cache, Kho vận, Sản phẩm & Khuyến mãi chuyên sâu
- **Mục tiêu Sprint:** Tối ưu bảo mật phân quyền có Redis Cache, xử lý Nhập kho & tính giá vốn, quản lý biến thể Multi-SKU, và bộ Unit Tests toàn diện cho khuyến mãi.
- **Phân bổ công việc:**
  - 👑 **Leader (3 tasks):** `T14` (Auth Controller Endpoints), `T16` (Dynamic RBAC Redis Pipeline BE), `T17` (Role & Permission Management BE).
  - 🅰️ **Member A (2 tasks):** `T45` (Split Payment Coordinator BE), `T48` (Sequential Invoice Generation BE).
  - 🅱️ **Member B (2 tasks):** `T30` (Stock-In Voucher & Ledger BE), `T31` (Stock Entries Realtime Queries & Alerts BE).
  - 🅲 **Member C (2 tasks):** `T24` (Product & SKU Management BE), `T25` (Unit Conversions & Price Lists BE).
  - 🅳 **Member D (3 tasks):** `T37` (Loyalty Point Accrual & Redemption BE), `T40` (Unit Tests Promotion Engine), `T41` (Promotion & Voucher CRUD BE).
- **Kết quả nghiệm thu:** Phân quyền có cache Redis; nhập kho cập nhật tồn kho tự động; quét barcode SKU nhanh; `PromotionEngine` pass 100% unit tests.

---

### 🏃 SPRINT 3 (Tuần 5 – Tuần 6): Đơn hàng, Cổng thanh toán, SignalR, Hóa đơn & AI Backend
- **Mục tiêu Sprint:** Hoàn thiện 100% Backend API: Luồng giỏ hàng, kết nối cổng thanh toán MoMo/VietQR Webhook, SignalR push realtime, Kiểm kê kho, trừ kho tự động và Trợ lý AI.
- **Phân bổ công việc:**
  - 👑 **Leader (3 tasks):** `T18` (RBAC Controller Endpoints), `T42` (Order Core Handlers & Giỏ hàng BE), `T43` (Order Checkout & Status Progression BE).
  - 🅰️ **Member A (2 tasks):** `T46` (MoMo & VietQR Payment Adapters with Webhook), `T47` (SignalR PaymentHub for Realtime QR).
  - 🅱️ **Member B (1 task lớn):** `T32` (Stock Take Kiểm kê kho & Variance Approval BE/API).
  - 🅲 **Member C (2 tasks):** `T26` (Products, SKUs & Barcode Query Controller), `T28` (Bulk Product Excel Import ClosedXML).
  - 🅳 **Member D (2 tasks):** `T53` (Auto Inventory Deduction & Loyalty Trigger on Paid BE), `T54` (AI Chatbot Session & Provider BE).
- **Kết quả nghiệm thu:** Toàn bộ Backend & Infra hoàn thành 100%. Test thông suốt luồng tạo đơn -> QR payment webhook -> push SignalR -> trừ kho -> sinh hóa đơn.

---

### 🖥️ SPRINT 4 (Tuần 7 – Tuần 8): Giao diện WinForms Quản trị & Vận hành Cửa hàng
- **Mục tiêu Sprint:** Xây dựng các màn hình Desktop Quản trị dữ liệu, Vận hành kho, Mở/Đóng ca và Tra cứu khách hàng (kết nối trực tiếp vào Backend đã ổn định).
- **Phân bổ công việc:**
  - 👑 **Leader (2 tasks):** `T15` (WinUI Login Screen PIN & Password), `T44` (Orders Controller Endpoints hoàn thiện).
  - 🅰️ **Member A (2 tasks):** `T21` (WinUI Employee Management Screen), `T49` (Thermal Receipt Printing ESC/POS & PDF Export).
  - 🅱️ **Member B (1 task lớn):** `T33` (WinUI Inventory & Stock-In Screens: `frmStockIn`, `frmStockTake`, `frmStockAlert`).
  - 🅲 **Member C (1 task lớn):** `T27` (WinUI Product & Category Management Forms: `frmProductList`, `frmProductEdit`).
  - 🅳 **Member D (2 tasks):** `T38` (WinUI Customer Management & Loyalty Form), `T55` (Real-time Executive Dashboard Metrics Query BE).
- **Kết quả nghiệm thu:** Đăng nhập mã PIN cảm ứng; mở ca; quản lý kho hàng, sản phẩm và khách hàng mượt mà trên Desktop App.

---

### 🖥️ SPRINT 5 (Tuần 9 – Tuần 10): Màn hình POS Thu ngân chính, Realtime QR, Dashboard & Nghiệm thu
- **Mục tiêu Sprint:** Hoàn thiện màn hình bán hàng chính cho thu ngân, popup thanh toán QR SignalR, xem/in lại hóa đơn, biểu đồ Dashboard doanh thu, Đa ngôn ngữ và kiểm thử E2E.
- **Phân bổ công việc:**
  - 👑 **Leader (3 tasks):** `T50` (WinUI POS Cashier Main Screen `frmSalesMain` — phím tắt F1-F12, giỏ hàng, barcode), `T59` (End-to-End Integration Tests Sales Cycle), `T60` (Production Docker Stack Nginx SSL + Loki/Prometheus).
  - 🅰️ **Member A (2 tasks):** `T51` (WinUI Payment Selection & Dynamic QR Dialog `frmPaymentDialog`), `T52` (WinUI Invoice Viewer & Reprint Screen `frmInvoiceView`).
  - 🅱️ **Member B (1 task + Test):** `T35` (WinUI Open/Close Shift Dialogs: `frmOpenShift`, `frmCloseShift`), phối hợp Leader test đối soát két tiền thực tế.
  - 🅲 **Member C (1 task):** `T58` (System Configuration & Multi-language i18n Engine BE/FE).
  - 🅳 **Member D (2 tasks):** `T56` (Multi-dimensional Revenue & Inventory Reports BE), `T57` (WinUI Executive Dashboard & Report Visualizer).
- **Kết quả nghiệm thu:** Hoàn thành 100% đồ án! Thu ngân thao tác quét mã vạch bán hàng, thanh toán QR tự động chốt đơn qua SignalR, in bill nhiệt và xem biểu đồ báo cáo trực quan. Bộ test E2E pass toàn diện.

# 🏪 Kiến Trúc Hệ Thống POS — Phiên Bản Thực Tế & Đầy Đủ

> **Mô hình**: WinForms .NET 8 (Client) + ASP.NET Core Web API (Cloud Server) + SQL Server
> **Pattern**: MVP (WinForms UI) + Service + Repository Pattern + DI
> **Auth & Logging**: JWT Bearer + Đăng nhập PIN/Password & Grafana Stack (Loki + Prometheus + Grafana)
> **Triển khai**: Docker + Docker Compose (dev & production)

---

## 1. 🗺️ BỨC TRANH TỔNG THỂ

```
 ┌─────────────────────────────────────────────────────────────────────────┐
 │                      CLOUD SERVER (VPS / Azure)                         │
 │                                                                         │
 │   ┌─────────────────────────────────────────────────────────────────┐   │
 │   │               ASP.NET Core Web API (.NET 10)                    │   │
 │   │               https://api.domain.com/api/v1                     │   │
 │   │               Scalar / Swagger UI: /swagger                     │   │
 │   └──────────────────────────────┬──────────────────────────────────┘   │
 │                                  │                                      │
 │   ┌──────────────────────────────▼──────────────────────────────────┐   │
 │   │                      SQL Server 2022                            │   │
 │   │                (1 DB duy nhất, phân biệt StoreId)               │   │
 │   └──────────────────────────────┬──────────────────────────────────┘   │
 │                                  │                                      │
 │   ┌──────────────────────────────┴──────────────────────────────────┐   │
 │   │                MONITORING & LOGGING STACK                       │   │
 │   │  Serilog ──► Loki (Log Data) ──────┐                            │   │
 │   │  /metrics ──► Prometheus (Metrics) ├──► Grafana UI (:3000)      │   │
 │   └────────────────────────────────────┘                            │   │
 └─────────────────────────────────────────────────────────────────────────┘
              ▲                                        ▲
              |                                        |
              │ HTTPS REST + JWT                       │ HTTP REST
 ┌────────────┴──────────┐                ┌────────────┴──────────────┐
 │   WinForms .NET 8     │                │   Web Client (Tương lai)  │
 │   (Quầy thu ngân)     │                │   React / Blazor / Vue    │
 │   Store A: Quầy 1,2,3 │                │   (Admin Dashboard)       │
 │   Store B: Quầy 1,2   │                │                           │
 └───────────────────────┘                └───────────────────────────┘
```

> Toàn bộ khối "CLOUD SERVER" (API, SQL Server, Redis, Nginx, monitoring stack) chạy trong Docker. WinForms là app desktop cài trực tiếp lên máy Windows tại quầy — không chạy trong container, chỉ gọi API qua HTTPS ra ngoài.

---

## 2. 🏗️ KIẾN TRÚC LAYER — MVP + Service + Repository

### Nguyên tắc cốt lõi

- **UI (WinForms)** không biết gì về DB, chỉ gọi API
- **Presenter** điều phối giữa View và Service — có thể unit test
- **Service** chứa business logic — có thể unit test vì dùng Interface
- **Repository** ẩn EF Core — chỉ Service biết Repository
- **DI Container** kết nối tất cả — swap implementation dễ dàng

```
┌──────────────────────────────────────────────────────────────┐
│                    WINFORMS CLIENT (.NET 10)                 │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐     │
│  │  View Layer (Forms)                                 │     │
│  │  frmSalesMain | frmProduct | frmInventory | ...     │     │
│  │  Implement IXxxView interface                       │     │
│  │  Chỉ hiển thị UI, không có logic                    │     │
│  └──────────────────────┬──────────────────────────────┘     │
│                         │ gọi / nhận event                   │
│  ┌──────────────────────▼──────────────────────────────┐     │
│  │  Presenter Layer                                    │     │
│  │  ProductPresenter | OrderPresenter | ...            │     │
│  │  Điều phối View ↔ ApiClient                         │     │
│  └──────────────────────┬──────────────────────────────┘     │
│                         │ gọi HTTP                           │
│  ┌──────────────────────▼──────────────────────────────┐     │
│  │  API Client Layer                                   │     │
│  │  ProductApiClient | OrderApiClient | ...            │     │
│  │  HttpClient + Polly (retry, circuit breaker)        │     │
│  └─────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────┘
                          │ HTTPS REST + JWT
                          ▼
┌──────────────────────────────────────────────────────────────┐
│                    CLOUD BACKEND                             │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  API Layer (ASP.NET Core Controllers)                │    │
│  │  Auth Middleware | JWT Validation                    │    │
│  │  TenantMiddleware (xác định StoreId từ token)        │    │
│  │  Swagger / Scalar (API docs)                         │    │
│  └───────────────────────┬──────────────────────────────┘    │
│                          │ gọi                               │
│  ┌───────────────────────▼──────────────────────────────┐    │
│  │  Application Layer (Business Logic)                  │    │
│  │  MediatR Command/Query + Handler theo từng use case  │    │
│  │  Pipeline: Validation | Authorization | Logging      │    │
│  └───────────────────────┬──────────────────────────────┘    │
│                          │ gọi qua Interface                 │
│  ┌───────────────────────▼──────────────────────────────┐    │
│  │  Repository Abstractions                             │    │
│  │  IProductRepository | IOrderRepository | ...         │    │
│  │  Interface đặt ở Application,Infrastructure implement│    │
│  └───────────────────────┬──────────────────────────────┘    │
│                          │                                   │
│  ┌───────────────────────▼──────────────────────────────┐    │
│  │  Infrastructure                                      │    │
│  │  AppDbContext (EF Core / Npgsql) | SQL Server 2022   │    │
│  │  Redis | SignalR | MinIO | MoMo Adapter | VietQR     │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

---

## 3. 📁 CẤU TRÚC SOLUTION

```
POS.sln
│
├── 🌐 POS.API/                          (.NET 10 Web API)
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── EmployeesController.cs
│   │   ├── StoresController.cs
│   │   ├── ProductsController.cs
│   │   ├── SkusController.cs
│   │   ├── CategoriesController.cs
│   │   ├── InventoryController.cs
│   │   ├── OrdersController.cs
│   │   ├── PaymentsController.cs
│   │   ├── InvoicesController.cs
│   │   ├── CustomersController.cs
│   │   ├── FeedbacksController.cs
│   │   ├── ChatbotController.cs
│   │   ├── FaqController.cs
│   │   ├── PromotionsController.cs
│   │   ├── VouchersController.cs
│   │   ├── ShiftsController.cs
│   │   ├── ReportsController.cs
│   │   └── ConfigController.cs
│   ├── Middleware/
│   │   ├── ExceptionMiddleware.cs       ← Bắt lỗi toàn cục → trả JSON
│   │   └── RequestLoggingMiddleware.cs  ← Ghi log HTTP request/response
│   ├── Hubs/
│   │   └── PaymentHub.cs               ← SignalR: push QR payment result
│   ├── Filters/
│   │   └── ValidationFilter.cs         ← Auto validate request
│   ├── Mapping/
│   │   ├── ProductMappings.cs          ← Map Contract Request ↔ Application
│   │   ├── OrderMappings.cs
│   │   └── CommonMappings.cs
│   └── Program.cs                      ← DI setup, Swagger, Auth & Serilog config
│
├── 📋 POS.Application/                  (Business Logic)
│   ├── Abstractions/                    ← Cổng đi ra ngoài Application, Infrastructure implement
│   │   ├── Persistence/
│   │   │   ├── IProductRepository.cs
│   │   │   ├── ISkuRepository.cs
│   │   │   ├── IInventoryRepository.cs
│   │   │   ├── IOrderRepository.cs
│   │   │   ├── IPaymentRepository.cs
│   │   │   ├── IInvoiceRepository.cs
│   │   │   ├── IPromotionRepository.cs
│   │   │   ├── ICustomerRepository.cs
│   │   │   ├── IFeedbackRepository.cs
│   │   │   ├── IFaqRepository.cs
│   │   │   ├── IChatConversationRepository.cs
│   │   │   ├── IEmployeeRepository.cs
│   │   │   ├── IStoreRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Auth/
│   │   │   └── ICurrentUser.cs
│   │   ├── Payments/
│   │   │   └── IPaymentGateway.cs
│   │   ├── Chatbot/
│   │   │   └── IChatbotAiProvider.cs
│   │   ├── Caching/
│   │   │   └── ICacheService.cs
│   │   ├── Storage/
│   │   │   └── IFileStorageService.cs
│   │   └── Notifications/
│   │       └── IRealtimeEventPublisher.cs
│   ├── UseCases/                        ← Nhóm use case theo feature nghiệp vụ
│   │   ├── Products/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateProduct/
│   │   │   │   │   ├── CreateProductCommand.cs
│   │   │   │   │   ├── CreateProductCommandHandler.cs
│   │   │   │   │   └── CreateProductCommandValidator.cs
│   │   │   │   ├── UpdateProduct/
│   │   │   │   │   ├── UpdateProductCommand.cs
│   │   │   │   │   ├── UpdateProductCommandHandler.cs
│   │   │   │   │   └── UpdateProductCommandValidator.cs
│   │   │   │   └── DeleteProduct/
│   │   │   │       ├── DeleteProductCommand.cs
│   │   │   │       └── DeleteProductCommandHandler.cs
│   │   │   ├── Queries/
│   │   │   │   ├── SearchProducts/
│   │   │   │   │   ├── SearchProductsQuery.cs
│   │   │   │   │   ├── SearchProductsQueryHandler.cs
│   │   │   │   │   └── SearchProductsQueryValidator.cs
│   │   │   │   └── GetProductDetail/
│   │   │   │       ├── GetProductDetailQuery.cs
│   │   │   │       └── GetProductDetailQueryHandler.cs
│   │   ├── Skus/
│   │   ├── Orders/
│   │   ├── Inventory/
│   │   ├── Payments/
│   │   ├── Invoices/
│   │   ├── Employees/
│   │   ├── Stores/
│   │   ├── Promotions/
│   │   ├── Customers/
│   │   ├── Chatbot/
│   │   ├── Shifts/
│   │   ├── Reports/
│   │   ├── Auth/
│   │   └── Config/
│   └── Common/
│       ├── Behaviors/
│       │   ├── ValidationBehavior.cs
│       │   ├── AuthorizationBehavior.cs
│       │   ├── LoggingBehavior.cs
│       │   └── TransactionBehavior.cs
│       └── PagedResult.cs
│
├── 🏛️ POS.Domain/                       (Entities + Value Objects + Domain Services)
│   ├── Entities/
│   │   ├── Product/
│   |   │   ├── Enums/
│   |   │   ├── Product.cs
│   │   ├── Skus/
│   │   ├── Orders/
│   │   ├── Inventory/
│   │   ├── Payments/
│   │   ├── Invoices/
│   │   ├── Employees/
│   │   ├── Stores/
│   │   ├── Promotions/
│   │   ├── Customers/
│   │   ├── Chatbot/
│   │   ├── Shifts/
│   │   ├── Reports/
│   │   ├── Auth/
│   │   └── Config/
│   ├── ValueObjects/
│   ├── Common/
│   │   └── Result.cs
│   │   └── Error.cs
│   └── Services/                       ← Domain Service: logic nghiệp vụ thuần, không đụng DB/Redis/HTTP
│       ├── IPromotionEngine.cs
│       ├── PromotionEngine.cs
│       └── StockAvailabilityChecker.cs
│
├── 🔧 POS.Infrastructure/               (EF Core, SQL Server, External APIs)
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/              ← EF Fluent API (1 file per Entity)
│   │   │   ├── ProductConfiguration.cs
│   │   │   ├── OrderConfiguration.cs
│   │   │   └── ...
│   │   ├── Repositories/                ← Implement interface từ Application/Abstractions
│   │   │   ├── ProductRepository.cs
│   │   │   ├── SkuRepository.cs
│   │   │   ├── InventoryRepository.cs
│   │   │   ├── OrderRepository.cs
│   │   │   └── ...
│   │   └── Migrations/                  ← EF Core migrations
│   ├── Logging/                         ← Cấu hình Logging & Sink
│   │   └── SerilogLokiConfiguration.cs  ← Cấu hình đẩy Log về Grafana Loki
│   ├── Cache/
│   │   └── RedisCacheService.cs
│   ├── Payment/
│   │   ├── MoMoPaymentAdapter.cs        ← Implement IPaymentGateway
│   │   └── VietQRAdapter.cs
│   ├── Storage/
│   │   └── FileStorageService.cs        ← MinIO / Azure Blob
│   ├── Notifications/
│   │   └── SignalREventService.cs
│   ├── Reports/
│   │   ├── ExcelReportService.cs        ← ClosedXML
│   │   └── PdfReportService.cs          ← QuestPDF
│   └── DependencyInjection.cs           ← Extension method đăng ký services
│
├── 🖥️ POS.WinForms/                     (.NET 10 WinForms)
│   ├── Forms/
│   │   ├── Auth/
│   │   │   └── frmLogin.cs
│   │   ├── Dashboard/
│   │   │   └── frmDashboard.cs          ← Màn hình đầu tiên sau đăng nhập
│   │   ├── Sales/
│   │   │   ├── frmSalesMain.cs          ← Màn hình bán hàng chính
│   │   │   ├── frmPaymentDialog.cs      ← Dialog chọn thanh toán + QR
│   │   │   └── frmInvoiceView.cs        ← Xem lại / in lại hóa đơn
│   │   ├── Employees/
│   │   │   ├── frmEmployeeList.cs
│   │   │   └── frmEmployeeEdit.cs
│   │   ├── Stores/
│   │   │   ├── frmStoreList.cs          ← Owner: danh sách cửa hàng trong chuỗi
│   │   │   └── frmStoreEdit.cs
│   │   ├── Products/
│   │   │   ├── frmProductList.cs
│   │   │   └── frmProductEdit.cs
│   │   ├── Inventory/
│   │   │   ├── frmStockIn.cs
│   │   │   ├── frmStockTake.cs
│   │   │   └── frmStockAlert.cs
│   │   ├── Customers/
│   │   │   ├── frmCustomerLookup.cs
│   │   │   └── frmCustomerEdit.cs
│   │   ├── Feedbacks/
│   │   │   ├── frmFeedbackList.cs
│   │   │   └── frmFeedbackDetail.cs
│   │   ├── Promotions/
│   │   │   └── frmPromotionList.cs
│   │   ├── Shifts/
│   │   │   ├── frmOpenShift.cs
│   │   │   └── frmCloseShift.cs
│   │   ├── Reports/
│   │   │   └── frmReport.cs
│   │   └── Settings/
│   │       └── frmSettings.cs
│   ├── Presenters/                      ← MVP Presenters
│   │   ├── SalesPresenter.cs
│   │   ├── DashboardPresenter.cs
│   │   ├── EmployeePresenter.cs
│   │   ├── StorePresenter.cs
│   │   ├── InvoicePresenter.cs
│   │   ├── ProductPresenter.cs
│   │   ├── InventoryPresenter.cs
│   │   ├── CustomerPresenter.cs
│   │   ├── FeedbackPresenter.cs
│   │   ├── ShiftPresenter.cs
│   │   └── ReportPresenter.cs
│   ├── ViewInterfaces/                  ← View Contracts (MVP)
│   │   ├── ISalesView.cs
│   │   ├── IDashboardView.cs
│   │   ├── IEmployeeView.cs
│   │   ├── IStoreView.cs
│   │   ├── IInvoiceView.cs
│   │   ├── IProductView.cs
│   │   ├── IInventoryView.cs
│   │   ├── ICustomerView.cs
│   │   ├── IFeedbackView.cs
│   │   ├── IShiftView.cs
│   │   └── IReportView.cs
│   ├── ApiClients/                      ← Gọi REST API
│   │   ├── BaseApiClient.cs             ← HttpClient base + token management
│   │   ├── ProductApiClient.cs
│   │   ├── OrderApiClient.cs
│   │   ├── PaymentApiClient.cs
│   │   ├── InvoiceApiClient.cs
│   │   ├── EmployeeApiClient.cs
│   │   ├── StoreApiClient.cs
│   │   ├── InventoryApiClient.cs
│   │   ├── CustomerApiClient.cs
│   │   ├── FeedbackApiClient.cs
│   │   ├── PromotionApiClient.cs
│   │   ├── ShiftApiClient.cs
│   │   └── ReportApiClient.cs
│   ├── Services/
│   │   ├── BarcodeService.cs            ← Xử lý input từ barcode scanner
│   │   ├── PrinterService.cs            ← In hóa đơn nhiệt ESC/POS
│   │   ├── QRDisplayService.cs          ← Hiển thị QR payment
│   │   ├── SignalRClientService.cs      ← Nhận real-time từ server
│   │   ├── OfflineCacheService.cs       ← SQLite local
│   │   ├── SessionService.cs            ← Quản lý token, thông tin user
│   │   └── AutoUpdateService.cs         ← Tự cập nhật app
│   └── Program.cs                       ← DI setup
│
├── 📦 POS.Contracts/                    (Public API Contracts dùng chung cho API client)
│   ├── V1/
│   │   ├── Common/
│   │   │   ├── ApiResponse.cs
│   │   │   ├── ApiError.cs
│   │   │   └── PagedResponse.cs
│   │   ├── Products/
│   │   │   ├── CreateProductRequest.cs
│   │   │   ├── UpdateProductRequest.cs
│   │   │   ├── ProductResponse.cs
│   │   │   └── ProductListItemResponse.cs
│   │   ├── Orders/
│   │   │   ├── CreateOrderRequest.cs
│   │   │   ├── CheckoutRequest.cs
│   │   │   ├── OrderResponse.cs
│   │   │   └── OrderListItemResponse.cs
│   │   └── Auth/
│   │       ├── LoginRequest.cs
│   │       ├── PinLoginRequest.cs
│   │       └── TokenResponse.cs
│   └── Enums/                           ← Enum cần expose ra client
│
├── 🧪 POS.Tests/                        (Unit Tests — tách theo tầng)
│   ├── Domain/
│   │   ├── PromotionEngineTests.cs      ← Test kỹ nhất, không mock gì (logic thuần)
│   │   └── StockAvailabilityCheckerTests.cs
│   ├── Application/
│   │   ├── OrderServiceTests.cs         ← Mock IRepository/ICacheService
│   │   ├── PromotionServiceTests.cs
│   │   └── InventoryServiceTests.cs
│   └── WinForms/
│       └── SalesPresenterTests.cs       ← Mock IView + ApiClient
│
└── 🐳 deploy/                            (Docker & hạ tầng triển khai)
    ├── Dockerfile                        ← Multi-stage build cho POS.API
    ├── docker-compose.yml                ← Base: SQL server, Redis, API, Loki, Prometheus, Grafana (dev)
    ├── docker-compose.prod.yml           ← Override cho production: Nginx + SSL, healthcheck, restart policy
    ├── .env.example
    ├── nginx/
    │   └── nginx.conf
    └── prometheus/
        └── prometheus.yml
```

### Quy ước thiết kế

- `POS.Contracts` chỉ chứa public API contract: `Request`, `Response`, `ApiResponse`, `ApiError`, `PagedResponse`, enum cần expose ra WinForms/Web Client.
- `POS.Application` không chứa API `Request/Response`. Layer này dùng `Command` cho thao tác ghi, `Query` cho thao tác đọc, và `Dto`/`PagedResult` cho kết quả nội bộ.
- Các feature nghiệp vụ đặt trong `POS.Application/UseCases`, ví dụ `UseCases/Products`, `UseCases/Orders`, `UseCases/Inventory`.
- Mỗi use case chính có `Command/Query + Handler + Validator` trong cùng một folder, ví dụ `UseCases/Products/Commands/CreateProduct`.
- Không dùng `IProductService/ProductService` làm Application Service chính khi đã dùng MediatR, tránh flow thừa lớp kiểu `Controller -> Mediator -> Handler -> Service -> Repository`.
- Controller gọi `mediator.Send(command/query)`; pipeline behavior chạy validation, authorization, logging, transaction trước/sau handler.
- List và search dùng chung một query có filter/paging/sort, ví dụ `SearchProductsQuery`. Khi không truyền `keyword/filter` thì đó là lấy danh sách.
- `POS.API` chịu trách nhiệm map `Contract Request -> Application Command/Query` và `Application Dto -> Contract Response`.
- Repository/payment/cache/storage/current-user interface đặt ở `POS.Application/Abstractions`; implementation bằng EF Core/external service đặt ở `POS.Infrastructure`.
- Domain service vẫn được dùng cho logic thuần như `PromotionEngine`, `StockAvailabilityChecker`, `PriceCalculator`; handler gọi các domain service này sau khi đã lấy đủ dữ liệu.
- `POS.Domain` chỉ giữ entity, enum, value object, domain service thuần; không biết repository, EF Core, HTTP, Redis.

Dependency chuẩn:

```
POS.WinForms -> POS.Contracts
POS.API -> POS.Contracts + POS.Application
POS.Application -> POS.Domain
POS.Infrastructure -> POS.Application + POS.Domain
POS.Domain -> không phụ thuộc project nào
```

---

## 5. 🔌 API ENDPOINTS (Swagger / Scalar)

```
Base: https://api.yourpos.com/api/v1
Auth: Bearer JWT  |  Header: X-Store-Id
(role=Owner: thay X-Store-Id bằng query param ?storeIds=1,2,3 để chọn phạm vi
nhiều cửa hàng, hoặc bỏ trống để lấy tất cả cửa hàng được cấp quyền)

━━━ AUTH ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
POST   /auth/login              { username, password }
POST   /auth/pin                { pin }
POST   /auth/refresh            { refreshToken }
POST   /auth/logout             { refreshToken }
POST   /auth/change-pin         { oldPin, newPin }
POST   /auth/change-password    { oldPassword, newPassword }
GET    /auth/me

━━━ EMPLOYEES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /employees               ?storeId=&roleId=  (Owner xem nhiều store)
POST   /employees               { name, username, password, pin, roleId, storeId? }
GET    /employees/{id}
PUT    /employees/{id}
PUT    /employees/{id}/lock     { isActive }
POST   /employees/{id}/reset-password  { newPassword }
POST   /employees/{id}/reset-pin       { newPin }
GET    /employees/{id}/login-history   (audit log đăng nhập — từ AuditLogs)

━━━ STORES (chỉ Owner) ━━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /stores
POST   /stores                  { name, address, phone, timezone, currencyCode }
GET    /stores/{id}
PUT    /stores/{id}
PUT    /stores/{id}/status      { isActive }
POST   /stores/{id}/assign-admin       { employeeId }
POST   /stores/{id}/grant-owner-access { employeeId }  (EmployeeStoreAccess)
DELETE /stores/{id}/revoke-owner-access/{employeeId}

━━━ RBAC (chỉ Owner / Admin) ━━━━━━━━━━━━━━━━━━━━
GET    /roles                   ?storeId=  (bao gồm system roles + custom roles)
POST   /roles                   { name, description, storeId? }
GET    /roles/{id}
PUT    /roles/{id}
DELETE /roles/{id}              (chỉ custom role, không xóa system role)
GET    /roles/{id}/permissions
PUT    /roles/{id}/permissions  { permissionIds[] }  (ghi đè toàn bộ danh sách)

GET    /resources               (danh sách resource có thể phân quyền)
GET    /permissions             ?resourceId=  (danh sách hành động trên resource)

━━━ PRODUCTS & SKU ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /products                ?search=&categoryId=&page=&size=
POST   /products
GET    /products/{id}
PUT    /products/{id}
DELETE /products/{id}
GET    /products/{id}/skus
POST   /products/{id}/skus      { skuCode, barcode, attributesJson, costPrice,
                                  sellPrice, taxRate }
POST   /products/bulk-import    (multipart Excel)

GET    /skus/{id}
PUT    /skus/{id}
DELETE /skus/{id}               (soft-delete: is_active = 0)
GET    /skus/barcode/{code}     Quét barcode → SKU + giá + tồn kho

GET    /skus/{id}/unit-conversions            (bảng UnitConversions)
POST   /skus/{id}/unit-conversions            { unitName, conversionFactor, sellPrice }
PUT    /skus/{id}/unit-conversions/{ucId}
DELETE /skus/{id}/unit-conversions/{ucId}

GET    /skus/{id}/prices        (PriceLists — giá có thời hạn / theo nhóm KH)
POST   /skus/{id}/prices        { price, validFrom, validTo, customerGroup? }
DELETE /skus/{id}/prices/{priceId}

━━━ CATEGORIES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /categories              (cây danh mục)
POST   /categories
PUT    /categories/{id}
DELETE /categories/{id}

━━━ INVENTORY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /inventory               ?skuId=&categoryId=  (StockEntries — tồn kho hiện tại)
GET    /inventory/alerts        (hàng sắp hết / hết hạn — từ StockEntries + StockBatches)
POST   /inventory/dispose       { skuId, qty, note }  (xuất hủy — StockTransaction type=Dispose)

GET    /inventory/batches       ?skuId=&expiryBefore=  (StockBatches — theo dõi lô / hạn dùng)

GET    /stock-in-vouchers       ?supplierId=&status=&from=&to=  (StockInVouchers)
POST   /stock-in-vouchers       { supplierId, items: [{ skuId, qty, unitPrice }], note }
GET    /stock-in-vouchers/{id}
PUT    /stock-in-vouchers/{id}/status  { status }  (Draft → Completed / Cancelled)

GET    /stock-takes
POST   /stock-takes             (tạo phiếu kiểm kê)
GET    /stock-takes/{id}
PUT    /stock-takes/{id}/items  { items: [{ skuId, actualQty, note }] }
POST   /stock-takes/{id}/approve

GET    /suppliers
POST   /suppliers
GET    /suppliers/{id}
PUT    /suppliers/{id}
DELETE /suppliers/{id}          (soft-delete: is_active = 0)

GET    /suppliers/{id}/payments               (SupplierPayments — công nợ NCC)
POST   /suppliers/{id}/payments  { voucherId?, amount, method, note }
GET    /suppliers/{id}/stock-in-history       (lịch sử phiếu nhập theo NCC)

━━━ ORDERS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
POST   /orders                  { shiftId, customerId? }
GET    /orders/{id}
GET    /orders                  ?storeId=&shiftId=&status=&from=&to=&page=&size=
POST   /orders/{id}/items       { skuId, qty } → trả về giỏ + KM đã áp
DELETE /orders/{id}/items/{itemId}
POST   /orders/{id}/voucher     { code }
DELETE /orders/{id}/voucher
POST   /orders/{id}/checkout    { payments: [{ method, amount, transactionRef? }],
                                   customerId? }  ← hỗ trợ split payment (nhiều
                                   phương thức); order.status=Paid chỉ khi tổng
                                   các payment thành công = grand_total
POST   /orders/{id}/cancel      (cần quyền Manager)

━━━ PAYMENTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
POST   /payments/qr/generate    { orderId, method(MoMo/VietQR) }
GET    /payments/{id}/status    (polling)
POST   /payments/webhook/momo   (MoMo callback — public, verify signature)
POST   /payments/webhook/vietqr

━━━ INVOICES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /invoices                ?orderId=&from=&to=
GET    /invoices/{id}
GET    /invoices/{id}/pdf       (xuất PDF để in/gửi email)

━━━ CUSTOMERS & LOYALTY ━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /customers               ?phone=&name=
POST   /customers
GET    /customers/{id}
PUT    /customers/{id}
GET    /customers/{id}/loyalty              (LoyaltyAccount — điểm hiện tại)
GET    /customers/{id}/loyalty/transactions ?from=&to=&type=  (PointTransactions)
POST   /customers/{id}/loyalty/adjust       { points, note }  (type=Adjust — Manager)

GET    /member-tiers            (MemberTiers — danh sách hạng thành viên toàn chuỗi)
PUT    /member-tiers/{id}       { minSpending, pointRate, discountRate, displayColor }

━━━ CHATBOT (Trợ lý ảo — public, không cần đăng nhập nhân viên) ━━━
POST   /chatbot/sessions        { storeId }  → { sessionId }
                                  (khởi tạo phiên chat mới cho khách ẩn danh)
POST   /chatbot/message         { sessionId, message }
                                  → { reply, matchedFaqIds[] }
                                  Luồng: FaqSearchService tìm FaqEntry liên
                                  quan → IChatbotAiProvider sinh câu trả lời
                                  dựa trên các FaqEntry đó → lưu ChatMessage
                                  (Customer + Bot) → giới hạn số tin/phiên
GET    /chatbot/sessions/{sessionId}/history

━━━ FAQ (quản lý bởi Admin) ━━━━━━━━━━━━━━━━━━━━━━
GET    /faqs                    ?storeId=&category=&isActive=
POST   /faqs                    { category, question, answer, keywords, storeId? }
PUT    /faqs/{id}
DELETE /faqs/{id}
PUT    /faqs/{id}/toggle        { isActive }

━━━ PROMOTIONS & VOUCHERS ━━━━━━━━━━━━━━━━━━━━━━━
GET    /promotions              ?storeId=&status=
POST   /promotions
GET    /promotions/{id}
PUT    /promotions/{id}
DELETE /promotions/{id}

GET    /promotions/{id}/vouchers              (Vouchers gắn với promotion)
POST   /promotions/{id}/vouchers  { code, maxUses, perCustomerLimit, expiresAt }

GET    /vouchers                ?code=&isActive=  (tìm / list voucher)
GET    /vouchers/{id}
PUT    /vouchers/{id}           { maxUses, perCustomerLimit, expiresAt, isActive }
DELETE /vouchers/{id}
GET    /vouchers/{code}/validate               ⭐ Kiểm tra voucher hợp lệ trước checkout

━━━ SHIFTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
POST   /shifts/open             { openingCash }
GET    /shifts/current
GET    /shifts                  ?storeId=&status=&from=&to=  (danh sách ca)
GET    /shifts/{id}
POST   /shifts/{id}/close       { actualCash, note }
GET    /shifts/{id}/summary

━━━ REPORTS & DASHBOARD ━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /reports/dashboard       ?storeId=  (Owner: bỏ trống storeId để xem
                                  tổng hợp toàn chuỗi) → doanh thu hôm nay,
                                  số đơn, giá trị đơn TB, biểu đồ 7-30 ngày,
                                  top 5 SP bán chạy, cảnh báo nhanh (tồn kho/
                                  kiểm kê chờ duyệt)
GET    /reports/revenue         ?from=&to=&storeId=&employeeId=
GET    /reports/top-selling     ?from=&to=&limit=
GET    /reports/inventory       ?storeId=&categoryId=
GET    /reports/slow-moving     ?days=30
GET    /reports/shift/{id}
POST   /reports/export          { type, format(xlsx/pdf), params }
GET    /reports/export/{jobId}  (download khi xong)

━━━ CONFIG ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GET    /config/store
PUT    /config/store
GET    /config/i18n/{lang}      (en/vi/...)
PUT    /config/i18n/{lang}      (Admin thêm key mới)
```

---

## 6. ⚡ LUỒNG XỬ LÝ NGHIỆP VỤ

### 6.1 Luồng Bán Hàng Chính

```
[Thu ngân]
    │
    ├─ 1. Mở ca
    │       POST /shifts/open { openingCash: 500000 }
    │
    ├─ 2. Tạo đơn mới
    │       POST /orders { shiftId }
    │       → Nhận orderId
    │
    ├─ 3. Tìm khách hàng (tuỳ chọn)
    │       GET /customers?phone=0901234567
    │       POST /orders/{id}/customer { customerId }
    │
    ├─ 4. Quét barcode sản phẩm (lặp đi lặp lại)
    │       → BarcodeService bắt input từ scanner
    │       GET /skus/barcode/8934567890123
    │       POST /orders/{id}/items { skuId, qty: 1 }
    │       ← Server chạy PromotionEngine → trả về giỏ hàng + KM đã áp
    │       → WinForms cập nhật UI giỏ hàng real-time
    │
    ├─ 5. Áp voucher (tuỳ chọn)
    │       POST /orders/{id}/voucher { code: "SUMMER20" }
    │
    ├─ 6. Thanh toán (1 hoặc nhiều phương thức — split payment)
    │   ├─ TIỀN MẶT:
    │   │       POST /orders/{id}/checkout
    │   │             { payments: [{ method: "Cash", amount: 200000 }] }
    │   │       ← { change: 50000, orderId, receiptData }
    │   │       → PrinterService in hóa đơn nhiệt
    │   │
    │   ├─ QR (MoMo/VietQR):
    │   │       POST /payments/qr/generate { orderId, method: "MoMo" }
    │   │       ← { qrCode, paymentId }
    │   │       → WinForms hiển thị QR (QRDisplayService)
    │   │       → SignalRClientService lắng nghe "PaymentResult/{paymentId}"
    │   │       [Khách quét QR trên điện thoại]
    │   │       → MoMo gọi POST /payments/webhook/momo
    │   │       → Server verify signature → Update DB → SignalR push
    │   │       ← SignalR event "PaymentSuccess"
    │   │       → WinForms nhận → Complete order → In hóa đơn
    │   │
    │   └─ NHIỀU PHƯƠNG THỨC (split payment, vd Cash + Card):
    │           POST /orders/{id}/checkout
    │                { payments: [{ method: "Cash", amount: 200000 },
    │                              { method: "Card", amount: 150000,
    │                                transactionRef: "..." }] }
    │           → Server tạo nhiều dòng Payment, xử lý độc lập từng dòng
    │           → order.status chuyển "Paid" chỉ khi tổng các payment
    │             thành công = grand_total; nếu 1 dòng thất bại, đơn giữ
    │             trạng thái chờ để thu ngân bù bằng phương thức khác
    │
    └─ 7. Đóng ca
            POST /shifts/{id}/close { actualCash: 1200000, note: "..." }
            ← { summary: { cash, momo, card, total, diff } }
            → In biên bản đóng ca
```

### 6.2 Promotion Engine Flow

```
POST /orders/{id}/items { skuId, qty }
    │
    ▼
PromotionService.Evaluate(order, newItem)     ← Application layer: điều phối
    │
    ├── Lấy promotions đang active từ DB (cache Redis 5 phút)
    │
    ▼
PromotionEngine.Evaluate(cart, activeRules, now)  ← Domain layer: logic thuần, không đụng DB/Redis
    │
    ├── Filter eligible promotions:
    │   ├── Theo thời gian (valid_from <= now <= valid_to)
    │   ├── Theo SKU / Category
    │   ├── Theo điều kiện giỏ hàng (min_order_amount)
    │   ├── Theo khung giờ (HappyHour)
    │   └── Theo hạng thành viên
    │
    ├── Sort theo Priority (cao → thấp)
    │
    ├── Apply với Stacking Rules:
    │   foreach promotion (theo priority):
    │       if is_exclusive → apply 1 cái duy nhất, stop
    │       if is_stackable → apply, tiếp tục
    │       if !is_stackable → skip nếu đã có exclusive khác
    │
    ├── Tính toán discount_amount cho từng item / toàn đơn
    │
    └── Trả về PromotionResult
        │
        ▼
PromotionService nhận kết quả → build OrderDto:
    ├── items (với discount_amount từng dòng)
    ├── order_discounts (danh sách KM đã áp)
    ├── subtotal, discount_total, tax_total, grand_total
    └── applied_promotions (để hiển thị cho thu ngân)
```

> Vì `PromotionEngine` không phụ thuộc DB/Redis/HTTP, toàn bộ rule stacking/exclusive/priority có thể viết unit test chạy hàng trăm case trong vài giây, không cần mock gì.

### 6.3 Inventory Flow sau Checkout

```
CheckoutOrderCommand thành công
    │
    ├── Foreach OrderItem:
    │   StockTransaction.Insert(type: SaleOut, qty: -item.qty, ref: orderId)
    │   StockEntry.qty_on_hand -= item.qty
    │
    ├── Check min_stock cảnh báo:
    │   if qty_on_hand <= min_stock:
    │       → Push SignalR alert đến màn hình quản lý
    │       → Ghi vào bảng StockAlerts
    │
    ├── Nếu có Customer + Loyalty:
    │   points_earned = floor(grand_total * point_rate / 100)
    │   LoyaltyAccount.points_balance += points_earned
    │   PointTransaction.Insert(type: Earn, points: +points_earned)
    │
    └── AuditLog.Insert(action: OrderCheckout, entity: Order, id: orderId)
```

---

## 7. 🛠️ TECHNOLOGY STACK CHÍNH THỨC

### Backend

| Hạng mục            | Công nghệ                | NuGet Package                                      |
| ------------------- | ------------------------ | -------------------------------------------------- |
| **Runtime**         | .NET 10                  | —                                                  |
| **Web Framework**   | ASP.NET Core Web API     | —                                                  |
| **ORM**             | Entity Framework Core 8  | `Npgsql.EntityFrameworkCore.SQLServer`             |
| **Validation**      | FluentValidation         | `FluentValidation.AspNetCore`                      |
| **Auth**            | JWT Bearer               | `Microsoft.AspNetCore.Authentication.JwtBearer`    |
| **API Docs**        | Scalar (mới hơn Swagger) | `Scalar.AspNetCore`                                |
| **Mapping**         | Mapster                  | `Mapster`                                          |
| **Cache**           | Redis                    | `StackExchange.Redis`                              |
| **Real-time**       | SignalR                  | `Microsoft.AspNetCore.SignalR`                     |
| **Background Jobs** | Hangfire                 | `Hangfire.SQLServer`                               |
| **Logging**         | Serilog + Grafana Loki   | `Serilog.AspNetCore`, `Serilog.Sinks.Grafana.Loki` |
| **Excel**           | ClosedXML                | `ClosedXML`                                        |
| **PDF**             | QuestPDF                 | `QuestPDF`                                         |
| **Hash Password**   | BCrypt                   | `BCrypt.Net-Next`                                  |
| **HTTP Client**     | Refit                    | `Refit` (gọi MoMo/VietQR API)                      |

### WinForms Client

| Hạng mục           | Công nghệ            | NuGet Package                              |
| ------------------ | -------------------- | ------------------------------------------ |
| **UI Library**     | MaterialSkin 2       | `MaterialSkin.2`                           |
| **HTTP**           | HttpClient + Polly   | `Microsoft.Extensions.Http.Polly`          |
| **SignalR Client** | SignalR Client       | `Microsoft.AspNetCore.SignalR.Client`      |
| **Local DB**       | SQLite               | `sqlite-net-pcl`                           |
| **Print bill**     | ESCPOS.NET           | `ESCPOS.NET`                               |
| **QR Generate**    | ZXing.Net            | `ZXing.Net.Bindings.Windows.Compatibility` |
| **Report Print**   | FastReport           | `FastReport.OpenSource`                    |
| **DI**             | MS Extensions DI     | `Microsoft.Extensions.DependencyInjection` |
| **Config**         | MS Extensions Config | `Microsoft.Extensions.Configuration.Json`  |
| **JSON**           | System.Text.Json     | built-in                                   |

### Infrastructure

| Hạng mục                 | Công nghệ                                  |
| ------------------------ | ------------------------------------------ |
| **Database**             | SQL Server 2022                            |
| **Cache**                | Redis 7 (Docker)                           |
| **Cloud**                | VPS Ubuntu / Azure App Service             |
| **Reverse Proxy**        | Nginx + SSL (Let's Encrypt/Certbot)        |
| **Container**            | Docker + Docker Compose (dev & production) |
| **CI/CD**                | GitHub Actions                             |
| **Monitoring / Logging** | Grafana + Loki + Prometheus                |
| **File Storage**         | MinIO self-host / Azure Blob               |

---

## 8. 🔐 CHIẾN LƯỢC AUTH & PHÂN QUYỀN (CHUẨN THỰC TẾ)

### 8.1 Đăng Nhập 2 Chế Độ (Username/Password & PIN 6 số)

```
┌─────────────────────────────────────────────────────────────┐
│                   MÀN HÌNH ĐĂNG NHẬP (POS)                  │
│                                                             │
│  [ TAB 1: USERNAME / PASSWORD ]   [ TAB 2: MÃ PIN 6 SỐ ]    │
│  Tên đăng nhập: [_____________]     [ 1 ][ 2 ][ 3 ]         │
│  Mật khẩu:      [_____________]     [ 4 ][ 5 ][ 6 ]         │
│  [    ĐĂNG NHẬP VĂN PHÒNG   __]     [ 7 ][ 8 ][ 9 ]         │
│                                       [ 0 ] [ ⌫ ]          │
│                                   [ ĐĂNG NHẬP QUẦY NHANH ]  │
└─────────────────────────────────────────────────────────────┘
```

| Hình thức               | Dành cho                | Mục đích & Đặc điểm                                                        |
| ----------------------- | ----------------------- | -------------------------------------------------------------------------- |
| **Username + Password** | Admin, Manager          | Đăng nhập đầu ngày, truy cập báo cáo, cấu hình. Bảo mật cao.               |
| **PIN 6 số**            | Thu ngân, Nhân viên kho | Đăng nhập nhanh tại quầy khi đổi ca. Nhập cực nhanh trên màn hình cảm ứng. |

### 8.2 Quản Lý Token & Session

- **Access Token (JWT)**: Hết hạn sau **8 tiếng** (vừa đủ 1 ca làm việc). Payload chứa: `employee_id`, `role`, `store_id`, `shift_id`.
- **Refresh Token**: Hết hạn sau **30 ngày**, lưu trong SQL Server. Cho phép thu hồi (revoke) ngay lập tức để buộc nhân viên đăng xuất.
- **Tự động gia hạn**: App WinForms tự động gọi API refresh token ngầm khi Access Token sắp hết hạn.

### 8.3 An Toàn & Bảo Mật (Security Checklist)

1. **Mã hóa**: Mật khẩu và mã PIN luôn được mã hóa dạng hash bằng **BCrypt**.
2. **Khóa tài khoản (Rate Limiting)**: Nhập sai sai quá 5 lần (Password hoặc PIN) → Khóa tài khoản 15 phút.
3. **Bắt buộc HTTPS**: Tất cả giao tiếp API mã hóa TLS/SSL.
4. **Revoke Token khi Đóng ca**: Khi nhân viên bấm "Đóng ca", Token bị thu hồi trên Server.
5. **Audit Log**: Ghi lại lịch sử đăng nhập/đăng xuất (thời gian, IP, thiết bị).

### 8.4 Permission Matrix (RBAC)

| Quyền                                 | Owner | Admin | Manager | Cashier |
| ------------------------------------- | :---: | :---: | :-----: | :-----: |
| Bán hàng / Checkout                   |  ✅   |  ✅   |   ✅    |   ✅    |
| Hủy đơn hàng                          |  ✅   |  ✅   |   ✅    |   ❌    |
| Hoàn tiền                             |  ✅   |  ✅   |   ✅    |   ❌    |
| Override giá                          |  ✅   |  ✅   |   ✅    |   ❌    |
| Nhập kho                              |  ✅   |  ✅   |   ✅    |   ❌    |
| Duyệt kiểm kê                         |  ✅   |  ✅   |   ✅    |   ❌    |
| Chuyển hàng giữa chi nhánh            |  ✅   |  ✅   |   ✅    |   ❌    |
| Xử lý chăm sóc khách hàng (CRM)       |  ✅   |  ✅   |   ✅    |   ✅    |
| Quản lý sản phẩm                      |  ✅   |  ✅   |   ❌    |   ❌    |
| Cấu hình KM                           |  ✅   |  ✅   |   ❌    |   ❌    |
| Xem báo cáo toàn chuỗi (đa chi nhánh) |  ✅   |  ❌   |   ❌    |   ❌    |
| Xem báo cáo chi nhánh                 |  ✅   |  ✅   |   ✅    |   ❌    |
| Quản lý nhân viên                     |  ✅   |  ✅   |   ❌    |   ❌    |
| Tạo cửa hàng mới / gán Admin          |  ✅   |  ❌   |   ❌    |   ❌    |
| Cấu hình hệ thống (chi nhánh)         |  ✅   |  ✅   |   ❌    |   ❌    |
| Cấu hình chung toàn chuỗi             |  ✅   |  ❌   |   ❌    |   ❌    |

---

## 9. 📊 LOGGING, MONITORING & TRIỂN KHAI DOCKER

### 9.1 Mô Hình Thu Thập & Hiển Thị Log

```
ASP.NET Core API ──► Serilog ──► Grafana Loki  ──┐
                                                 ├──► Grafana Dashboard (:3000)
ASP.NET Core API ──► /metrics ──► Prometheus    ──┘
```

### 9.2 Tích Hợp Serilog Trong ASP.NET Core API

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "POS.API")
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        uri: "http://loki:3100",
        labels: new[] { new LokiLabel { Key = "app", Value = "pos-api" } })
    .CreateLogger();

builder.Host.UseSerilog();
```

### 9.3 Khả Năng Của Grafana UI (:3000)

1. **Truy vấn Log linh hoạt (Log Explorer)**:
   - Lọc theo cửa hàng: `{app="pos-api"} | json | StoreId="store_01"`
   - Lọc tất cả lỗi: `{app="pos-api"} |= "ERROR"`
   - Tra cứu sự cố đơn hàng: `{app="pos-api"} |= "OrderId_12345"`
2. **Dashboard Real-time**: Doanh thu trong ngày, số đơn hàng/phút, tốc độ phản hồi API (Latency p50/p95).
3. **Cảnh báo Tự Động (Alerting Rules)**: Gửi Telegram/Email khi API error rate > 1%, webhook lỗi hoặc tồn kho MinStock.

### 9.4 Docker Compose — Môi Trường Dev

Chạy `docker compose up -d --build` trong thư mục `deploy/` là có đủ Postgres + Redis + API + Loki + Prometheus + Grafana, port mở trực tiếp ra `localhost` để dev test nhanh (API `:5000`, Postgres `:5432`, Grafana `:3000`, Prometheus `:9090`). Migration EF Core chạy tự động qua container `pos-api-migrator` (chạy 1 lần rồi thoát) trước khi `pos-api` nhận traffic, đảm bảo schema luôn khớp code khi khởi động.

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${MSSQL_SA_PASSWORD}"
      MSSQL_PID: "Developer"
    ports:
      - "14330:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    healthcheck:
      test:
        [
          "CMD-SHELL",
          '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT 1" -C || /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT 1" || exit 1',
        ]
      interval: 10s
      timeout: 5s
      retries: 10

  redis:
    image: redis:7-alpine
    command: ["redis-server", "--requirepass", "${REDIS_PASSWORD}"]
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data

  pos-api-migrator:
    build:
      context: ..
      dockerfile: deploy/Dockerfile
      target: build
    command:
      [
        "dotnet",
        "ef",
        "database",
        "update",
        "--project",
        "src/POS.Infrastructure",
        "--startup-project",
        "src/POS.Api",
      ]
    environment:
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
    depends_on:
      postgres:
        condition: service_healthy

  pos-api:
    build:
      context: ..
      dockerfile: deploy/Dockerfile
    ports:
      - "5000:8080"
    environment:
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Redis__ConnectionString: "redis:6379,password=${REDIS_PASSWORD}"
      Jwt__Secret: ${JWT_SECRET}
      Serilog__LokiUrl: "http://loki:3100"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      pos-api-migrator:
        condition: service_completed_successfully

  loki:
    image: grafana/loki:2.9.8
    ports: ["3100:3100"]
    volumes: [loki-data:/loki]

  prometheus:
    image: prom/prometheus:v2.54.1
    ports: ["9090:9090"]
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus

  grafana:
    image: grafana/grafana:11.2.0
    ports: ["3000:3000"]
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_ADMIN_PASSWORD}
    volumes:
      - grafana-data:/var/lib/grafana
    depends_on: [loki, prometheus]

volumes:
  postgres-data:
  redis-data:
  loki-data:
  prometheus-data:
  grafana-data:
```

### 9.5 Docker Compose — Production (override)

Chạy production bằng `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build`. File override chỉ thêm/ghi đè phần khác biệt so với dev: đóng toàn bộ port expose trực tiếp (Postgres, Redis, Prometheus, Grafana không lộ ra internet), thêm Nginx làm reverse proxy + SSL, thêm `restart: always` và giới hạn tài nguyên cho `pos-api`.

```yaml
services:
  sqlserver:
    ports: !reset []
    restart: always

  redis:
    ports: !reset []
    restart: always

  pos-api:
    ports: !reset []
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    restart: always
    deploy:
      resources:
        limits:
          cpus: "1.0"
          memory: 512M

  prometheus:
    ports: !reset []
    restart: always

  grafana:
    ports: !reset []
    restart: always

  nginx:
    image: nginx:1.27-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - certbot-www:/var/www/certbot:ro
      - certbot-conf:/etc/letsencrypt:ro
    depends_on: [pos-api, grafana]
    restart: always

  certbot:
    image: certbot/certbot:latest
    volumes:
      - certbot-www:/var/www/certbot
      - certbot-conf:/etc/letsencrypt
    entrypoint: "sh -c 'trap exit TERM; while :; do certbot renew; sleep 12h & wait $${!}; done;'"

volumes:
  certbot-www:
  certbot-conf:
```

Nginx đảm nhận: redirect HTTP → HTTPS, terminate SSL (Let's Encrypt qua Certbot), forward `/api/` vào `pos-api`, cấu hình riêng cho `/hubs/` (WebSocket của SignalR), rate-limit riêng cho `/api/v1/auth/` để chống brute-force PIN/password, và expose Grafana qua path `/grafana/` thay vì public thẳng cổng `:3000`.

### 9.6 Dockerfile (multi-stage, cho POS.API)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/POS.Api/POS.Api.csproj", "src/POS.Api/"]
COPY ["src/POS.Application/POS.Application.csproj", "src/POS.Application/"]
COPY ["src/POS.Domain/POS.Domain.csproj", "src/POS.Domain/"]
COPY ["src/POS.Infrastructure/POS.Infrastructure.csproj", "src/POS.Infrastructure/"]
RUN dotnet restore "src/POS.Api/POS.Api.csproj"
COPY . .
WORKDIR "/src/src/POS.Api"
RUN dotnet build "POS.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "POS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN adduser --disabled-password --gecos "" appuser
USER appuser
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
EXPOSE 8080
ENTRYPOINT ["dotnet", "POS.Api.dll"]
```

> Toàn bộ file Docker thật (`Dockerfile`, `docker-compose.yml`, `docker-compose.prod.yml`, `nginx.conf`, `prometheus.yml`, `.env.example`, `README.md` hướng dẫn chạy) nằm trong thư mục `deploy/` của solution — chỉ cần `cp .env.example .env`, điền secret, rồi `docker compose up -d --build` là chạy được ngay.

---

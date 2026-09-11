# HƯỚNG DẪN THIẾT KẾ FEATURE END-TO-END (CLEAN ARCHITECTURE & DDD) - POS SYSTEM

Tài liệu này chuẩn hóa toàn bộ quy trình phân tích, thiết kế và hiện thực một tính năng (**Feature End-to-End**) trong dự án **POS-System**, dựa trên kiến trúc **Clean Architecture**, **Domain-Driven Design (DDD)**, **CQRS (MediatR)**, **FluentValidation** và **Result Pattern**.

---

## 1. CHI TIẾT CÁC BƯỚC THIẾT KẾ 1 FEATURE

Khi bắt đầu một feature mới (ví dụ `CreateStore`), bạn sẽ triển khai qua **5 tầng chính** theo thứ tự từ trong ra ngoài (hoặc theo chiều luồng Request):

```
1. POS.Contracts     -> Định nghĩa DTO Request / Response công khai cho Client
2. POS.Api           -> Controller tiếp nhận, Mapping DTO <-> Command/Query, trả HTTP Response
3. POS.Application   -> Command/Query, Validator, Handler, Interface Repository, UnitOfWork
4. POS.Domain        -> Entity (Rich Model), Value Objects, Domain Rules/Invariants, Errors
5. POS.Infrastructure-> EF Core EntityConfiguration, Repository Implementation, AppDbContext, DI
```

---

### BƯỚC 1: TẦNG CONTRACTS (`POS.Contracts`)

#### 1. Mục đích & Nguyên tắc:

- Chứa các **Data Transfer Object (DTO)** thuần túy dùng để giao tiếp với Client bên ngoài qua HTTP API.
- Phải là **`record`** bất biến (immutable), không chứa logic nghiệp vụ, không tham chiếu tới Domain hay EF Core.
- Được tổ chức theo phiên bản API (ví dụ: `V1/Stores`).

#### 2. File cần tạo / chỉnh sửa:

- `src/POS.Contracts/V1/{Entities}/{Entity}Requests.cs`
- `src/POS.Contracts/V1/{Entities}/{Entity}Responses.cs`

#### 3. Code minh họa:

**`StoreRequests.cs`**:

```csharp
namespace POS.Contracts.V1.Stores;

public record CreateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND"
);
```

**`StoreResponses.cs`**:

```csharp
namespace POS.Contracts.V1.Stores;

public record StoreDetailResponse(
    Guid Id,
    string Name,
    string? Address,
    bool IsActive,
    string? Phone = null,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND",
    string? TaxCode = null,
    string? ReceiptHeader = null,
    string? ReceiptFooter = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null
);
```

---

### BƯỚC 2: TẦNG API (`POS.Api`)

#### 1. Mục đích & Nguyên tắc:

- Đóng vai trò là cổng giao tiếp (Entry Point) của ứng dụng.
- **Không chứa business logic**.
- Sử dụng **MediatR (`ISender`)** để dispatch Command/Query xuống tầng Application.
- Nhận `Result<T>` từ Application và sử dụng Extension method `ToActionResult()` để map thành HTTP Status Code tương ứng (200, 201, 400, 404, 409,...).
- Bọc dữ liệu trả về bằng chuẩn chung `ApiResponse<T>`.

#### 2. File cần tạo / chỉnh sửa:

- `src/POS.Api/Controllers/{Entities}Controller.cs`
- `src/POS.Api/Mapping/{Entity}Mapping.cs`

#### 3. Code minh họa:

**`StoresController.cs`**:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Stores.Commands.CreateStore;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Stores;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/v1/stores")]
public class StoresController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> Create(
        [FromBody] CreateStoreRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Mapping từ Request Contract sang Application Command
        var command = new CreateStoreCommand(
            request.Name,
            request.Address,
            request.Phone,
            request.Timezone,
            request.CurrencyCode);

        // 2. Gửi Command qua MediatR
        var result = await mediator.Send(command, cancellationToken);

        // 3. Xử lý thất bại (Validation error, Domain error...)
        if (result.IsFailure)
            return this.ToActionResult(result);

        // 4. Trả về 201 Created cùng kết quả chuẩn hóa
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<StoreDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStoreDetailQuery(id), cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<StoreDetailResponse>.Ok(result.Value!.ToResponse()));
    }
}
```

**`StoreMapping.cs`**:

```csharp
using POS.Application.UseCases.Stores.Commands.CreateStore;
using POS.Contracts.V1.Stores;

namespace POS.Api.Mappings;

public static class StoreMapping
{
    public static StoreDetailResponse ToResponse(this CreateStoreDto store)
    {
        return new StoreDetailResponse(
            store.Id,
            store.Name,
            store.Address,
            store.IsActive,
            store.Phone,
            store.Timezone,
            store.CurrencyCode);
    }
}
```

---

### BƯỚC 3: TẦNG APPLICATION (`POS.Application`)

#### 1. Mục đích & Nguyên tắc:

- Là nơi điều phối luồng nghiệp vụ (Orchestration).
- Triển khai mô hình **CQRS**:
  - **Command**: Thực hiện ghi/thay đổi dữ liệu (implements `ICommand<TResponse>` hoặc `ICommand`).
  - **Query**: Thực hiện đọc dữ liệu (implements `IQuery<TResponse>`).
- **Validation**:
  - Tách biệt kiểm tra định dạng dữ liệu (Format/Length/Required) bằng **FluentValidation**.
  - `ValidationBehavior` trong MediatR Pipeline sẽ tự động bắt lỗi và trả về `ValidationResult` mà không cần try-catch hay throw Exception.
- **Result Pattern**:
  - Handler trả về `Result<T>` thay vì throw exception giúp tối ưu hiệu năng và code sạch, dễ test.

#### 2. File cần tạo / chỉnh sửa:

- `POS.Application/UseCases/{Entity}/Commands/Create{Entity}/Create{Entity}Command.cs`
- `POS.Application/UseCases/{Entity}/Commands/Create{Entity}/Create{Entity}CommandValidator.cs`
- `POS.Application/UseCases/{Entity}/Commands/Create{Entity}/Create{Entity}CommandHandler.cs`
- `POS.Application/Abstractions/Persistence/I{Entity}Repository.cs`

#### 3. Code minh họa:

**`CreateStoreCommand.cs`**:

```csharp
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Stores.Commands.CreateStore;

public record CreateStoreCommand(
    string Name,
    string? Address,
    string? Phone,
    string Timezone,
    string CurrencyCode
) : ICommand<CreateStoreDto>;

public record CreateStoreDto(
    Guid Id,
    string Name,
    string? Address,
    bool IsActive,
    string? Phone = null,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND"
);
```

**`CreateStoreCommandValidator.cs`**:

```csharp
using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.CreateStore;

public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên cửa hàng không được để trống.")
            .MaximumLength(200).WithMessage("Tên cửa hàng tối đa 200 ký tự.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Đơn vị tiền tệ không được để trống.")
            .MaximumLength(10).WithMessage("Mã tiền tệ tối đa 10 ký tự.");
    }
}
```

**`CreateStoreCommandHandler.cs`**:

```csharp
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Stores.Commands.CreateStore;

public class CreateStoreCommandHandler : ICommandHandler<CreateStoreCommand, CreateStoreDto>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStoreCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
    {
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateStoreDto>> Handle(
        CreateStoreCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Tạo Domain Entity (Khởi tạo Rich Domain Model)
        var store = new Store(
            command.Name,
            command.Address,
            command.Phone,
            command.Timezone,
            command.CurrencyCode,
            isActive: true);

        // 2. Thêm vào Repository
        await _storeRepository.AddAsync(store, cancellationToken);

        // 3. Commit Transaction thông qua UnitOfWork
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Trả về DTO kết quả bọc trong Result.Success
        return new CreateStoreDto(
            store.Id,
            store.Name,
            store.Address,
            store.IsActive,
            store.Phone,
            store.Timezone,
            store.CurrencyCode);
    }
}
```

**`IStoreRepository.cs`** (Nằm trong `POS.Application/Abstractions/Persistence/`):

```csharp
using POS.Domain.Stores;

namespace POS.Application.Abstractions.Persistence;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Store>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Store store, CancellationToken cancellationToken = default);
}
```

---

### BƯỚC 4: TẦNG DOMAIN (`POS.Domain`)

#### 1. Mục đích & Nguyên tắc thiết kế Domain chuẩn DDD:

- **Trọng tâm của hệ thống**: Chứa toàn bộ Enterprise Business Rules và Core Logic.
- **Độc lập tuyệt đối**: Không phụ thuộc vào bất kỳ framework bên ngoài nào (kể cả EF Core, ASP.NET Core).
- **Rich Domain Model (Mô hình nghiệp vụ giàu tính đóng gói)**:
  - Tất cả các thuộc tính phải có `private set` để ngăn chặn việc sửa đổi trạng thái tùy tiện từ bên ngoài.
  - Sử dụng **Constructor** để đảm bảo Entity luôn ở trạng thái hợp lệ khi được tạo.
  - Các hành vi thay đổi trạng thái (Domain Behaviors) phải được thực hiện thông qua các phương thức nghiệp vụ rõ ràng (ví dụ: `UpdateInfo()`, `Deactivate()`, `AssignEmployee()`).
- **Domain Invariants & Exception/Error Handling**:
  - Các ràng buộc bất biến kỹ thuật bắt buộc (ví dụ: null string kiểm tra tham số) có thể dùng `ArgumentException` / `ArgumentNullException`.
  - Các vi phạm quy tắc nghiệp vụ (ví dụ: số dư không đủ, mã trùng, trạng thái không hợp lệ) trả về `Error` với các `ErrorType` cụ thể (`ErrorType.Validation`, `ErrorType.AlreadyExists`, `ErrorType.NotFound`, `ErrorType.Invalid`).

#### 2. File cần tạo / chỉnh sửa:

- `src/POS.Domain/Stores/{Entity}.cs`
- `src/POS.Domain/Common/BaseEntity.cs` (Entity cơ sở)
- `src/POS.Domain/Common/Error.cs` và `Result.cs`

#### 3. Code minh họa:

**`Store.cs`**:

```csharp
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Domain.Stores;

public class Store : BaseEntity
{
    // Constructor không tham số cho EF Core
    public Store() : base() { }

    // Constructor nghiệp vụ đảm bảo Invariants
    public Store(
        string name,
        string? address = null,
        string? phone = null,
        string? timezone = "Asia/Ho_Chi_Minh",
        string? currencyCode = "VND",
        string? taxCode = null,
        string? receiptHeader = null,
        string? receiptFooter = null,
        bool isActive = true,
        Guid? id = null)
        : base(id)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Store name is required", nameof(name))
            : name;

        Address = address;
        Phone = phone;
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "Asia/Ho_Chi_Minh" : timezone;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode;
        TaxCode = taxCode;
        ReceiptHeader = receiptHeader;
        ReceiptFooter = receiptFooter;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Encapsulated properties (private set)
    public string Name { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string Timezone { get; private set; } = "Asia/Ho_Chi_Minh";
    public string CurrencyCode { get; private set; } = "VND";
    public string? TaxCode { get; private set; }
    public string? ReceiptHeader { get; private set; }
    public string? ReceiptFooter { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation Property
    public ICollection<Employee> Employees { get; private set; } = new List<Employee>();

    // Domain Methods thể hiện hành vi nghiệp vụ
    public void UpdateInfo(string name, string? address, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Store name is required", nameof(name));

        Name = name;
        Address = address;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

### BƯỚC 5: TẦNG INFRASTRUCTURE (`POS.Infrastructure`)

#### 1. Mục đích & Nguyên tắc:

- Hiện thực hóa việc lưu trữ dữ liệu, tương tác cơ sở dữ liệu (SQL Server via EF Core), Cache (Redis), Email, 외부 API.
- **EF Core Configuration**: Tách riêng cấu hình mapping bảng (`IEntityTypeConfiguration<T>`) thay vì để chung trong DbContext để giữ DbContext gọn gàng.
- **Repository Implementation**: Hiện thực các Interface đã khai báo ở tầng Application.
- **Đăng ký DI (Dependency Injection)**: Đăng ký đầy đủ Repository, DbContext, Services vào `IServiceCollection`.

#### 2. File cần tạo / chỉnh sửa:

- `src/POS.Infrastructure/Persistence/Repositories/{Entity}Repository.cs`
- `src/POS.Infrastructure/Persistence/AppDbContext.cs`
- `src/POS.Infrastructure/DependencyInjection.cs`

#### 3. Code minh họa:

**`StoreRepository.cs`**:

```csharp
using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<List<Store>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Stores.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Store store, CancellationToken cancellationToken = default) =>
        await _context.Stores.AddAsync(store, cancellationToken);
}
```

**`DependencyInjection.cs`** (`POS.Infrastructure`):

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Cấu hình DbContext với SQL Server
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // 2. Đăng ký Repositories & UnitOfWork (Scoped)
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

---

## 2. CHECKLIST TỪNG BƯỚC TRIỂN KHAI FEATURE MỚI

Khi bạn được giao thiết kế một Feature mới (ví dụ: `CreateProduct`, `UpdateOrderStatus`, `CreateStockInVoucher`...), hãy làm theo checklist sau:

| STT   | Tầng (Layer)                       | File cần tạo / chỉnh sửa                                                                                                                     | Việc cần làm                                                                                                           |
| :---- | :--------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------- | :--------------------------------------------------------------------------------------------------------------------- |
| **1** | **Domain**                         | `POS.Domain/{Feature}/{Entity}.cs`                                                                                                           | Khởi tạo/cập nhật Entity kế thừa `BaseEntity`, đóng gói thuộc tính `private set`, viết business methods & constructor. |
| **2** | **Contracts**                      | `POS.Contracts/V1/{Feature}/{Entity}Requests.cs`<br/>`{Entity}Responses.cs`                                                                  | Định nghĩa các `record` Request/Response DTO công khai.                                                                |
| **3** | **Application (Abstractions)**     | `POS.Application/Abstractions/Persistence/I{Entity}Repository.cs`                                                                            | Định nghĩa interface thao tác dữ liệu.                                                                                 |
| **4** | **Application (Use Cases)**        | `Commands/{Action}/{Action}Command.cs`<br/>`Commands/{Action}/{Action}CommandValidator.cs`<br/>`Commands/{Action}/{Action}CommandHandler.cs` | Tạo Command/Query, viết FluentValidation rule, viết Handler điều phối nghiệp vụ và gọi repository.                     |
| **5** | **Infrastructure**                 | `Persistence/Configurations/{Entity}Configuration.cs`<br/>`Persistence/Repositories/{Entity}Repository.cs`<br/>`AppDbContext.cs`             | Cấu hình EntityTypeConfiguration, implement Repository, thêm DbSet, đăng ký DI trong `DependencyInjection.cs`.         |
| **6** | **Api**                            | `Controllers/{Entity}Controller.cs`<br/>`Mapping/{Entity}Mapping.cs`                                                                         | Tạo Endpoint nhận Request, mapping sang Command, gọi `mediator.Send()`, trả về `ToActionResult()`.                     |
| **7** | **Migration (Nếu có thay đổi DB)** | Package Manager Console / CLI                                                                                                                | Chạy lệnh `dotnet ef migrations add <FeatureName>`                                                                     |

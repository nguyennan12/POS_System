# Store Management — implementation report

## Kết quả

Đã implement GetAllStoresQuery, UpdateStoreCommand, UpdateStoreStatusCommand,
AssignAdminToStoreCommand và GrantOwnerAccessCommand theo CQRS/MediatR,
FluentValidation, Result Pattern và Repository/UnitOfWork hiện có.

Contract chỉ bổ sung JsonRequired cho UpdateStoreStatusRequest.IsActive: giữ tên trường
và kiểu bool, nhưng thiếu trường bị từ chối thay vì mặc định false.
Không sửa package references, schema hoặc migration.
Không commit. T16 vẫn chưa có implementation trong working tree khi triển khai.

## Endpoint

Base route: /api/v1/stores. Tất cả action yêu cầu authenticated principal.

| Method | Route | Kết quả thành công |
|---|---|---|
| GET | / | 200, ApiResponse<List<StoreResponse>>, có lọc scope |
| POST | / | 201, ApiResponse<StoreDetailResponse>, Location tới GetById |
| GET | /{id} | 200, ApiResponse<StoreDetailResponse>, đủ trường detail |
| PUT | /{id} | 200, ApiResponse<StoreDetailResponse> |
| PUT | /{id}/status | 200, ApiResponse<StoreDetailResponse> |
| POST | /{id}/assign-admin | 200, ApiResponse<StoreDetailResponse> |
| POST | /{id}/grant-owner-access | 200, ApiResponse<StoreDetailResponse> |

Các lỗi nghiệp vụ: 400 invalid/validation, 401 thiếu danh tính hợp lệ,
403 thiếu quyền/scope, 404 entity không tồn tại, 409 duplicate access/PIN.
Scope được kiểm tra trước khi đọc Store theo ID: một ID ngoài scope trả 403
kể cả khi ID đó không tồn tại, tránh tiết lộ sự tồn tại của cửa hàng.

## Business rules

- Caller phải là Employee active, không đang bị khóa, có system role Owner trong DB.
  Không tin role/store claim cũ để quyết định quyền.
- Scope theo tài liệu database: IsChainOwner sử dụng EmployeeStoreAccess;
  employee không phải chain owner sử dụng StoreId chính.
  Role Owner không tự động bỏ qua scope. GetAll lọc ngay trong truy vấn EF,
  bao gồm cửa hàng inactive mà caller có quyền.
- Create cần chain owner; lưu Store và access của chính người tạo trong cùng
  SaveChanges để cửa hàng mới có thể đọc/quản lý ngay.
- Update cập nhật đầy đủ thông tin trong contract và UpdatedAt.
  Phone/Address/TaxCode/ReceiptHeader/ReceiptFooter có thể xóa bằng null.
  Đây là PUT, không phải cập nhật từng trường kiểu PATCH.
- Theo lựa chọn của người dùng: trạng thái được bật/tắt theo IsActive,
  gọi lại cùng trạng thái là no-op. Owner vẫn có thể sửa/mở lại cửa hàng inactive.
  JSON thiếu/sai isActive hoặc gửi null bị model binding trả 400 trước khi dispatch;
  explicit false/true vẫn được chấp nhận.
- Theo lựa chọn của người dùng: assign-admin được nâng system Cashier thành
  system StoreManager và chuyển cửa hàng; không có role Admin mới.
  Không hạ Owner, không đổi IsChainOwner, không chuyển custom role chưa có rule.
- Caller phải có scope ở cả cửa hàng nguồn và đích khi chuyển nhân viên.
  Employee phải active, không đang bị khóa; cửa hàng đích phải active.
  Từ chối chuyển khi có ca Open ở cửa hàng khác hoặc PIN lookup trùng tại đích.
  Từ chối chuyển nếu employee thiếu lookup hash hoặc còn employee ở đích thiếu
  lookup hash (Employee.PinLookupMissing, HTTP 400). T12/T20 chưa implement:
  không suy ngược từ BCrypt, không thay hash hoặc tự tạo hệ thống PIN mới.
  Nâng role trong cùng cửa hàng không đổi phạm vi PIN nên giữ behavior cũ.
- Phải tìm được đúng một global system StoreManager role; thiếu hoặc trùng role
  trả lỗi nghiệp vụ. StoreId và RoleId được cập nhật trong cùng lần SaveChanges.
- Không tự thay thế manager khác: schema cho phép nhiều employee là manager.
- Grant chỉ nhận Employee đã có system Owner role và IsChainOwner=true, active,
  không đang bị khóa; cửa hàng phải active và nằm trong scope caller.
  Không tự nâng role hoặc bật IsChainOwner cho recipient.
- GrantedBy lấy từ Employee đã xác thực, không lấy từ request.
  Duplicate access trả 409; bắt cả unique-constraint violation do request đồng thời.
- SaveChanges của EF bảo đảm ghi atomic. Unique PIN violation trong assignment
  cũng được dịch thành 409 mà không để tầng Application phụ thuộc Npgsql.
- Sau assignment xóa cache perm:{employee_id} theo convention T16 đã mô tả.
  Nếu xóa cache thất bại sau commit, ghi warning và giữ kết quả thành công.
  Scope Store trong feature này luôn đọc từ DB, không có cache scope mới.
- Validator kiểm tra GUID, trường bắt buộc, Name 200, Address 500, Phone 20,
  Timezone 50 và ID timezone hợp lệ, CurrencyCode đúng 3 chữ cái in hoa,
  TaxCode 20. ReceiptHeader/Footer là text không có max length trong schema.

## Authorization và giới hạn

- Chỉ bổ sung adapter CurrentUser cho ICurrentUser có sẵn, đọc employee_id
  từ principal đã qua JWT authentication của Program.cs.
- StoreManagementAccess là helper nội bộ của use case Store cho điều kiện
  Owner/scope; không đăng ký authorization framework, policy engine hoặc
  MediatR AuthorizationBehavior mới.
- T16 vẫn MISSING: chưa có permission động, cache miss/TTL pipeline và thứ tự
  authorization trước validation theo thiết kế chung. Vì vậy chưa thể tuyên bố
  hoàn tất acceptance criteria của toàn bộ hệ thống RBAC.
- Luồng phát hành/revoke token chưa có; caller cần JWT hợp lệ với employee_id.
  Seeder hiện chỉ có StoreManager/Cashier employee, chưa bootstrap Owner.
- Chưa bổ sung audit pipeline; source chỉ có entity AuditLog. Cần tích hợp
  cơ chế audit chung khi triển khai dependency tương ứng.
- Không thay đổi module bán hàng/kho: việc chặn giao dịch mới của store inactive
  và xử lý token có store claim cũ phải được các module/Auth/T16 thực thi.
- Chưa có chính sách tự đóng ca hoặc hủy đơn khi tắt store; implementation chỉ
  đổi trạng thái, giữ dữ liệu lịch sử. Chưa thêm revoke-owner-access vì ngoài
  5 use case được giao.
- Assignment chạy toàn bộ read/check/write trong transaction Serializable của
  PostgreSQL qua UnitOfWork. Nếu employee bị transfer đồng thời sau snapshot,
  PostgreSQL từ chối ghi dựa trên dữ liệu cũ; transaction rollback, không xóa cache.
  SQLSTATE 40001 được trả theo Result/ErrorType.Invalid hiện có, mã
  Persistence.ConcurrentModification (HTTP 400). Không tự retry; yêu cầu mới
  phải đọc và kiểm tra scope nguồn mới. Cache chỉ được xóa sau commit.
  Các use case khác vẫn dùng SaveChanges như cũ. Việc mở ca/thu hồi quyền đồng
  thời cần phối hợp isolation với module tương ứng; không triển khai T16/T34.
- Cache invalidation sau commit là best effort, chưa có durable retry/outbox;
  cần thống nhất với T16 trước khi dùng permission cache trong production.

## Verification

- dotnet build POS.sln --no-restore -m:1 -nr:false -v:q: PASS, 0 errors, 18 warnings từ package
  dependency hiện có; không sửa package ngoài phạm vi.
- dotnet test POS.sln --no-build --no-restore -m:1 -nr:false -v:minimal: PASS.
  POS.Application.Tests: 25 passed. POS.Api.Tests: 21 passed. Tổng 46, 0 failed.
  POS.Infrastructure.Tests: 1 PostgreSQL concurrency test skipped.
  POS.Domain.Tests chưa có test source.
- Test mới tập trung vào caller/scope, mapping detail, create access atomic
  theo lời gọi UnitOfWork, grant/duplicate race error, promotion/transfer,
  open shift, validation, route và HTTP error mapping.
- Có HTTP model-binding tests dùng MVC/TestServer thật cho isActive; identity và
  mediator được cung cấp trong test để cô lập binding, không giả lập PostgreSQL.
  Concurrency test dùng hai DbContext/transaction và repository PostgreSQL thật,
  điều phối cả hai đọc cùng nguồn, cho request B commit trước rồi kiểm tra A bị
  rollback và lần gửi lại bị Forbidden do nguồn mới nằm ngoài scope của A.
  Test chỉ chạy khi POS_TEST_POSTGRES trỏ tới database test riêng với quyền
  CREATE SCHEMA. Test tạo/xóa schema tên ngẫu nhiên, không reset database hiện có.
  Môi trường hiện tại chưa có biến này và Docker engine không chạy nên test bị
  skipped; chưa tuyên bố transaction/race đã được kiểm chứng trên PostgreSQL thật.
- git diff --check: PASS. Không có migration mới; contract chỉ thêm required metadata.

## Kiểm tra thủ công trước commit/PR

1. Chuẩn bị Owner hợp lệ (role Owner, IsChainOwner và access phù hợp), JWT với
   employee_id, PostgreSQL và Redis. Không dùng role claim để giả lập dữ liệu DB.
2. Gọi đủ 7 endpoint; kiểm tra anonymous 401, StoreManager/Cashier 403,
   Owner ngoài scope 403, Owner list chỉ thấy store được cấp.
3. Create rồi GetById: kiểm tra Location, creator access, các trường detail và UTC timestamps.
4. Update đủ trường, xóa nullable fields; kiểm tra giới hạn DB và input không hợp lệ.
5. Tắt rồi bật lại store; kiểm tra nghiệp vụ bán hàng từ chối store inactive khi
   module tương ứng được tích hợp.
6. Nâng Cashier/chuyển StoreManager giữa hai store caller có quyền; kiểm tra
   employee inactive, source ngoài scope, ca Open, PIN trùng và role seed bị thiếu/trùng.
7. Grant Owner access: kiểm tra GrantedBy/GrantedAt, recipient sai role,
   duplicate tuần tự và hai request đồng thời (409, chỉ một row).
8. Đối chiếu constraint names thực tế với migration:
   IX_employee_store_access_employee_id_store_id và IX_employees_store_id_pin_lookup_hash.
9. Kiểm tra Redis outage sau assignment và tích hợp T16/token invalidation,
   audit, concurrency trước khi đánh dấu toàn bộ acceptance criteria hoàn tất.

## Files modified (18, so với HEAD)

- src/POS.Api/Controllers/StoresController.cs
- src/POS.Api/Extensions/ResultExtensions.cs
- src/POS.Api/Mapping/StoreMapping.cs
- src/POS.Api/Program.cs
- src/POS.Application/Abstractions/Persistence/IStoreRepository.cs
- src/POS.Application/Abstractions/Persistence/IUnitOfWork.cs
- src/POS.Contracts/V1/Stores/StoreRequests.cs
- src/POS.Application/UseCases/Stores/Commands/CreateStore/CreateStoreCommand.cs
- src/POS.Application/UseCases/Stores/Commands/CreateStore/CreateStoreCommandHandler.cs
- src/POS.Application/UseCases/Stores/Commands/CreateStore/CreateStoreCommandValidator.cs
- src/POS.Application/UseCases/Stores/Queries/GetStoreDetail/GetStoreDetailQuery.cs
- src/POS.Application/UseCases/Stores/Queries/GetStoreDetail/GetStoreDetailQueryHandler.cs
- src/POS.Domain/Employees/Employee.cs
- src/POS.Domain/Employees/EmployeeStoreAccess.cs
- src/POS.Domain/Stores/Store.cs
- src/POS.Infrastructure/DependencyInjection.cs
- src/POS.Infrastructure/Persistence/Repositories/StoreRepository.cs
- src/POS.Infrastructure/Persistence/Repositories/UnitOfWork.cs

## Files created (30, gồm báo cáo này; so với HEAD)

- src/POS.Api/Auth/CurrentUser.cs
- src/POS.Application/Abstractions/Persistence/DuplicateStoreAccessException.cs
- src/POS.Application/Abstractions/Persistence/EmployeeAssignmentConflictException.cs
- src/POS.Application/Abstractions/Persistence/IEmployeeRepository.cs
- src/POS.Application/Abstractions/Persistence/IEmployeeStoreAccessRepository.cs
- src/POS.Application/Abstractions/Persistence/IRoleRepository.cs
- src/POS.Application/UseCases/Stores/Commands/AssignAdminToStore/AssignAdminToStoreCommand.cs
- src/POS.Application/UseCases/Stores/Commands/AssignAdminToStore/AssignAdminToStoreCommandHandler.cs
- src/POS.Application/UseCases/Stores/Commands/AssignAdminToStore/AssignAdminToStoreCommandValidator.cs
- src/POS.Application/UseCases/Stores/Commands/GrantOwnerAccess/GrantOwnerAccessCommand.cs
- src/POS.Application/UseCases/Stores/Commands/GrantOwnerAccess/GrantOwnerAccessCommandHandler.cs
- src/POS.Application/UseCases/Stores/Commands/GrantOwnerAccess/GrantOwnerAccessCommandValidator.cs
- src/POS.Application/UseCases/Stores/Commands/UpdateStore/UpdateStoreCommand.cs
- src/POS.Application/UseCases/Stores/Commands/UpdateStore/UpdateStoreCommandHandler.cs
- src/POS.Application/UseCases/Stores/Commands/UpdateStore/UpdateStoreCommandValidator.cs
- src/POS.Application/UseCases/Stores/Commands/UpdateStoreStatus/UpdateStoreStatusCommand.cs
- src/POS.Application/UseCases/Stores/Commands/UpdateStoreStatus/UpdateStoreStatusCommandHandler.cs
- src/POS.Application/UseCases/Stores/Commands/UpdateStoreStatus/UpdateStoreStatusCommandValidator.cs
- src/POS.Application/UseCases/Stores/Queries/GetAllStores/GetAllStoresQuery.cs
- src/POS.Application/UseCases/Stores/Queries/GetAllStores/GetAllStoresQueryHandler.cs
- src/POS.Application/UseCases/Stores/Queries/GetStoreDetail/GetStoreDetailQueryValidator.cs
- src/POS.Application/UseCases/Stores/StoreManagementAccess.cs
- src/POS.Application/UseCases/Stores/StoreValidation.cs
- src/POS.Infrastructure/Persistence/Repositories/EmployeeRepository.cs
- src/POS.Infrastructure/Persistence/Repositories/EmployeeStoreAccessRepository.cs
- src/POS.Infrastructure/Persistence/Repositories/RoleRepository.cs
- tests/POS.Api.Tests/Stores/StoresControllerTests.cs
- tests/POS.Application.Tests/Stores/StoreManagementTests.cs
- tests/POS.Infrastructure.Tests/Stores/StoreAssignmentConcurrencyTests.cs
- docs/store_management_implementation.md

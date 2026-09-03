# POS System — Core Backend & Infrastructure

Hệ thống quản lý bán hàng (POS) xây dựng theo kiến trúc Clean Architecture & CQRS:

- **Backend:** ASP.NET Core (.NET 10) Web API + MediatR (CQRS) + FluentValidation + JWT Bearer
- **Database & Cache:** SQL Server 2022 + Redis 7
- **Observability:** Serilog → Grafana Loki + Prometheus Metrics + Grafana Dashboard
- **Deployment:** Docker Compose

---

## 🛠 Yêu cầu môi trường

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker Desktop + Docker Compose v2
- (Tùy chọn) EF Core CLI tools: `dotnet tool install --global dotnet-ef`
- (Tùy chọn) SQL Server Management Studio (SSMS) hoặc Azure Data Studio / DBeaver

---

## 🚀 Khởi chạy dự án

### 1. Khởi chạy môi trường Development

Mỗi môi trường đã có sẵn file cấu hình `.env` riêng biệt:

- File mẫu: `deploy/dev/.env.example`
- File cấu hình: `deploy/dev/.env`

Chế độ này tự động mount source code từ máy vào container hỗ trợ hot reload:

```bash
cd deploy/dev
docker compose up -d --build
```

**Xem log Hot Reload theo thời gian thực:**

```bash
docker logs -f dev-pos-api-1
```

**Dừng môi trường Dev:**

```bash
cd deploy/dev
docker compose down
```

---

### 2. Khởi chạy môi trường Production (Bản build đóng gói)

- File mẫu: `deploy/production/.env.example`
- File cấu hình: `deploy/production/.env`

Chế độ này build toàn bộ source code thành image tối ưu cho production:

```bash
cd deploy/production
docker compose up -d --build
```

**Dừng môi trường Production:**

```bash
cd deploy/production
docker compose down
```

---

> **Lưu ý:** Hệ thống đã tích hợp cơ chế tự động chạy Migration khi khởi động API, không cần chạy lệnh update database thủ công. Chỉ cần `--build` ở lần chạy đầu tiên. Các lần tiếp theo chỉ cần gõ: `docker compose up -d`

---

## 🌐 Danh sách dịch vụ & Cổng truy cập

| Dịch vụ                     | Địa chỉ / URL                   | Ghi chú                             |
| --------------------------- | ------------------------------- | ----------------------------------- |
| **API Swagger UI**          | http://localhost:5000/swagger   | Tài liệu & Test API trực tiếp       |
| **Health Check tổng quát**  | http://localhost:5000/health    | Kiểm tra tình trạng API             |
| **Health Check DB & Redis** | http://localhost:5000/health/db | Kiểm tra kết nối SQL Server & Redis |
| **Grafana Dashboard**       | http://localhost:3000           | Log & Metrics dashboard             |
| **Prometheus**              | http://localhost:9090           | Thu thập metrics                    |
| **SQL Server**              | `localhost:14330`               | Xem thông tin kết nối bên dưới      |
| **Redis**                   | `localhost:6379`                | Cache & Distributed lock            |

---

## 🔑 Thông tin Kết nối

### 1. Kết nối SQL Server

- **Server Name:** `tcp:localhost,14330` _(cần tiền tố `tcp:` khi dùng SSMS)_
- **Authentication:** SQL Server Authentication
- **User:** `sa`
- **Password:** `your_password`
- **Database:** `pos_dev`
- **Trust Server Certificate:** `True` / `Encrypt=False`

### 2. Kết nối Redis

- **Server Name:** `localhost:6379`
- **User:** `default`
- **Password:** `your_password`

---

## 📦Khi thay đổi Database Schema

Khi thêm hoặc sửa các Domain Entity, tạo migration mới bằng lệnh:

```bash
dotnet ef migrations add <TenMigration> \
  --project src/POS.Infrastructure \
  --startup-project src/POS.Api \
  --output-dir Persistence/Migrations
```

Sau đó rebuild lại Docker:

---

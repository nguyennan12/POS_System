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

## 🚀 Các bước khởi chạy

### 1. Build thử nghiệm (Local)

```bash
cd POS-System
dotnet restore
dotnet build
```

### 2. Khởi chạy toàn bộ hệ thống bằng Docker Compose

```bash
cd deploy
docker compose up -d --build
```

> **Lưu ý:** Cấu hình env theo format **env.sample** trong **/deploy** và không cần chạy lệnh tạo migration thủ công.

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

## 🔑 Thông tin tài khoản & Kết nối mẫu

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

## 📦 Phát triển (Khi thay đổi Database Schema)

Khi thêm hoặc sửa các Domain Entity, tạo migration mới bằng lệnh:

```bash
dotnet ef migrations add <TenMigration> \
  --project src/POS.Infrastructure \
  --startup-project src/POS.Api \
  --output-dir Persistence/Migrations
```

Sau đó rebuild lại Docker:

```bash
cd deploy
docker compose build pos-api
docker compose up -d --no-deps pos-api
```

---

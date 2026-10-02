# POS System — Core Backend & Applications

Hệ thống quản lý bán hàng (POS) xây dựng theo kiến trúc Clean Architecture & CQRS:

- **Backend:** ASP.NET Core (.NET 10) Web API + MediatR (CQRS) + FluentValidation + JWT Bearer
- **Client App:** .NET 10 Desktop Application (WPF Fluent UI / MVVM)
- **Database & Cache:** PostgreSQL 17 + Redis 7
- **Observability:** Serilog → Grafana Alloy Collector → Grafana Cloud (Loki Logs + Prometheus Metrics + APM Dashboard)
- **Deployment:** Docker Compose (dev & production)

---

## 🛠 Yêu cầu môi trường

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 (để chạy ứng dụng Desktop UI)
- Docker Desktop + Docker Compose v2
- (Tùy chọn) EF Core CLI tools: `dotnet tool install --global dotnet-ef`
- (Tùy chọn) DBeaver / pgAdmin / DataGrip

---

## 🚀 Khởi chạy dự án

### 1. Khởi chạy môi trường Backend Development (Docker)

Mỗi môi trường đã có sẵn file cấu hình `.env` riêng biệt:

- File mẫu: `docker/dev/.env.example`
- File cấu hình: `docker/dev/.env`

Chế độ này tự động mount source code từ máy vào container hỗ trợ hot reload:

```bash
cd docker/dev
docker compose up -d --build
```

*(Tùy chọn) Nếu máy có chạy cụm Monitoring (Grafana Alloy / Prometheus):*
```bash
docker compose -f docker-compose.yml -f docker-compose.monitoring.yml up -d --build
```

**Xem log Hot Reload theo thời gian thực:**

```bash
docker logs -f pos_api_dev
```

**Dừng môi trường Dev:**

```bash
cd docker/dev
docker compose down
```

---

### 2. Khởi chạy Giao diện Desktop POS Client (WPF / WinUI)

Sau khi backend API đã chạy (`http://localhost:5000`), khởi chạy ứng dụng bán hàng máy POS trên Windows:

```bash
# Chạy từ thư mục gốc của project:
dotnet run --project src/POS.WinUI/POS.WinUI
```


---

### 3. Khởi chạy môi trường Production Backend 

- File mẫu: `docker/production/.env.example`
- File cấu hình: `docker/production/.env`

Chế độ này build toàn bộ source code thành image tối ưu cho production:

```bash
cd docker/production
docker compose up -d --build
```

*(Tùy chọn) Chạy kèm Monitoring trên Production / Staging:*
```bash
docker compose -f docker-compose.yml -f docker-compose.monitoring.yml up -d --build
```

**Dừng môi trường Production:**

```bash
cd docker/production
docker compose down
```

---

> **Lưu ý:** Hệ thống đã tích hợp cơ chế tự động chạy Migration khi khởi động API, không cần chạy lệnh update database thủ công. Chỉ cần `--build` ở lần chạy đầu tiên. Các lần tiếp theo chỉ cần gõ: `docker compose up -d`

---

## 🌐 Danh sách dịch vụ & Cổng truy cập

| Dịch vụ | Địa chỉ / URL | Ghi chú |
| :--- | :--- | :--- |
| **Giao diện POS Desktop Client** | Ứng dụng Windows (`POS.WinUI`) | Giao diện thu ngân / quầy bán hàng |
| **API Scalar UI** | http://localhost:5000/scalar/v1 | Tài liệu & Test API trực tiếp |
| **Health Check tổng quát** | http://localhost:5000/health | Kiểm tra tình trạng API |
| **Health Check DB & Redis** | http://localhost:5000/health/db | Kiểm tra kết nối PostgreSQL & Redis |
| **Grafana Alloy Web UI** | http://localhost:12345 | Giao diện quản lý & Debug luồng dữ liệu Alloy |
| **Grafana Cloud APM Dashboard** | [Mở Dashboard trên Cloud](https://bigcherry2726.grafana.net/d/pos-apm-cloud/708c2ab) | Giám sát toàn diện Metrics & Logs 24/7 |
| **PostgreSQL** | `localhost:5432` | Xem thông tin kết nối bên dưới |
| **Redis** | `localhost:6379` | Cache & Distributed lock |

---

## 🔑 Thông tin Kết nối

### 1. Kết nối PostgreSQL

- **Host:** `localhost`
- **Port:** `5432`
- **User:** `pos_user`
- **Password:** `Postgres_pos` (theo `.env`)
- **Database:** `pos_dev` (hoặc `pos_prod`)

### 2. Kết nối Redis

- **Host:** `localhost`
- **Port:** `6379`
- **Password:** `redis_pos` (theo `.env`)

---

## 📊 Kiến trúc Observability (Grafana Alloy & Cloud)

Hệ thống sử dụng mô hình Gateway tập trung để thu thập Metrics và Logs:

1. **Metrics:** `pos-api` và `cAdvisor` xuất metrics tại `/metrics`, **Grafana Alloy** (`global_alloy`) định kỳ cào mỗi 15s và `remote_write` lên Grafana Cloud Prometheus.
2. **Logs:** Serilog trong `pos-api` đẩy log nội bộ qua `http://global_alloy:3100`, Alloy tự động gom cụm, nén và chuyển tiếp lên Grafana Cloud Loki kèm `RequestBody` và `ResponseBody`.

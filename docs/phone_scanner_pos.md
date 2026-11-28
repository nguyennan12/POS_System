# Ghép điện thoại quét mã với POS

1. Đặt `SCANNER_PUBLIC_BASE_URL` trong `docker/dev/.env` thành `http://<IP-máy-POS>:5000`. Chạy Docker Compose với cả `docker/dev/docker-compose.yml` và `docker/dev/docker-compose.override.yml` để dùng PostgreSQL/Redis global.
2. Trên iPhone cùng mạng, mở `http://<IP-máy-POS>:5000/health` để kiểm tra đường đến API.
3. Đăng nhập Owner hoặc StoreManager, vào **Cài đặt → Điện thoại quét mã**, chọn quầy và đăng ký điện thoại.
4. Dán URL webhook vừa tạo vào app quét với phương thức **POST**. URL chỉ hiện lúc đăng ký hoặc cấp lại; giữ bí mật. URL cũ theo từng ca không còn dùng được.
5. Thu ngân mở ca tại đúng quầy và vào Cashier. Màn này tự kết nối để nhận mã. Quét SKU có tồn kho; mỗi mã hợp lệ thêm một đơn vị vào giỏ và phát hai tiếng xác nhận trên máy POS.

Quản lý có thể cấp lại URL hoặc thu hồi thiết bị trong Cài đặt. Khi quầy chưa mở ca hoặc Cashier không kết nối, webhook trả 409. URL sai hoặc đã thu hồi trả 401. Khi app trả 202 mà giỏ không đổi, kiểm tra SKU, tồn kho và thông báo trên Cashier.

Trong production, dùng địa chỉ HTTPS cố định cho `SCANNER_PUBLIC_BASE_URL`. Với IP LAN có thể thay đổi, nên giữ IP máy POS ổn định. kisledScan có thể yêu cầu OK, Submit và Reset cho mỗi lần quét; việc đăng ký URL một lần không thay đổi thao tác của app đó.

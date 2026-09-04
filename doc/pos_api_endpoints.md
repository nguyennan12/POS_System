## 🔌 API ENDPOINTS (Scalar)

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
                                  → { reply }
                                  Luồng: IChatbotAiProvider sinh câu trả lời
                                  → lưu ChatMessage (Customer + Bot)
                                  → giới hạn số tin/phiên
GET    /chatbot/sessions/{sessionId}/history

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

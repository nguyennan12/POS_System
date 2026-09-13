# TÀI LIỆU BUSINESS RULES HỆ THỐNG POS

> Tài liệu này mô tả các quy tắc/điều kiện mà hệ thống **bắt buộc** phải tuân theo. Nội dung tập trung vào quyền hạn, điều kiện thực hiện, dữ liệu hợp lệ, ràng buộc, trạng thái, tính toán, quan hệ dữ liệu, ngoại lệ và tác động sau khi nghiệp vụ thành công.

---

## 1. NGUYÊN TẮC CHUNG

### 1.1 Phạm vi áp dụng

- Mọi nghiệp vụ phải được thực hiện trong phạm vi một cửa hàng hợp lệ, trừ nghiệp vụ cấp chuỗi do `Owner` thực hiện.
- Người dùng phải là nhân viên đang hoạt động, không bị khóa tài khoản và có quyền truy cập cửa hàng đang thao tác.
- Hệ thống phải kiểm tra quyền theo RBAC trước khi kiểm tra dữ liệu nghiệp vụ.
- Mọi thao tác tạo, sửa, duyệt, hủy, hoàn tiền, điều chỉnh tồn kho, điều chỉnh điểm và thay đổi cấu hình phải được ghi audit log.
- Các nghiệp vụ làm thay đổi nhiều bảng dữ liệu phải chạy trong cùng một transaction. Nếu một bước thất bại, toàn bộ thay đổi phải rollback.

### 1.2 Vai trò chuẩn

| Vai trò        | Phạm vi quyền                                                                                                                         |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `Owner`        | Quản trị toàn chuỗi, cấu hình hệ thống, sản phẩm, giá, khuyến mãi, nhân sự, báo cáo toàn chuỗi.                                       |
| `StoreManager` | Quản trị nghiệp vụ trong cửa hàng được phân quyền: bán hàng, kho, nhà cung cấp, kiểm kê, duyệt hủy/hoàn/điều chỉnh, báo cáo cửa hàng. |
| `Cashier`      | Bán hàng, thanh toán, tạo/cập nhật khách hàng cơ bản, mở/đóng ca của chính mình.                                                      |

### 1.3 Xử lý ngoại lệ chuẩn

- Nếu không đủ quyền: hệ thống từ chối thao tác, không thay đổi dữ liệu.
- Nếu dữ liệu không hợp lệ: hệ thống trả lỗi validation kèm trường lỗi cụ thể, không tạo bản ghi bán phần.
- Nếu quan hệ dữ liệu không tồn tại hoặc không thuộc cùng cửa hàng: hệ thống trả lỗi nghiệp vụ, không tự tạo dữ liệu liên quan.
- Nếu trạng thái hiện tại không cho phép hành động: hệ thống trả lỗi chuyển trạng thái không hợp lệ.
- Nếu xảy ra xung đột đồng thời khi cập nhật tồn kho, thanh toán hoặc voucher: hệ thống phải khóa/kiểm tra lại dữ liệu trước khi ghi nhận thành công.
- Nếu mất kết nối cổng thanh toán: payment giữ trạng thái `Pending` cho đến khi nhận webhook hợp lệ hoặc hết hạn thành `Timeout`.

---

## 2. PHÂN QUYỀN VÀ PHẠM VI DỮ LIỆU

### 2.1 Quyền hạn

- `Owner` được quản lý dữ liệu toàn chuỗi và có thể phân quyền cho nhân viên.
- `StoreManager` chỉ được thao tác dữ liệu của các cửa hàng đã được gán quyền.
- `Cashier` chỉ được thao tác bán hàng trong cửa hàng và ca làm việc của chính mình.
- Quyền thực tế được xác định bởi role, permission và danh sách cửa hàng được truy cập.

### 2.2 Điều kiện

- Nhân viên phải có trạng thái active.
- Cửa hàng phải có trạng thái active.
- Token đăng nhập phải còn hiệu lực và chưa bị revoke.
- Với thao tác bán hàng, nhân viên phải có ca `Open` hợp lệ tại cửa hàng.

### 2.3 Dữ liệu hợp lệ

- Username là duy nhất toàn hệ thống.
- PIN tra cứu nhanh là duy nhất trong phạm vi cửa hàng.
- Nhân viên cấp chuỗi có thể không gắn `store_id`, nhưng nhân viên cửa hàng phải thuộc ít nhất một cửa hàng.
- Role tùy chỉnh phải có danh sách permission hợp lệ.

### 2.4 Ràng buộc

- Không cho phép nhân viên không thuộc cửa hàng xem hoặc sửa dữ liệu cửa hàng đó.
- Không cho phép tự nâng quyền vượt quá quyền của người đang thao tác.
- Không xóa vật lý dữ liệu đã phát sinh giao dịch; chỉ được khóa hoặc chuyển trạng thái inactive khi nghiệp vụ cho phép.
- Không cho phép dùng token cũ sau khi đổi mật khẩu, đăng xuất hoặc đóng ca.

### 2.5 Trạng thái

| Đối tượng | Chuyển trạng thái hợp lệ                                                                |
| --------- | --------------------------------------------------------------------------------------- |
| Nhân viên | `Active` -> `Inactive/Locked`; `Locked` -> `Active` khi được mở khóa hoặc hết hạn khóa. |
| Cửa hàng  | `Active` -> `Inactive`; cửa hàng inactive không được phát sinh giao dịch mới.           |
| Token     | `Active` -> `Revoked`; token đã revoke không được khôi phục.                            |

### 2.6 Tính toán

- Số lần đăng nhập sai liên tiếp tăng thêm 1 sau mỗi lần sai PIN/mật khẩu.
- Khi số lần sai vượt quá ngưỡng cấu hình, tài khoản bị khóa đến thời điểm `locked_until`.
- Sau khi đăng nhập thành công, số lần sai liên tiếp phải được reset về 0.

### 2.7 Quan hệ dữ liệu

- `EmployeeStoreAccess` xác định nhân viên được thao tác ở cửa hàng nào.
- `RolePermissions` xác định quyền của role.
- Audit log phải tham chiếu được người thực hiện, cửa hàng liên quan và đối tượng bị tác động nếu có.

### 2.8 Ngoại lệ

- Nếu nhân viên bị khóa: từ chối đăng nhập.
- Nếu role không có permission bắt buộc: từ chối thao tác.
- Nếu cửa hàng inactive: từ chối mọi nghiệp vụ tạo giao dịch mới.

### 2.9 Tác động

- Đăng nhập thành công tạo refresh token mới.
- Đăng xuất, đổi mật khẩu hoặc đóng ca phải revoke token liên quan theo chính sách bảo mật.
- Thay đổi quyền phải làm mất hiệu lực cache permission của nhân viên bị ảnh hưởng.

---

## 3. SẢN PHẨM, SKU, DANH MỤC VÀ GIÁ

### 3.1 Quyền hạn

- `Owner` được tạo, sửa, khóa sản phẩm, SKU, bảng giá và cấu hình thuế.
- `StoreManager` chỉ được xem và quản lý trong phạm vi quyền nếu được cấp permission tương ứng.
- `Cashier` chỉ được tra cứu sản phẩm, barcode, giá bán và tồn kho để bán hàng.

### 3.2 Điều kiện

- Cửa hàng phải active.
- Danh mục phải tồn tại trước khi gán sản phẩm vào danh mục.
- SKU chỉ được tạo cho sản phẩm thuộc cùng cửa hàng.
- Giá bán áp dụng phải có hiệu lực tại thời điểm bán.

### 3.3 Dữ liệu hợp lệ

- Tên danh mục và tên sản phẩm không được rỗng.
- Cây danh mục không được tạo vòng lặp cha-con.
- SKU code và barcode phải duy nhất trong phạm vi cửa hàng.
- Giá vốn và giá bán không được âm.
- Thuế VAT của SKU chỉ được nhận các mức hợp lệ: `0`, `5`, `8`, `10`.
- Thuộc tính biến thể của SKU phải là JSON hợp lệ.
- Hệ số quy đổi đơn vị phải lớn hơn 0.

### 3.4 Ràng buộc

- Không bán SKU inactive hoặc SKU thuộc sản phẩm inactive.
- Không cho phép hai bảng giá cùng SKU, cùng nhóm khách hàng, cùng cửa hàng bị chồng lấn thời gian hiệu lực.
- Không xóa danh mục/sản phẩm/SKU đã phát sinh đơn hàng hoặc giao dịch kho.
- Không cho phép giá bán cuối cùng nhỏ hơn 0 sau giảm giá.

### 3.5 Trạng thái

| Đối tượng | Chuyển trạng thái hợp lệ                                        |
| --------- | --------------------------------------------------------------- |
| Sản phẩm  | `Active` <-> `Inactive`                                         |
| SKU       | `Active` <-> `Inactive`                                         |
| Bảng giá  | Có hiệu lực khi nằm trong khoảng ngày áp dụng và không bị khóa. |

### 3.6 Tính toán

- Giá bán tại POS được xác định theo thứ tự: bảng giá hợp lệ theo cửa hàng/SKU/nhóm khách hàng/thời điểm bán, nếu không có thì dùng giá bán mặc định của SKU.
- Giá theo đơn vị quy đổi = giá SKU gốc x hệ số quy đổi hoặc giá bán riêng của đơn vị nếu được cấu hình.
- Giá ghi vào dòng đơn hàng phải được snapshot tại thời điểm thêm vào đơn, không tự thay đổi khi bảng giá sau đó được cập nhật.

### 3.7 Quan hệ dữ liệu

- Product phải thuộc một Category hợp lệ.
- SKU phải thuộc một Product hợp lệ và cùng cửa hàng với Product.
- UnitConversion phải thuộc một SKU hợp lệ.
- PriceList phải tham chiếu SKU, cửa hàng và nhóm khách hàng hợp lệ nếu có.

### 3.8 Ngoại lệ

- Nếu barcode không tồn tại hoặc inactive: POS báo không tìm thấy sản phẩm có thể bán.
- Nếu SKU bị khóa sau khi đã nằm trong giỏ nhưng trước khi thanh toán: hệ thống phải kiểm tra lại và từ chối checkout.
- Nếu giá hợp lệ không xác định được: không cho phép bán SKU đó.

### 3.9 Tác động

- Tạo/cập nhật sản phẩm, SKU, giá phải ghi audit log.
- Thay đổi giá không được sửa lại giá của các order item đã tạo trước đó.
- Khóa SKU làm SKU biến mất khỏi danh sách bán hàng nhưng vẫn giữ dữ liệu lịch sử.

---

## 4. KHO, NHẬP KHO, XUẤT HỦY VÀ KIỂM KÊ

### 4.1 Quyền hạn

- `Owner` và `StoreManager` được nhập kho, xuất hủy, kiểm kê và duyệt điều chỉnh tồn.
- `Cashier` không được tự ý điều chỉnh tồn kho, trừ xuất kho tự động khi bán hàng thành công.
- Xuất hủy và duyệt kiểm kê bắt buộc cần quyền quản lý.

### 4.2 Điều kiện

- SKU phải active và thuộc cửa hàng đang thao tác.
- Nhà cung cấp phải active khi lập phiếu nhập kho.
- Phiếu nhập kho chỉ được hoàn tất khi còn ở trạng thái `Draft`.
- Phiếu kiểm kê chỉ được duyệt khi đã ở trạng thái `Pending`.
- Xuất hủy phải có lý do nghiệp vụ.

### 4.3 Dữ liệu hợp lệ

- Số lượng nhập kho, số lượng kiểm kê thực tế và số lượng xuất hủy phải lớn hơn 0.
- Đơn giá nhập kho không được âm.
- Tồn kho tối thiểu không được âm.
- Lô hàng phải có mã lô duy nhất theo cửa hàng và SKU.
- Hạn dùng, nếu có, không được nhỏ hơn ngày nhập lô.

### 4.4 Ràng buộc

- Không cho phép tồn kho khả dụng âm sau bất kỳ giao dịch nào.
- Không sửa phiếu nhập kho đã `Completed` hoặc `Cancelled`.
- Không sửa phiếu kiểm kê đã `Approved`.
- Không cho phép xuất hủy vượt quá tồn kho khả dụng.
- Không cho phép một SKU có nhiều bản ghi tồn kho chính trong cùng cửa hàng.

### 4.5 Trạng thái

| Đối tượng        | Chuyển trạng thái hợp lệ                       |
| ---------------- | ---------------------------------------------- |
| StockInVoucher   | `Draft` -> `Completed`; `Draft` -> `Cancelled` |
| StockTake        | `Draft` -> `Pending` -> `Approved`             |
| StockTransaction | Sau khi tạo là bất biến, không sửa trực tiếp.  |

### 4.6 Tính toán

- `qty_on_hand_mới = qty_on_hand_hiện_tại + delta`.
- `StockIn` làm tăng tồn kho với delta dương.
- `SaleOut` và `Dispose` làm giảm tồn kho với delta âm.
- `Adjust` làm tăng hoặc giảm tồn kho đúng bằng chênh lệch được duyệt trong kiểm kê.
- Tổng tiền phiếu nhập = tổng `(số lượng x đơn giá)` của từng dòng nhập.
- Nếu cấu hình dùng giá vốn bình quân, giá vốn mới = `(tồn cũ x giá vốn cũ + lượng nhập x đơn giá nhập) / (tồn cũ + lượng nhập)`.
- Cảnh báo tồn thấp phát sinh khi `qty_on_hand <= min_stock`.

### 4.7 Quan hệ dữ liệu

- StockEntry phải gắn với đúng một cặp Store-SKU.
- StockTransaction phải tham chiếu nguồn nghiệp vụ hợp lệ: order, phiếu nhập, phiếu xuất hủy hoặc phiếu kiểm kê.
- StockInVoucher phải tham chiếu Supplier hợp lệ.
- StockTakeItem phải thuộc StockTake cùng cửa hàng.

### 4.8 Ngoại lệ

- Nếu không đủ tồn để bán hoặc xuất hủy: từ chối nghiệp vụ và trả tồn khả dụng hiện tại.
- Nếu hai người cùng cập nhật một SKU: hệ thống phải kiểm tra lại tồn trước khi commit.
- Nếu phiếu nhập/kiểm kê không ở trạng thái được phép: từ chối chuyển trạng thái.

### 4.9 Tác động

- Hoàn tất phiếu nhập tạo StockTransaction `StockIn`, tăng StockEntry và tăng số lượng lô nếu có.
- Bán hàng thành công tạo StockTransaction `SaleOut` và giảm tồn.
- Xuất hủy thành công tạo StockTransaction `Dispose` và giảm tồn.
- Duyệt kiểm kê tạo StockTransaction `Adjust` cho từng SKU có chênh lệch và cập nhật tồn kho.

---

## 5. KHÁCH HÀNG, HẠNG THÀNH VIÊN, ĐIỂM VÀ CRM

### 5.1 Quyền hạn

- `Owner`, `StoreManager` và `Cashier` được tạo/cập nhật thông tin khách hàng cơ bản.
- Điều chỉnh điểm thủ công chỉ được thực hiện bởi `Owner` hoặc `StoreManager`.
- Xem lịch sử mua hàng chi tiết phải có quyền CRM hoặc quyền báo cáo tương ứng.

### 5.2 Điều kiện

- Khách hàng phải active để được tích điểm, dùng điểm và nhận ưu đãi thành viên.
- Đơn hàng phải `Paid` mới được cộng điểm và cộng doanh số tích lũy.
- Điểm chỉ được đổi trong đơn hàng hợp lệ và chưa thanh toán xong.

### 5.3 Dữ liệu hợp lệ

- Số điện thoại khách hàng là định danh chính và phải duy nhất.
- Mã thành viên/barcode khách hàng phải duy nhất nếu được cấp.
- Số dư điểm không được âm.
- Giao dịch điểm phải thuộc một loại hợp lệ: `Earn`, `Redeem`, `Adjust`.
- Lý do bắt buộc khi điều chỉnh điểm thủ công.

### 5.4 Ràng buộc

- Không cho phép đổi điểm vượt quá số dư hiện có.
- Không cho phép dùng điểm của khách hàng này cho đơn hàng của khách hàng khác.
- Không xóa khách hàng đã có đơn hàng; chỉ được chuyển inactive.
- Không tự động thay đổi lịch sử điểm khi sửa thông tin cá nhân khách hàng.

### 5.5 Trạng thái

| Đối tượng       | Chuyển trạng thái hợp lệ                                               |
| --------------- | ---------------------------------------------------------------------- |
| Hạng thành viên | `Normal` -> `Silver` -> `Gold` -> `VIP` theo ngưỡng doanh số cấu hình. |
| CRM Ticket      | `New` -> `Processing` -> `Resolved` -> `Closed`                        |
| Khách hàng      | `Active` <-> `Inactive`                                                |

### 5.6 Tính toán

- Điểm tích lũy = giá trị đủ điều kiện của đơn hàng x tỷ lệ tích điểm theo hạng thành viên.
- Giá trị đổi điểm = số điểm đổi x tỷ lệ quy đổi điểm được cấu hình.
- Tổng chi tiêu khách hàng chỉ tăng khi đơn hàng `Paid`; khi hoàn tiền, tổng chi tiêu và điểm phải được điều chỉnh theo chính sách hoàn tiền.
- Hạng thành viên được tính lại dựa trên tổng chi tiêu hợp lệ sau khi đơn hàng thanh toán hoặc hoàn tiền.

### 5.7 Quan hệ dữ liệu

- LoyaltyAccount phải thuộc đúng một Customer.
- PointTransaction phải tham chiếu Customer/LoyaltyAccount và Order nếu phát sinh từ đơn hàng.
- CRM Ticket phải gắn với Customer nếu khách hàng đã được định danh.

### 5.8 Ngoại lệ

- Nếu số điện thoại đã tồn tại: hệ thống không tạo khách hàng mới trùng, phải trả khách hàng hiện có hoặc báo lỗi trùng.
- Nếu khách hàng inactive: không cho tích điểm hoặc dùng điểm.
- Nếu điều chỉnh điểm làm số dư âm: từ chối thao tác.

### 5.9 Tác động

- Tạo khách hàng thành công tạo LoyaltyAccount mặc định nếu chưa có.
- Đơn hàng `Paid` tạo PointTransaction `Earn` nếu đơn đủ điều kiện.
- Dùng điểm tạo PointTransaction `Redeem` và giảm số dư điểm.
- Điều chỉnh điểm tạo PointTransaction `Adjust`, cập nhật số dư và ghi audit log.

---

## 6. KHUYẾN MÃI VÀ VOUCHER

### 6.1 Quyền hạn

- `Owner` được tạo, sửa, kích hoạt, khóa promotion và voucher.
- `StoreManager` được xem và áp dụng trong cửa hàng; chỉ được cấu hình nếu được cấp permission.
- `Cashier` được áp khuyến mãi/voucher hợp lệ khi bán hàng.
- Ghi đè giảm giá ngoài rule cần quyền quản lý.

### 6.2 Điều kiện

- Promotion phải active và nằm trong thời gian hiệu lực.
- Voucher phải active, chưa hết hạn, chưa vượt số lượt dùng và thỏa giới hạn theo khách hàng.
- Đơn hàng/SKU phải thuộc target của promotion.
- Đơn hàng phải đạt giá trị tối thiểu nếu promotion/voucher có `min_total`.

### 6.3 Dữ liệu hợp lệ

- Loại promotion hợp lệ: `PercentSku`, `FixedSku`, `BuyXGetY`, `CartPercent`, `CartFixed`, `HappyHour`.
- Giá trị giảm, giá trị tối thiểu, giới hạn giảm tối đa và priority không được âm.
- Voucher code phải duy nhất.
- `max_uses` và giới hạn dùng theo khách hàng phải lớn hơn 0.
- Target promotion phải tham chiếu category hoặc SKU hợp lệ.

### 6.4 Ràng buộc

- Không cho áp promotion/voucher hết hạn hoặc inactive.
- Không cho một promotion target đồng thời vừa là category vừa là SKU nếu rule yêu cầu chọn một phạm vi.
- Không cho tổng giảm giá làm giá trị dòng hàng hoặc đơn hàng nhỏ hơn 0.
- Promotion `exclusive` không được áp cùng promotion khác.
- Promotion không `stackable` không được cộng dồn với promotion khác cùng phạm vi.
- Voucher chỉ được ghi nhận sử dụng sau khi order thanh toán thành công.

### 6.5 Trạng thái

| Đối tượng | Chuyển trạng thái hợp lệ                                                            |
| --------- | ----------------------------------------------------------------------------------- |
| Promotion | `Active` <-> `Inactive`; chỉ có hiệu lực khi đang active và đúng thời gian áp dụng. |
| Voucher   | `Active` <-> `Inactive/Expired`; voucher expired không được dùng lại.               |

### 6.6 Tính toán

- Promotion theo dòng hàng được tính trước promotion toàn đơn.
- Promotion phần trăm = giá trị đủ điều kiện x phần trăm giảm, nhưng không vượt `max_discount` nếu có.
- Promotion cố định = số tiền giảm cố định, nhưng không vượt giá trị đủ điều kiện.
- `BuyXGetY` chỉ tính trên số lượng SKU/nhóm SKU đủ điều kiện theo cấu hình.
- Khi nhiều promotion có thể áp dụng, hệ thống xét theo `priority`, `exclusive`, `is_stackable` và cấu hình chọn cộng dồn hoặc chọn lợi ích tốt nhất.
- Grand total sau khuyến mãi = subtotal - tổng giảm giá + tổng thuế.

### 6.7 Quan hệ dữ liệu

- Promotion phải thuộc cửa hàng hoặc phạm vi chuỗi hợp lệ.
- PromotionTarget phải tham chiếu đúng Promotion.
- VoucherUsage phải tham chiếu Voucher, Order và Customer nếu voucher yêu cầu định danh khách hàng.
- OrderDiscount phải tham chiếu promotion hoặc voucher là nguồn giảm giá.

### 6.8 Ngoại lệ

- Nếu voucher không hợp lệ: không áp voucher và trả lý do cụ thể như hết hạn, hết lượt, sai khách hàng hoặc chưa đạt min total.
- Nếu promotion engine gặp xung đột rule: áp dụng rule có priority cao hơn hoặc rule có lợi nhất theo cấu hình.
- Nếu voucher đã được dùng nhưng thanh toán thất bại: không tăng `used_count`.

### 6.9 Tác động

- Áp promotion/voucher tạo OrderDiscount trên đơn hàng.
- Khi order `Paid`, VoucherUsage được tạo và `used_count` của voucher tăng.
- Hủy đơn trước thanh toán phải giải phóng voucher đã giữ chỗ nếu có.

---

## 7. ĐƠN HÀNG, GIỎ HÀNG VÀ CHECKOUT

### 7.1 Quyền hạn

- `Cashier`, `StoreManager` và `Owner` được tạo đơn hàng trong ca hợp lệ.
- Hủy đơn yêu cầu `StoreManager` hoặc `Owner`, trừ đơn `Draft` do chính cashier tạo và chưa phát sinh payment thành công.
- Hoàn tiền yêu cầu `StoreManager` hoặc `Owner`.

### 7.2 Điều kiện

- Nhân viên phải có ca `Open` tại cửa hàng.
- Đơn hàng phải có ít nhất một dòng hàng hợp lệ trước khi xác nhận.
- SKU trong đơn phải active, còn bán được và đủ tồn tại thời điểm checkout.
- Customer, nếu có, phải active.

### 7.3 Dữ liệu hợp lệ

- Số lượng dòng hàng phải lớn hơn 0.
- Đơn giá, thuế, giảm giá và tổng tiền không được âm.
- Giảm giá dòng hàng không được vượt giá trị trước giảm của dòng đó.
- Currency của đơn hàng phải trùng currency cấu hình của cửa hàng.

### 7.4 Ràng buộc

- Không sửa dòng hàng, giá, giảm giá hoặc khách hàng sau khi đơn đã `Paid`.
- Không cho checkout nếu tổng tiền không khớp với tổng dòng hàng, thuế và giảm giá do server tính lại.
- Không cho thanh toán đơn đã `Cancelled`, `Refunded` hoặc `PartiallyRefunded` nếu không có nghiệp vụ hoàn tiền riêng.
- Không cho một order thuộc ca đã `Closed` phát sinh payment mới.

### 7.5 Trạng thái

| Trạng thái hiện tại | Trạng thái được chuyển sang     |
| ------------------- | ------------------------------- |
| `Draft`             | `Confirmed`, `Cancelled`        |
| `Confirmed`         | `Paid`, `Cancelled`             |
| `Paid`              | `PartiallyRefunded`, `Refunded` |
| `PartiallyRefunded` | `Refunded`                      |
| `Cancelled`         | Không chuyển tiếp               |
| `Refunded`          | Không chuyển tiếp               |

### 7.6 Tính toán

- `line_gross = quantity x unit_price`.
- `line_total = line_gross - line_discount + line_tax`.
- `subtotal = tổng line_gross`.
- `discount_total = tổng line_discount + tổng discount toàn đơn`.
- `tax_total = tổng line_tax`.
- `grand_total = subtotal - discount_total + tax_total`.
- Server là nguồn tính toán cuối cùng; client chỉ gửi lựa chọn hàng hóa, số lượng, khách hàng và mã ưu đãi.

### 7.7 Quan hệ dữ liệu

- Order phải thuộc Store và Shift hợp lệ.
- OrderItem phải thuộc Order và tham chiếu SKU hợp lệ.
- OrderDiscount phải thuộc Order và tham chiếu promotion/voucher hợp lệ nếu có.
- Order phải tham chiếu Employee tạo đơn.

### 7.8 Ngoại lệ

- Nếu tồn kho thay đổi trong lúc checkout: hệ thống tính lại tồn và từ chối nếu không đủ.
- Nếu promotion/voucher không còn hợp lệ khi checkout: hệ thống tính lại đơn không có ưu đãi đó và yêu cầu xác nhận lại.
- Nếu order đã bị thay đổi bởi phiên khác: hệ thống trả lỗi xung đột và yêu cầu tải lại đơn.

### 7.9 Tác động

- Tạo order `Draft` lưu snapshot giá, thuế và thông tin SKU tại thời điểm tạo dòng hàng.
- Xác nhận order chuyển sang `Confirmed` và khóa dữ liệu tính tiền để chuẩn bị thanh toán.
- Khi thanh toán đủ, order chuyển `Paid`, ghi `paid_at` và kích hoạt các tác động: trừ kho, ghi payment, dùng voucher, tích điểm, tạo invoice.

---

## 8. THANH TOÁN, HÓA ĐƠN VÀ HOÀN TIỀN

### 8.1 Quyền hạn

- `Cashier`, `StoreManager` và `Owner` được tạo payment cho đơn `Confirmed` trong ca đang mở.
- Hoàn tiền, hủy payment thành công hoặc điều chỉnh sai lệch cần quyền `StoreManager` hoặc `Owner`.
- Webhook thanh toán điện tử được hệ thống tiếp nhận công khai nhưng bắt buộc xác thực chữ ký.

### 8.2 Điều kiện

- Order phải ở trạng thái `Confirmed` và chưa thanh toán đủ.
- Payment bằng tiền mặt yêu cầu số tiền khách đưa vào không nhỏ hơn số tiền cần thu cho phần thanh toán đó.
- Payment MoMo/VietQR/Card phải có mã tham chiếu giao dịch khi xác nhận thành công.
- Payment bằng điểm yêu cầu khách hàng có LoyaltyAccount đủ điểm.

### 8.3 Dữ liệu hợp lệ

- Phương thức thanh toán hợp lệ: `Cash`, `MoMo`, `VietQR`, `Card`, `Points`.
- Số tiền payment phải lớn hơn 0.
- Tiền thừa không được âm.
- Transaction reference phải duy nhất theo phương thức để chống ghi nhận trùng webhook.

### 8.4 Ràng buộc

- Không cho tổng tiền đã thu hợp lệ vượt quá `grand_total` của order.
- Không cho payment `Success` chuyển lại `Pending`.
- Không tạo invoice cho order chưa `Paid`.
- Không tạo nhiều invoice cho cùng một order.
- Không ghi nhận webhook nếu chữ ký không hợp lệ, sai số tiền, sai order hoặc trùng transaction reference.

### 8.5 Trạng thái

| Đối tượng | Chuyển trạng thái hợp lệ                                              |
| --------- | --------------------------------------------------------------------- |
| Payment   | `Pending` -> `Success`; `Pending` -> `Failed`; `Pending` -> `Timeout` |
| Order     | `Confirmed` -> `Paid` khi tổng payment hợp lệ bằng `grand_total`      |
| Invoice   | Tạo một lần sau khi order `Paid`; không sửa số tiền lịch sử.          |

### 8.6 Tính toán

- Với tiền mặt: `cash_applied = amount - change_amount`.
- Với phương thức không tiền mặt: `applied_amount = amount`.
- Tổng đã thu hợp lệ = tổng `applied_amount` của payment `Success`.
- Order chỉ chuyển `Paid` khi tổng đã thu hợp lệ bằng đúng `grand_total`.
- Tiền thừa = số tiền khách đưa - số tiền còn phải thu của phần thanh toán tiền mặt.
- Mã hóa đơn sinh theo định dạng `HD-{StoreCode}-{YYYYMMDD}-{Sequence}` và phải duy nhất.

### 8.7 Quan hệ dữ liệu

- Payment phải thuộc một Order hợp lệ.
- Payment điện tử phải liên kết được với yêu cầu QR hoặc transaction reference.
- Invoice phải tham chiếu duy nhất đến Order.
- Payment bằng điểm phải tham chiếu Customer/LoyaltyAccount của chính order đó.

### 8.8 Ngoại lệ

- Nếu thanh toán chưa đủ: order giữ `Confirmed`, payment thành công vẫn được lưu và hệ thống yêu cầu thanh toán phần còn lại.
- Nếu webhook đến nhiều lần: chỉ ghi nhận một lần theo transaction reference.
- Nếu thanh toán điện tử hết hạn: payment chuyển `Timeout`, order không chuyển `Paid`.
- Nếu tạo invoice thất bại sau khi payment đủ: toàn bộ transaction hoàn tất checkout phải rollback hoặc đưa vào hàng đợi xử lý bù có kiểm soát.

### 8.9 Tác động

- Payment `Success` được ghi nhận vào order và báo realtime về quầy thanh toán nếu là QR.
- Khi order chuyển `Paid`, hệ thống trừ tồn kho, tạo StockTransaction `SaleOut`, cập nhật doanh thu ca, ghi nhận voucher usage, cộng điểm và tạo invoice.
- Hoàn tiền thành công chuyển order sang `PartiallyRefunded` hoặc `Refunded`, ghi nhận payment/transaction hoàn tiền, điều chỉnh điểm và doanh số theo chính sách hoàn tiền.

---

## 9. CA LÀM VIỆC VÀ ĐỐI SOÁT TIỀN MẶT

### 9.1 Quyền hạn

- `Cashier` được mở và đóng ca của chính mình.
- `StoreManager` và `Owner` được xem, đối soát và duyệt sai lệch ca trong phạm vi quyền.

### 9.2 Điều kiện

- Nhân viên chỉ được có một ca `Open` tại cùng một thời điểm trong một cửa hàng.
- Mở ca yêu cầu cửa hàng active và nhân viên có quyền bán hàng tại cửa hàng.
- Đóng ca yêu cầu ca đang `Open` và không còn order `Draft/Confirmed` cần xử lý, trừ khi manager duyệt hủy/chuyển giao.

### 9.3 Dữ liệu hợp lệ

- Tiền đầu ca không được âm.
- Tiền mặt thực đếm cuối ca không được âm.
- Ghi chú bắt buộc nếu có chênh lệch tiền mặt.

### 9.4 Ràng buộc

- Không tạo order mới bằng ca đã `Closed`.
- Không sửa tiền đầu ca sau khi ca đã phát sinh giao dịch nếu không có quyền quản lý.
- Không cho cashier đóng ca của nhân viên khác.
- Không cho đóng ca khi còn payment `Pending` chưa được xử lý theo chính sách cửa hàng.

### 9.5 Trạng thái

| Đối tượng | Chuyển trạng thái hợp lệ |
| --------- | ------------------------ |
| Shift     | `Open` -> `Closed`       |

### 9.6 Tính toán

- Tiền mặt kỳ vọng cuối ca = tiền đầu ca + tổng `cash_applied` của payment tiền mặt thành công - tiền hoàn bằng tiền mặt.
- Chênh lệch tiền mặt = tiền mặt thực đếm - tiền mặt kỳ vọng.
- Doanh thu ca được tính từ order `Paid` phát sinh trong ca, phân tách theo phương thức thanh toán.

### 9.7 Quan hệ dữ liệu

- Shift phải thuộc Employee và Store hợp lệ.
- Order phát sinh trong ca phải tham chiếu Shift.
- Payment trong ca được tổng hợp thông qua Order thuộc Shift đó.

### 9.8 Ngoại lệ

- Nếu ca đã đóng: từ chối tạo order hoặc payment mới.
- Nếu chênh lệch vượt ngưỡng cấu hình: yêu cầu manager duyệt trước khi hoàn tất đối soát.
- Nếu còn payment pending: hệ thống yêu cầu xử lý payment trước hoặc manager xác nhận theo chính sách.

### 9.9 Tác động

- Mở ca tạo Shift trạng thái `Open`.
- Đóng ca cập nhật `closing_cash`, `actual_cash`, thời điểm đóng, trạng thái `Closed` và biên bản đối soát.
- Đóng ca có thể revoke token/phiên bán hàng theo chính sách bảo mật.

---

## 10. NHÀ CUNG CẤP VÀ CÔNG NỢ

### 10.1 Quyền hạn

- `Owner` và `StoreManager` được quản lý nhà cung cấp, phiếu nhập và thanh toán công nợ.
- `Cashier` không được tạo/sửa công nợ nhà cung cấp.

### 10.2 Điều kiện

- Nhà cung cấp phải active để được chọn khi nhập kho.
- Thanh toán công nợ phải tham chiếu nhà cung cấp và phiếu nhập/nhóm công nợ hợp lệ nếu có.

### 10.3 Dữ liệu hợp lệ

- Tên nhà cung cấp không được rỗng.
- Số tiền thanh toán nhà cung cấp phải lớn hơn 0.
- Phương thức thanh toán hợp lệ: `Cash`, `BankTransfer`, `Other`.

### 10.4 Ràng buộc

- Không xóa nhà cung cấp đã có phiếu nhập hoặc thanh toán; chỉ được inactive.
- Không ghi nhận thanh toán vượt quá công nợ còn lại nếu công nợ được theo dõi theo phiếu.
- Không sửa thanh toán công nợ đã đối soát nếu không có quyền quản lý.

### 10.5 Trạng thái

- Nhà cung cấp active mới được phát sinh nghiệp vụ nhập kho mới.
- Phiếu nhập đã `Completed` là nguồn phát sinh công nợ/chi phí, không được đưa về `Draft`.

### 10.6 Tính toán

- Công nợ phát sinh = tổng tiền phiếu nhập hoàn tất - tổng tiền đã thanh toán.
- Số dư công nợ nhà cung cấp = tổng công nợ phát sinh còn lại theo nhà cung cấp.

### 10.7 Quan hệ dữ liệu

- SupplierPayment phải tham chiếu Supplier hợp lệ.
- StockInVoucher phải tham chiếu Supplier hợp lệ.
- Thanh toán theo phiếu, nếu có, phải cùng nhà cung cấp với phiếu nhập.

### 10.8 Ngoại lệ

- Nếu nhà cung cấp inactive: không cho tạo phiếu nhập mới.
- Nếu số tiền thanh toán vượt công nợ còn lại: từ chối thanh toán hoặc yêu cầu manager duyệt theo chính sách.

### 10.9 Tác động

- Hoàn tất phiếu nhập làm tăng giá trị phải trả nhà cung cấp.
- Ghi nhận thanh toán làm giảm công nợ và tạo lịch sử thanh toán.
- Mọi thay đổi công nợ phải xuất hiện trong báo cáo nhà cung cấp.

---

## 11. BÁO CÁO, CẤU HÌNH VÀ OFFLINE

### 11.1 Quyền hạn

- `Owner` được xem báo cáo toàn chuỗi và thay đổi cấu hình hệ thống.
- `StoreManager` được xem báo cáo cửa hàng được phân quyền.
- `Cashier` chỉ được xem thông tin cần thiết cho ca của mình nếu được cấp quyền.

### 11.2 Điều kiện

- Báo cáo chỉ lấy dữ liệu trong phạm vi cửa hàng và thời gian người dùng được phép xem.
- Cấu hình theo cửa hàng ghi đè cấu hình toàn cục nếu cùng khóa cấu hình.
- Offline mode chỉ dùng cho nghiệp vụ được cho phép trong cấu hình.

### 11.3 Dữ liệu hợp lệ

- Khóa cấu hình phải duy nhất theo phạm vi global/store.
- Giá trị cấu hình phải đúng kiểu dữ liệu mà khóa đó yêu cầu.
- Export/import phải đúng schema file đã công bố.
- Dữ liệu offline phải có định danh tạm thời để đồng bộ và chống ghi trùng.

### 11.4 Ràng buộc

- Báo cáo là dữ liệu tổng hợp read-only, không được sửa giao dịch gốc từ màn hình báo cáo.
- Không cho import dữ liệu làm trùng SKU, barcode, voucher code hoặc phá quan hệ dữ liệu.
- Offline không được ghi nhận thanh toán điện tử nếu không xác thực được gateway.
- Offline không được vượt quyền hiện có đã cache của nhân viên.

### 11.5 Trạng thái

| Đối tượng               | Chuyển trạng thái hợp lệ                                           |
| ----------------------- | ------------------------------------------------------------------ |
| Bản ghi đồng bộ offline | `Queued` -> `Synced`; `Queued` -> `Conflict`; `Queued` -> `Failed` |
| Cấu hình                | Phiên bản mới ghi đè hiệu lực tương lai, không sửa audit lịch sử.  |

### 11.6 Tính toán

- Báo cáo doanh thu chỉ tính order `Paid`, trừ/điều chỉnh theo hoàn tiền.
- Báo cáo tồn kho lấy từ StockEntry và đối chiếu với StockTransaction.
- Báo cáo lợi nhuận = doanh thu thuần - giá vốn theo chính sách giá vốn đã cấu hình.
- Báo cáo ca lấy dữ liệu từ Shift, Order và Payment thành công.

### 11.7 Quan hệ dữ liệu

- Mọi số liệu báo cáo phải truy ngược được về order, payment, stock transaction hoặc shift gốc.
- Cấu hình cửa hàng phải tham chiếu Store hợp lệ.
- Dữ liệu offline khi đồng bộ phải map về đúng Store, Shift, Employee và Order gốc.

### 11.8 Ngoại lệ

- Nếu import sai schema: từ chối toàn bộ file hoặc từng dòng theo chế độ import được chọn và trả danh sách lỗi.
- Nếu đồng bộ offline bị trùng hoặc xung đột tồn kho: đánh dấu `Conflict` và yêu cầu xử lý thủ công.
- Nếu người dùng không đủ quyền xem báo cáo: không trả dữ liệu tổng hợp vượt phạm vi.

### 11.9 Tác động

- Thay đổi cấu hình phải ghi audit log và làm mới cache cấu hình.
- Export không thay đổi dữ liệu nguồn.
- Import thành công tạo/cập nhật dữ liệu theo transaction và ghi kết quả import.
- Đồng bộ offline thành công tạo giao dịch thật trên server và liên kết với định danh tạm thời từ client.

---

## 12. QUY TẮC BẤT BIẾN TOÀN HỆ THỐNG

- Không trạng thái cuối nào được sửa trực tiếp: `Cancelled`, `Refunded`, `Completed`, `Approved`, `Closed`, `Success`, `Failed`, `Timeout`.
- Không cho phép số tiền, số lượng, điểm hoặc tồn kho âm nếu nghiệp vụ không định nghĩa rõ delta âm.
- Không cho phép dữ liệu khác cửa hàng bị liên kết vào cùng một nghiệp vụ cửa hàng.
- Không cho phép client tự quyết định tổng tiền cuối cùng, tồn kho cuối cùng, điểm cuối cùng hoặc trạng thái thanh toán cuối cùng.
- Không cho phép webhook, import hoặc đồng bộ offline bỏ qua RBAC, chữ ký, idempotency và kiểm tra quan hệ dữ liệu.
- Mọi nghiệp vụ thành công phải để lại đủ dấu vết để audit: ai làm, làm lúc nào, ở cửa hàng nào, đối tượng nào, trước/sau thay đổi gì nếu áp dụng.

**1. TỔNG QUAN HỆ THỐNG**

Hệ thống POS đa chức năng dành cho bán lẻ, gồm 11 module chính:

1.  Quản lý sản phẩm

2.  Quản lý kho & tồn kho

3.  Quản lý khách hàng & Chăm sóc khách hàng (CRM)

4.  Khuyến mãi / Voucher

5.  Thanh toán (kể cả Split Payment)

6.  Hóa đơn

7.  Nhân viên, phân quyền & quản lý cửa hàng

8.  Ca làm việc (mở ca/đóng ca)

9.  Báo cáo & Dashboard

10. Cấu hình hệ thống (đa ngôn ngữ, đa tiền tệ, barcode offline)

**2. MODULE QUẢN LÝ SẢN PHẨM**

**2.1 Danh mục sản phẩm (Category)**

-   Cấu trúc cây đa cấp (danh mục cha - danh mục con), ví dụ: Đồ uống \>
    Nước ngọt \> Có gas

-   CRUD danh mục, sắp xếp thứ tự hiển thị, ẩn/hiện trên màn hình bán
    hàng

-   Gắn ảnh đại diện danh mục (dùng cho giao diện cảm ứng bán hàng)

**2.2 Sản phẩm & SKU**

-   **Sản phẩm cha (Product)**: tên, mô tả, danh mục, thương hiệu, đơn
    vị tính cơ bản, ảnh, trạng thái (đang bán/ngừng bán)

-   **SKU (biến thể)**: mỗi sản phẩm có thể có nhiều SKU theo thuộc tính
    (size, màu, dung tích\...)

    -   Mã SKU (tự sinh hoặc nhập tay), mã barcode/QR

    -   Đơn vị tính riêng (nếu bán theo nhiều đơn vị: cái, thùng, lốc →
        cần bảng quy đổi đơn vị)

    -   Giá vốn (cost), giá bán lẻ, giá bán buôn (nếu có)

    -   Thuế suất VAT áp dụng (0%, 5%, 8%, 10%)

-   **Quản lý giá**:

    -   Bảng giá theo thời gian hiệu lực (áp dụng khi có đợt tăng/giảm
        giá theo lịch)

    -   Bảng giá riêng theo nhóm khách hàng/cửa hàng (nếu multi-store)

    -   Lịch sử thay đổi giá (audit log)

**3. MODULE QUẢN LÝ KHO & TỒN KHO**

**3.1 Nhập kho (Stock In)**

-   Phiếu nhập kho từ nhà cung cấp: chọn NCC, danh sách SKU + số lượng +
    giá nhập

-   Nhập kho từ trả hàng của khách (return to stock)

-   In phiếu nhập kho, lưu lịch sử

**3.2 Xuất kho (Stock Out)**

-   Xuất bán (tự động khi thanh toán đơn hàng)

-   Xuất hủy (hàng hỏng, hết hạn) --- cần lý do xuất hủy

**3.3 Kiểm kê (Stock Take)**

-   Tạo phiếu kiểm kê theo kho/danh mục/toàn bộ

-   Nhân viên quét barcode kiểm đếm thực tế → hệ thống so sánh với tồn
    hệ thống

-   Xuất báo cáo chênh lệch (thừa/thiếu), có phê duyệt của quản lý trước
    khi điều chỉnh tồn kho chính thức

**3.4 Tồn kho & cảnh báo**

-   Tồn kho real-time theo từng SKU, từng kho/chi nhánh

-   Cảnh báo tồn kho tối thiểu (min stock) → nhắc nhập hàng

-   Cảnh báo hàng cận hạn sử dụng (nếu quản lý theo lô/hạn dùng -
    batch/expiry)

-   Trạng thái hiển thị: Còn hàng / Sắp hết / Hết hàng (để hiện trên màn
    hình bán)

**3.5 Nhà cung cấp (Supplier)**

-   Thông tin NCC: tên, mã số thuế, liên hệ, địa chỉ, điều khoản công nợ

-   Lịch sử nhập hàng theo từng NCC

-   Công nợ phải trả NCC (nếu cần theo dõi công nợ)

**4. MODULE QUẢN LÝ KHÁCH HÀNG & CHĂM SÓC KHÁCH HÀNG (CRM)**

**4.1 Đăng ký thành viên**

-   Thông tin: họ tên, SĐT (dùng làm định danh), ngày sinh, email

-   Mã thành viên tự sinh (barcode/QR riêng cho từng khách để quét khi
    mua hàng)

**4.2 Hạng thành viên & tích điểm**

-   Phân hạng (Thường/Bạc/Vàng/VIP\...) dựa trên tổng chi tiêu hoặc số
    lần mua

-   Quy tắc tích điểm: X% giá trị đơn hàng → điểm, hoặc điểm cố định
    theo mốc chi tiêu

-   Lịch sử giao dịch, lịch sử tích/dùng điểm

**4.3 Chăm sóc khách hàng (CRM)**

-   Ghi nhận phản ánh/khiếu nại/góp ý của khách hàng: tại quầy hoặc qua
    các kênh khác (điện thoại, Zalo, email) nếu có tích hợp sau

-   Phân loại theo loại (Khiếu nại/Góp ý/Khen ngợi), gắn với khách hàng
    và đơn hàng liên quan (nếu có)

-   Gán nhân viên xử lý, theo dõi trạng thái (Mới/Đang xử lý/Đã xử
    lý/Đóng), lưu lịch sử trao đổi xử lý

-   Xem lịch sử chăm sóc khách hàng gắn liền với hồ sơ khách (cùng với
    lịch sử mua hàng)

-   Báo cáo: số lượng khiếu nại theo loại, thời gian xử lý trung bình,
    so sánh giữa các cửa hàng (dành cho Chủ chuỗi xem toàn chuỗi)

**4.4 Trợ lý ảo (Chatbot AI hỏi đáp tự động)**

-   Khách hàng chat trực tiếp qua web widget riêng (nhúng trên website
    cửa hàng), khách hỏi từ xa trên điện thoại cá nhân, không cần đến
    cửa hàng, không cần cài thêm app

-   AI tự động trả lời dựa trên kho kiến thức FAQ do Admin nhập sẵn
    (giờ mở cửa, chính sách đổi trả, thông tin khuyến mãi đang chạy,
    hướng dẫn sử dụng thẻ thành viên...) — **không tự suy diễn/bịa
    thông tin ngoài phạm vi FAQ đã nhập**

-   Admin quản lý kho FAQ: thêm/sửa/xóa câu hỏi-đáp theo từng chủ đề,
    bật/tắt từng mục

-   Lưu lịch sử hội thoại theo phiên chat (session), có thể gắn với
    khách hàng nếu khách đã đăng nhập/định danh

-   Nếu AI không tìm được câu trả lời phù hợp trong kho FAQ → trả lời
    mặc định lịch sự (vd: xin lỗi chưa có thông tin, gợi ý khách liên
    hệ trực tiếp cửa hàng), **không cần chuyển tiếp nhân viên xử lý**
    ở giai đoạn hiện tại (không làm live-chat)

-   Giới hạn số tin nhắn mỗi phiên chat để kiểm soát chi phí gọi AI

**5. MODULE KHUYẾN MÃI / VOUCHER**

**5.1 Loại khuyến mãi**

-   Giảm giá theo % hoặc số tiền cố định trên SKU/danh mục/toàn đơn

-   Mua X tặng Y (combo khuyến mãi)

-   Voucher mã code (dùng 1 lần / nhiều lần, giới hạn số lượt, giới hạn
    theo khách hàng)

-   Khuyến mãi theo khung giờ (happy hour), theo ngày trong tuần

-   Khuyến mãi theo hạng thành viên

**5.2 Tự động áp voucher khi quét barcode**

Đây là điểm quan trọng cần thiết kế kỹ luồng xử lý:

1.  Nhân viên quét barcode sản phẩm → hệ thống thêm vào giỏ hàng

2.  Sau mỗi lần quét, engine khuyến mãi (**Promotion Engine**) kiểm tra:

    -   SKU/danh mục vừa quét có khuyến mãi đang hiệu lực (theo thời
        gian, điều kiện số lượng) không?

    -   Giỏ hàng hiện tại có đạt điều kiện áp voucher tự động không (vd:
        mua 2 tặng 1, hóa đơn \> 200k giảm 10%)?

3.  Nếu đạt điều kiện → tự động áp dụng, hiển thị dòng giảm giá trên hóa
    đơn tạm tính

4.  Nếu có nhiều khuyến mãi cùng áp dụng được → cần **quy tắc ưu tiên**
    (stacking rules): khuyến mãi nào được cộng dồn, khuyến mãi nào loại
    trừ lẫn nhau

5.  Thu ngân có thể xem chi tiết khuyến mãi đã áp, và có quyền hủy áp
    dụng (cần phân quyền override)

**6. MODULE THANH TOÁN**

**6.1 Phương thức thanh toán**

-   **Tiền mặt**: nhập số tiền khách đưa → tự động tính tiền thối
    (change) = tiền đưa - tổng hóa đơn

-   **MoMo QR Code**: sinh mã QR động (tích hợp API MoMo Business/QR
    chuẩn VietQR) → chờ callback xác nhận thanh toán thành công

-   **Thẻ (quẹt thẻ)**: tích hợp máy POS thẻ ngân hàng (qua cổng thanh
    toán/thiết bị POS vật lý) hoặc nhập thủ công mã giao dịch

-   **Thanh toán chia nhiều phương thức (Split Payment)**: cho phép
    kết hợp nhiều PTTT trong cùng 1 hóa đơn (vd: một phần tiền mặt +
    một phần thẻ/QR). Đơn hàng chỉ chuyển sang trạng thái đã thanh
    toán khi tổng các khoản thanh toán thành công bằng đúng tổng hóa
    đơn

**6.2 Luồng xử lý thanh toán**

1.  Tổng hợp hóa đơn (sau khi trừ khuyến mãi)

2.  Chọn một hoặc nhiều phương thức thanh toán (split payment), nhập
    số tiền tương ứng cho từng phương thức

3.  Nếu tiền mặt: nhập tiền khách đưa → hiển thị tiền thối

4.  Nếu QR/thẻ: gọi API cổng thanh toán → chờ kết quả → xác nhận thành
    công/thất bại/timeout (áp dụng riêng cho từng phương thức nếu có
    nhiều phương thức)

5.  Khi tổng các phương thức đã thanh toán thành công bằng đúng tổng
    hóa đơn → ghi nhận giao dịch, trừ tồn kho, cộng điểm tích lũy, in
    hóa đơn. Nếu 1 phương thức thất bại, giữ đơn ở trạng thái chờ để
    thu ngân chọn phương thức khác bù phần còn thiếu hoặc hủy để nhập
    lại

**7. MODULE HÓA ĐƠN**

**7.1 Sinh hóa đơn**

-   Hóa đơn được sinh tự động ngay khi đơn hàng chuyển trạng thái đã
    thanh toán đủ (bao gồm cả trường hợp split payment — chỉ sinh khi
    tổng các phương thức thanh toán = tổng đơn hàng)

-   Số hóa đơn (invoice_no) tự sinh theo quy tắc tăng dần, duy nhất
    theo từng cửa hàng

-   Nội dung hóa đơn: thông tin người mua (nếu có yêu cầu xuất hóa
    đơn có tên/MST công ty), danh sách SKU + số lượng + đơn giá, tổng
    tiền trước thuế, tiền thuế VAT theo từng dòng, tổng tiền sau thuế,
    thông tin khuyến mãi đã áp (nếu có)

**7.2 In & xuất hóa đơn**

-   In hóa đơn ra máy in nhiệt ngay sau khi thanh toán thành công
    (khổ giấy 58mm/80mm tùy máy in)

-   Xem lại hóa đơn theo mã đơn hàng, in lại khi cần

-   Xuất hóa đơn dạng PDF (gửi email/lưu trữ nếu cần)

**7.3 Ghi chú về hóa đơn điện tử**

-   Giai đoạn đầu **chưa** tích hợp hóa đơn điện tử kết nối cơ quan
    thuế (Viettel/MISA/VNPT...) theo Nghị định 123/70 — hóa đơn hiện
    tại chỉ là hóa đơn bán hàng nội bộ, dùng cho đối soát và giao cho
    khách

-   Nếu sau này cần hóa đơn điện tử hợp lệ về pháp lý, sẽ bổ sung
    tích hợp với nhà cung cấp hóa đơn điện tử ở giai đoạn sau

**8. MODULE NHÂN VIÊN, PHÂN QUYỀN & QUẢN LÝ CỬA HÀNG**

**8.1 Vai trò đề xuất**

  -----------------------------------------------------------------------
  **Vai trò**       **Quyền hạn chính**
  ----------------- -----------------------------------------------------
  **Chủ chuỗi       Quản lý nhiều/tất cả cửa hàng trong chuỗi: xem báo
  (Owner)**         cáo tổng hợp đa chi nhánh, cấu hình chung toàn chuỗi,
                    tạo cửa hàng mới và gán Admin cho từng cửa hàng,
                    duyệt chuyển hàng giữa các chi nhánh

  **Admin**         Toàn quyền trong phạm vi 1 cửa hàng: cấu hình hệ
                    thống, quản lý sản phẩm/giá, xem mọi báo cáo, quản
                    lý nhân viên, cấu hình khuyến mãi

  **Quản lý         Nhập/xuất kho, kiểm kê và duyệt kiểm kê, duyệt hủy
  (Manager)**       đơn/hoàn tiền, xem báo cáo doanh thu chi nhánh, quản
                    lý ca làm việc, chuyển hàng giữa các chi nhánh (nếu
                    được cấp quyền)

  **Thu ngân        Bán hàng, thanh toán, mở/đóng ca của mình, áp voucher
  (Cashier)**       (không override giá)
  -----------------------------------------------------------------------

-   Có thể mở rộng thêm phân quyền chi tiết theo module (RBAC:
    Role-Based Access Control) để linh hoạt tùy biến sau này thay vì cố
    định cứng vai trò

**8.2 Quản lý nhân viên**

-   CRUD nhân viên: tạo tài khoản, cập nhật thông tin, gán vai trò
    (Owner/Admin/Manager/Cashier), gán cửa hàng phụ trách

-   Khóa/mở khóa tài khoản (thủ công bởi Admin/Owner, hoặc tự động
    sau 5 lần đăng nhập sai)

-   Đặt lại mật khẩu/PIN cho nhân viên (khi nhân viên quên)

-   Xem lịch sử đăng nhập/đăng xuất của từng nhân viên (audit log)

**8.3 Quản lý cửa hàng (dành cho Owner)**

-   Tạo cửa hàng mới trong chuỗi: tên, địa chỉ, số điện thoại, múi
    giờ, tiền tệ mặc định

-   Gán Admin phụ trách cho từng cửa hàng

-   Cấp quyền truy cập nhiều cửa hàng cho Owner (chọn cửa hàng nào
    Owner được xem/quản lý, hoặc toàn quyền tất cả)

-   Ẩn/ngừng hoạt động 1 cửa hàng (không xóa dữ liệu)

**9. MODULE CA LÀM VIỆC**

**9.1 Mở ca**

-   Nhân viên đăng nhập, nhập số tiền quỹ đầu ca (opening cash)

-   Trong ca: mọi đơn hàng gắn với shift_id để đối soát

**9.2 Đóng ca**

-   Hệ thống tự tổng hợp doanh thu theo phương thức thanh toán trong
    ca → nhân viên kiểm đếm tiền mặt thực tế → so sánh chênh lệch (nếu
    có) → quản lý xác nhận

-   In biên bản đóng ca (báo cáo bàn giao ca)

-   Thu hồi token đăng nhập khi đóng ca (bắt buộc đăng nhập lại ca
    mới)

**10. MODULE BÁO CÁO & DASHBOARD**

**10.1 Dashboard tổng quan**

-   Màn hình đầu tiên khi Admin/Manager/Owner đăng nhập, xem nhanh
    tình hình kinh doanh mà không cần vào từng báo cáo chi tiết

-   Chỉ số nhanh trong ngày: doanh thu hôm nay (so sánh với hôm qua/
    cùng kỳ tuần trước), số đơn hàng, giá trị đơn trung bình

-   Biểu đồ doanh thu 7-30 ngày gần nhất (dạng đường/cột)

-   Top 5 sản phẩm bán chạy trong ngày/tuần

-   Cảnh báo nhanh: số SKU sắp hết hàng, số phiếu kiểm kê chờ duyệt,
    số feedback khách hàng chưa xử lý

-   **Với Owner**: dashboard tổng hợp toàn chuỗi — so sánh doanh thu
    giữa các cửa hàng, xếp hạng cửa hàng theo doanh thu/tăng trưởng

**10.2 Báo cáo doanh thu**

-   Theo ngày / tháng / năm, theo khoảng thời gian tùy chọn

-   Theo chi nhánh, theo nhân viên/ca làm việc

-   Theo phương thức thanh toán

-   Doanh thu theo danh mục/sản phẩm bán chạy (top selling)

-   Báo cáo khuyến mãi đã áp dụng (tổng chiết khấu)

**10.3 Báo cáo tồn kho**

-   Tồn kho hiện tại theo kho/danh mục/SKU

-   Báo cáo nhập-xuất-tồn (theo kỳ)

-   Báo cáo hàng chậm luân chuyển (slow-moving), hàng sắp hết hạn

-   Báo cáo chênh lệch kiểm kê

**10.4 Xuất/nhập file báo cáo**

-   Xuất Excel (.xlsx), PDF, CSV

-   Hỗ trợ đa ngôn ngữ trên báo cáo (tiêu đề cột theo ngôn ngữ đang
    chọn)

-   Nhập file Excel để cập nhật hàng loạt (bulk import) giá, sản phẩm,
    tồn kho đầu kỳ

**11. CẤU HÌNH HỆ THỐNG**

**11.1 Đa ngôn ngữ (i18n)**

-   Toàn bộ giao diện dùng key-based translation (vd: JSON theo từng
    ngôn ngữ: vi.json, en.json\...)

-   Cho phép admin thêm ngôn ngữ mới không cần sửa code (quản lý qua
    bảng translation trong DB hoặc file cấu hình)

**11.2 Đa tiền tệ**

-   Cấu hình tiền tệ mặc định theo cửa hàng (VND, USD\...)

-   Định dạng hiển thị số (dấu phân cách hàng nghìn/thập phân) theo từng
    tiền tệ/ngôn ngữ

-   Nếu cần đa tiền tệ thực sự (bán hàng nhận nhiều loại tiền) → cần
    thêm tỷ giá quy đổi và làm tròn theo quy tắc riêng

**11.3 Barcode & chế độ Offline (optional)**

-   Máy quét barcode kết nối qua USB/Bluetooth, hoạt động như bàn phím
    (keyboard wedge) hoặc qua SDK riêng

-   **Chế độ offline**: đây là phần phức tạp nhất về mặt kỹ thuật, cần
    thiết kế kỹ:

    -   App bán hàng lưu cache dữ liệu sản phẩm/giá/khuyến mãi/tồn kho
        tại máy local (SQLite/IndexedDB)

    -   Khi mất mạng: vẫn cho phép quét bán hàng, lưu đơn hàng tạm vào
        hàng đợi local (offline queue)

    -   Khi có mạng trở lại: đồng bộ (sync) đơn hàng lên server, xử lý
        xung đột dữ liệu (conflict resolution) --- ví dụ tồn kho có thể
        bị âm nếu 2 máy cùng bán 1 sản phẩm sắp hết khi offline

    -   Thanh toán QR/thẻ **không thể** hoạt động offline (cần mạng để
        xác thực giao dịch) → chỉ nên cho phép tiền mặt khi offline,
        hoặc ghi nợ tạm

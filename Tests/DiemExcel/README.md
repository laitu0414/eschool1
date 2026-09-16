# Kiểm tra quản lý điểm

Chạy từ thư mục dự án với đầu ra riêng để không ghi đè ứng dụng đang chạy:

    dotnet run --project Tests/DiemExcel/DiemExcel.Tests.csproj -p:UseAppHost=false -p:OutputPath=D:/DATN/obj/grades-tests-bin/

29 kiểm tra tự động: đọc XLSX, số thập phân, nhiều điểm, ô trống,
dữ liệu sai, công thức, tiêu đề, học sinh trùng, tính điểm theo trọng số
1–1–2–3, thiếu điểm, điểm 0, anti-forgery và mã phiên bản điểm.

Điểm 0–10; dấu phẩy/chấm cho số thập phân, dấu chấm phẩy tách nhiều điểm.
Nhập tay xóa trắng ô sẽ xóa điểm; Excel để trống giữ nguyên điểm.
ĐTB môn chỉ có khi đủ bốn nhóm điểm hợp lệ. Trung bình trên màn hình
là trung bình các môn/bản ghi có điểm, không phải xếp loại hoặc kết luận tốt nghiệp.

## Cần nghiệm thu với tài khoản và dữ liệu thử

- Nhập/sửa/xóa điểm, đối chiếu nhật ký trước–sau.
- Giáo viên chỉ sửa môn được phân công đúng lớp và học kỳ, kể cả phân công cả năm.
- Mở hai cửa sổ: lưu cửa sổ thứ nhất, cửa sổ thứ hai phải báo phiên cũ.
- Khóa xét lên lớp phải chặn nhập tay, Excel và xóa điểm.
- Excel có một dòng sai: không dòng nào được lưu.
- Xem điểm học sinh/phụ huynh, bộ lọc và in phiếu điểm.
- Lỗi phiên đăng nhập, mạng hoặc database không được báo lưu thành công.

Các kiểm tra tự động không thay thế kiểm thử giao dịch database,
phân quyền có phiên đăng nhập hoặc thao tác trình duyệt.
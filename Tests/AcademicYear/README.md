# Kiểm tra trạng thái năm học

Chạy từ thư mục dự án:

    dotnet run --project Tests/AcademicYear/AcademicYear.Tests.csproj -p:UseAppHost=false -p:OutputPath=D:/DATN/obj/academic-year-tests-bin/

12 kiểm tra quy tắc: năm đã qua dù còn bật trạng thái, khóa thủ công,
năm hiện tại, năm tương lai, năm không tồn tại, khoảng ngày sai,
ngày kết thúc còn được thao tác và ngày tiếp theo tự khóa.

Cần nghiệm thu với database và phiên đăng nhập:
- POST thêm/sửa/xóa lớp năm cũ bị từ chối; sửa cả năm học gửi lên vẫn bị chặn.
- POST phân công, lịch học, lịch nghỉ, học kỳ năm cũ bị từ chối.
- Chuyển lớp thủ công và Excel không chuyển vào/ra lớp năm cũ.
- Thay đổi số tiết môn học không sửa lịch của lớp năm cũ.
- Năm tương lai còn mở vẫn tạo được lớp; dữ liệu năm cũ vẫn xem được.

Các kiểm tra tự động chỉ kiểm tra quy tắc trạng thái, không thay thế kiểm thử HTTP/database.
Kiểm tra biên dịch runtime Razor cho 12 view: 5 view học vụ, menu dùng chung, lối tắt trong trang, tab năm học/học kỳ 3 layout quản trị và nút quay lại theo trang.

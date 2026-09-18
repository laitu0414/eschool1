using eSchool.Models;

namespace eSchool.Infrastructure;

public static class AcademicYearPolicy
{
    public const string ReadOnlyMessage = "Năm học đã kết thúc, đã khóa hoặc không hợp lệ. Không thể thay đổi dữ liệu.";

    public static bool CanModify(NamHoc? year, DateTime today) =>
        year != null && year.TrangThai && year.NgayBatDau.Date <= year.NgayKetThuc.Date
        && year.NgayKetThuc.Date >= today.Date;

    public static string Status(NamHoc year, DateTime today) =>
        year.NgayKetThuc.Date < today.Date ? "Đã kết thúc" :
        !year.TrangThai ? "Đã khóa" :
        year.NgayBatDau.Date > today.Date ? "Sắp bắt đầu" : "Đang diễn ra";
}

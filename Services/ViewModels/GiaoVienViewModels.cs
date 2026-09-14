using System.ComponentModel.DataAnnotations;
using eSchool.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eSchool.ViewModels
{
    public class GiaoVienFormViewModel
    {
        public int IdGiaoVien { get; set; }

        // Mã nội bộ được hệ thống tạo khi lưu, không nhận từ biểu mẫu.
        [StringLength(20)]
        public string MaGV { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; } = DateTime.Today;

        public string? GioiTinh { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0")]
        [StringLength(10)]
        public string? SDT { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string? Email { get; set; }

        public string? DiaChi { get; set; }

        public string? AnhDaiDien { get; set; }
        public IFormFile? AnhTaiLen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn môn dạy")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn dạy")]
        public int? IdMonHoc { get; set; }

        public List<SelectListItem> MonHocs { get; set; } = new();
    }

    public class PhanCongGiangDayViewModel
    {
        public List<PhanCongGiangDay> DanhSach { get; set; } = new();

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn giáo viên")]
        public int IdGiaoVien { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học")]
        public int IdMonHoc { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp học")]
        public int IdLop { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập học kỳ")]
        public string HocKy { get; set; } = "Học kỳ 1";

        [Required(ErrorMessage = "Vui lòng nhập năm học")]
        public string NamHoc { get; set; } = $"{DateTime.Today.Year}-{DateTime.Today.Year + 1}";

        [Range(2, 7, ErrorMessage = "Thứ phải từ 2 đến 7")]
        public int Thu { get; set; } = 2;

        [Range(1, 15, ErrorMessage = "Tiết bắt đầu không hợp lệ")]
        public int TietBatDau { get; set; } = 1;

        [Range(1, 5, ErrorMessage = "Số tiết phải từ 1 đến 5")]
        public int SoTiet { get; set; } = 1;

        public List<SelectListItem> GiaoViens { get; set; } = new();
        public List<SelectListItem> MonHocs { get; set; } = new();
        public List<SelectListItem> LopHocs { get; set; } = new();
        public Dictionary<int, int?> GiaoVienMonHocIds { get; set; } = new();
    }

    public class ChuNhiemViewModel
    {
        public List<LopHoc> LopHocs { get; set; } = new();
        public List<SelectListItem> GiaoViens { get; set; } = new();
    }
}

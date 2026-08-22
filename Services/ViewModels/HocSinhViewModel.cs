using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eSchool.ViewModels
{
    public class HocSinhViewModel
    {
        public int IdHocSinh { get; set; }

        [Required(ErrorMessage = "Mã học sinh không được để trống")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Mã học sinh phải từ 2 đến 20 ký tự")]
        public string MaHS { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        public DateTime NgaySinh { get; set; } = DateTime.Now;

        public string? GioiTinh { get; set; }
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(15)]
        public string? SDT { get; set; }
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string? Email { get; set; }
        [StringLength(255)]
        public string? DiaChi { get; set; }
        public string? AnhDaiDien { get; set; }
        public IFormFile? AnhTaiLen { get; set; }

        public bool TrangThai { get; set; } = true;

        public int? IdTaiKhoan { get; set; }
        public string? TenTaiKhoan { get; set; }
        public List<SelectListItem>? TaiKhoans { get; set; }

        public int? IdLopHoc { get; set; }
        public string? TenLop { get; set; }

        public List<SelectListItem>? LopHocs { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace eSchool.ViewModels
{
    public class PhuHuynhViewModel
    {
        public int IdPhuHuynh { get; set; }

        public int? IdHocSinh { get; set; }

        public string? TenHocSinh { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> HocSinhs { get; set; } = new();

        [Required(ErrorMessage = "Họ tên phụ huynh không được để trống")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
        public string HoTen { get; set; } = string.Empty;

        // SĐT đăng nhập được lấy từ học sinh liên kết, không nhận từ biểu mẫu.
        [StringLength(10)]
        public string? SDT { get; set; }
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string? Email { get; set; }
        [StringLength(255)]
        public string? DiaChi { get; set; }
        [StringLength(100)]
        public string? NgheNghiep { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}

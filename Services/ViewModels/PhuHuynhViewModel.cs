using System.ComponentModel.DataAnnotations;

namespace eSchool.ViewModels
{
    public class PhuHuynhViewModel
    {
        public int IdPhuHuynh { get; set; }

        [Required(ErrorMessage = "Họ tên phụ huynh không được để trống")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
        public string HoTen { get; set; } = string.Empty;

        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(15)]
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

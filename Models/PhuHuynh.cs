using System.ComponentModel.DataAnnotations;

namespace eSchool.Models
{
    public class PhuHuynh
    {
        [Key]
        public int IdPhuHuynh { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; }

        [Required]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0")]
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

        public int? IdTaiKhoan { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("IdTaiKhoan")]
        public TaiKhoan? TaiKhoan { get; set; }

        public ICollection<HocSinhPhuHuynh>? HocSinhPhuHuynhs { get; set; }
    }
}

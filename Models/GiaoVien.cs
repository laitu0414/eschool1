using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class GiaoVien
    {
        [Key]
        public int IdGiaoVien { get; set; }

        [Required]
        [StringLength(20)]
        public string MaGV { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; }

        public DateTime NgaySinh { get; set; }

        [StringLength(10)]
        public string? GioiTinh { get; set; }

        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0")]
        [StringLength(10)]
        public string? SDT { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [StringLength(255)]
        public string? AnhDaiDien { get; set; }

        public int? IdTaiKhoan { get; set; }

        [ForeignKey("IdTaiKhoan")]
        public TaiKhoan? TaiKhoan { get; set; }

        public int? IdMonHoc { get; set; }

        [ForeignKey("IdMonHoc")]
        public MonHoc? MonHoc { get; set; }

        public ICollection<LopHoc>? LopChuNhiems { get; set; }

        public ICollection<PhanCongGiangDay>? PhanCongGiangDays { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class TaiKhoan
    {
        [Key]
        public int IdTaiKhoan { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public bool TrangThai { get; set; } = true;

        public bool BatBuocDoiMatKhau { get; set; } = false;

        public int IdChucVu { get; set; }

        [ForeignKey("IdChucVu")]
        public ChucVu? ChucVu { get; set; }

        public GiaoVien? GiaoVien { get; set; }

        public HocSinh? HocSinh { get; set; }

        public PhuHuynh? PhuHuynh { get; set; }

        public ICollection<ThongBao>? ThongBaos { get; set; }
    }
}

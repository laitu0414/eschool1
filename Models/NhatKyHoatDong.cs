using System.ComponentModel.DataAnnotations;

namespace eSchool.Models
{
    public class NhatKyHoatDong
    {
        [Key]
        public int IdNhatKy { get; set; }

        [StringLength(100)]
        public string TenDangNhap { get; set; }

        [StringLength(100)]
        public string HanhDong { get; set; }

        public string NoiDung { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }
}
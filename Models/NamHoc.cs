using System.ComponentModel.DataAnnotations;

namespace eSchool.Models
{
    public class NamHoc
    {
        [Key]
        public int IdNamHoc { get; set; }

        [Required]
        [StringLength(20)]
        public string TenNamHoc { get; set; } = string.Empty;

        public DateTime NgayBatDau { get; set; }

        public DateTime NgayKetThuc { get; set; }

        public bool TrangThai { get; set; } = true;

        public ICollection<HocKy>? HocKys { get; set; }

        public ICollection<Diem>? Diems { get; set; }

        public ICollection<HocPhi>? HocPhis { get; set; }

        public ICollection<PhieuDiem>? PhieuDiems { get; set; }
    }
}

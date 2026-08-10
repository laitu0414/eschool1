using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class HocKy
    {
        [Key]
        public int IdHocKy { get; set; }

        [Required]
        [StringLength(30)]
        public string TenHocKy { get; set; } = string.Empty;

        public int IdNamHoc { get; set; }

        [ForeignKey(nameof(IdNamHoc))]
        public NamHoc? NamHoc { get; set; }

        public DateTime NgayBatDau { get; set; }

        public DateTime NgayKetThuc { get; set; }

        public bool TrangThai { get; set; } = true;

        public ICollection<Diem>? Diems { get; set; }

        public ICollection<HocPhi>? HocPhis { get; set; }

        public ICollection<PhieuDiem>? PhieuDiems { get; set; }
    }
}

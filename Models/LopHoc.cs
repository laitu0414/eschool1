using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class LopHoc
    {
        [Key]
        public int IdLop { get; set; }

        [Required]
        [StringLength(20)]
        public string MaLop { get; set; }

        [Required]
        [StringLength(50)]
        public string TenLop { get; set; }

        [StringLength(20)]
        public string? Khoi { get; set; }
        
        [StringLength(50)]
        public string? BuoiHoc { get; set; }

        [StringLength(20)]
        public string? NamHoc { get; set; }

        public int? IdGiaoVienCN { get; set; }

        [ForeignKey("IdGiaoVienCN")]
        public GiaoVien? GiaoVienChuNhiem { get; set; }

        public ICollection<DangKyLop>? DangKyLops { get; set; }

        public ICollection<PhanCongGiangDay>? PhanCongGiangDays { get; set; }

        public ICollection<DiemDanh>? DiemDanhs { get; set; }

        public ICollection<PhieuDiem>? PhieuDiems { get; set; }
    }
}

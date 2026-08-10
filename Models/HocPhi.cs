using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class HocPhi
    {
        [Key]
        public int IdHocPhi { get; set; }

        public int IdHocSinh { get; set; }

        [ForeignKey(nameof(IdHocSinh))]
        public HocSinh? HocSinh { get; set; }

        public int? IdNamHoc { get; set; }

        [ForeignKey(nameof(IdNamHoc))]
        public NamHoc? NamHoc { get; set; }

        public int? IdHocKy { get; set; }

        [ForeignKey(nameof(IdHocKy))]
        public HocKy? HocKyInfo { get; set; }

        [StringLength(20)]
        public string HocKy { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Học phí không được âm")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal SoTien { get; set; }

        public DateTime? NgayDuKien { get; set; }

        public DateTime? HanDongTien { get; set; }

        public DateTime? NgayDong { get; set; }

        public int TrangThai { get; set; }

        [StringLength(100)]
        public string? PhuongThuc { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PhanTramMienGiam { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? SoTienMienGiam { get; set; }

        [StringLength(255)]
        public string? LyDoMienGiam { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }
}

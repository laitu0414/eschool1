using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class KyLuat
    {
        [Key]
        public int IdKyLuat { get; set; }

        [Required]
        public int IdHocSinh { get; set; }

        [ForeignKey(nameof(IdHocSinh))]
        public virtual HocSinh? HocSinh { get; set; }

        public int? IdGiaoVien { get; set; }

        [ForeignKey(nameof(IdGiaoVien))]
        public virtual GiaoVien? GiaoVien { get; set; }

        public int? IdHocKy { get; set; }
        
        [ForeignKey(nameof(IdHocKy))]
        public virtual HocKy? HocKyInfo { get; set; }

        [Required]
        public DateTime NgayViPham { get; set; }

        [Required(ErrorMessage = "Hình thức kỷ luật không được để trống")]
        [StringLength(100)]
        public string HinhThuc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lý do kỷ luật không được để trống")]
        [StringLength(500)]
        public string LyDo { get; set; } = string.Empty;

        public bool TrangThai { get; set; } = true;
    }
}

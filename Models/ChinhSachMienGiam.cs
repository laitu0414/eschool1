using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class ChinhSachMienGiam
    {
        [Key]
        public int IdMienGiam { get; set; }

        [Required]
        public int IdHocSinh { get; set; }

        [ForeignKey("IdHocSinh")]
        public virtual HocSinh? HocSinh { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PhanTramGiam { get; set; }

        [StringLength(255)]
        public string LyDo { get; set; } = string.Empty;

        [StringLength(100)]
        public string HieuLuc { get; set; } = string.Empty;

        [StringLength(500)]
        public string? GhiChu { get; set; }
    }
}

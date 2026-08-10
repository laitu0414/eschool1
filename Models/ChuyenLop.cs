using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class ChuyenLop
    {
        [Key]
        public int IdChuyenLop { get; set; }

        public int IdHocSinh { get; set; }

        [ForeignKey(nameof(IdHocSinh))]
        public HocSinh HocSinh { get; set; }

        public int IdLopCu { get; set; }

        public int IdLopMoi { get; set; }

        public DateTime NgayChuyen { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? LyDo { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }
}
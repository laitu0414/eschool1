using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class DiemDanh
    {
        [Key]
        public int IdDiemDanh { get; set; }

        public int IdHocSinh { get; set; }

        [ForeignKey("IdHocSinh")]
        public HocSinh? HocSinh { get; set; }

        public int IdLop { get; set; }

        [ForeignKey("IdLop")]
        public LopHoc? LopHoc { get; set; }

        public DateTime NgayHoc { get; set; }

        public int? IdTietHoc { get; set; }

        [StringLength(30)]
        public string TrangThai { get; set; } = "Có mặt";

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }
}

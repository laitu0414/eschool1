using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class DangKyLop
    {
        [Key]
        public int IdDangKy { get; set; }

        public int IdHocSinh { get; set; }

        [ForeignKey("IdHocSinh")]
        public HocSinh? HocSinh { get; set; }

        public int IdLop { get; set; }

        [ForeignKey("IdLop")]
        public LopHoc? LopHoc { get; set; }

        public DateTime NgayDangKy { get; set; } = DateTime.Now;
    }
}
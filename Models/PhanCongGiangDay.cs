using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class PhanCongGiangDay
    {
        [Key]
        public int IdPhanCong { get; set; }

        public int? IdGiaoVien { get; set; }

        [ForeignKey("IdGiaoVien")]
        public GiaoVien? GiaoVien { get; set; }

        public int IdMonHoc { get; set; }

        [ForeignKey("IdMonHoc")]
        public MonHoc? MonHoc { get; set; }

        public int IdLop { get; set; }

        [ForeignKey("IdLop")]
        public LopHoc? LopHoc { get; set; }

        [StringLength(20)]
        public string? HocKy { get; set; }

        [StringLength(20)]
        public string? NamHoc { get; set; }

        public int? Thu { get; set; }

        public int? TietBatDau { get; set; }

        public int? SoTiet { get; set; }

        public int? IdPhongHoc { get; set; }

        [ForeignKey("IdPhongHoc")]
        public PhongHoc? PhongHoc { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class LichHocThayDoi
    {
        [Key]
        public int IdThayDoi { get; set; }

        public DateTime Ngay { get; set; }

        public int? IdLop { get; set; }
        [ForeignKey("IdLop")]
        public virtual LopHoc? LopHoc { get; set; }

        public int? TietBatDau { get; set; }
        public int? SoTiet { get; set; }

        public bool IsNghi { get; set; }

        public int? IdMonHocThayThe { get; set; }
        [ForeignKey("IdMonHocThayThe")]
        public virtual MonHoc? MonHocThayThe { get; set; }

        public int? IdGiaoVienThayThe { get; set; }
        [ForeignKey("IdGiaoVienThayThe")]
        public virtual GiaoVien? GiaoVienThayThe { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }
}

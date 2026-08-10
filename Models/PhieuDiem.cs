using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class PhieuDiem
    {
        [Key]
        public int IdPhieuDiem { get; set; }

        public int IdHocSinh { get; set; }

        [ForeignKey(nameof(IdHocSinh))]
        public HocSinh? HocSinh { get; set; }

        public int? IdLop { get; set; }

        [ForeignKey(nameof(IdLop))]
        public LopHoc? LopHoc { get; set; }

        public int? IdHocKy { get; set; }

        [ForeignKey(nameof(IdHocKy))]
        public HocKy? HocKy { get; set; }

        public int? IdNamHoc { get; set; }

        [ForeignKey(nameof(IdNamHoc))]
        public NamHoc? NamHoc { get; set; }

        public DateTime NgayLap { get; set; } = DateTime.Now;

        public int? NguoiLap { get; set; }

        [ForeignKey(nameof(NguoiLap))]
        public TaiKhoan? TaiKhoanNguoiLap { get; set; }
    }
}

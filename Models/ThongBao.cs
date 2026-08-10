using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class ThongBao
    {
        [Key]
        public int IdThongBao { get; set; }

        [Required]
        [StringLength(200)]
        public string TieuDe { get; set; }

        [Required]
        public string NoiDung { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public int DoiTuongNhan { get; set; } = 0;

        [NotMapped]
        public string TenDoiTuongNhan => DoiTuongNhan switch
        {
            1 => "Giáo viên",
            2 => "Học sinh",
            _ => "Tất cả"
        };

        public int IdTaiKhoan { get; set; }

        [ForeignKey("IdTaiKhoan")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class ThietBi
    {
        [Key]
        public int IdThietBi { get; set; }

        [Required(ErrorMessage = "Mã thiết bị không được để trống")]
        [StringLength(20)]
        public string MaTB { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên thiết bị không được để trống")]
        [StringLength(100)]
        public string TenTB { get; set; } = string.Empty;

        [StringLength(50)]
        public string LoaiTB { get; set; } = string.Empty;

        public int SoLuong { get; set; } = 1;

        public int IdPhongHoc { get; set; }
        [ForeignKey("IdPhongHoc")]
        public virtual PhongHoc? PhongHoc { get; set; }

        [StringLength(50)]
        public string TinhTrang { get; set; } = "Tốt"; // Tốt, Hỏng, Đang sửa

        public DateTime? NgayMua { get; set; }
    }
}

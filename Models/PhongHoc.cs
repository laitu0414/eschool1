using System.ComponentModel.DataAnnotations;

namespace eSchool.Models
{
    public class PhongHoc
    {
        [Key]
        public int IdPhongHoc { get; set; }

        [Required(ErrorMessage = "Mã phòng không được để trống")]
        [StringLength(20)]
        public string MaPhong { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng không được để trống")]
        [StringLength(100)]
        public string TenPhong { get; set; } = string.Empty;

        public int SucChua { get; set; } = 40;

        [StringLength(50)]
        public string LoaiPhong { get; set; } = "Học lý thuyết"; // Học lý thuyết, Thực hành, Lab, Thể chất

        [StringLength(255)]
        public string? TrangThietBi { get; set; } // Máy chiếu, Điều hòa, ...

        public bool TrangThai { get; set; } = true; // Đang sử dụng hay Đang bảo trì

        public int? IdLop { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("IdLop")]
        public virtual LopHoc? LopHoc { get; set; }
    }
}

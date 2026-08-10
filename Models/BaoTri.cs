using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class BaoTri
    {
        [Key]
        public int IdBaoTri { get; set; }

        [Required(ErrorMessage = "Mã bảo trì không được để trống")]
        [StringLength(20)]
        public string MaBaoTri { get; set; } = string.Empty;

        public int IdThietBi { get; set; }
        [ForeignKey("IdThietBi")]
        public virtual ThietBi? ThietBi { get; set; }

        [Required(ErrorMessage = "Ngày bảo trì không được để trống")]
        public DateTime NgayBaoTri { get; set; }

        [Required(ErrorMessage = "Nội dung bảo trì không được để trống")]
        [StringLength(500)]
        public string NoiDung { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chi phí không được để trống")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ChiPhi { get; set; }

        [StringLength(100)]
        public string NguoiThucHien { get; set; } = string.Empty;

        [StringLength(100)]
        public string KetQua { get; set; } = string.Empty;

        [StringLength(50)]
        public string TrangThai { get; set; } = "Chờ xử lý"; // Chờ xử lý, Đang xử lý, Hoàn thành
    }
}

using System.ComponentModel.DataAnnotations;

namespace eSchool.Models
{
    public class ChucVu
    {
        [Key]
        public int IdChucVu { get; set; }

        [Required]
        [StringLength(50)]
        public string TenChucVu { get; set; }

        public ICollection<TaiKhoan>? TaiKhoans { get; set; }
    }
}
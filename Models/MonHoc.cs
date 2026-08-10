using System.ComponentModel.DataAnnotations;

namespace eSchool.Models
{
    public class MonHoc
    {
        [Key]
        public int IdMonHoc { get; set; }

        [Required]
        [StringLength(20)]
        public string MaMon { get; set; }

        [Required]
        [StringLength(100)]
        public string TenMon { get; set; }

        [Range(1, 500, ErrorMessage = "Số tiết phải lớn hơn 0")]
        public int SoTiet { get; set; }

        public ICollection<PhanCongGiangDay>? PhanCongGiangDays { get; set; }

        public ICollection<GiaoVien>? GiaoViens { get; set; }

        public ICollection<Diem>? Diems { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eSchool.ViewModels
{
    public class ChuyenLopViewModel
    {
        public int IdChuyenLop { get; set; }

        [Required]
        public int IdHocSinh { get; set; }

        public string? MaHS { get; set; }
        public string? HoTen { get; set; }
        public string? LopCu { get; set; }

        public int IdLopCu { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp mới")]
        public int IdLopMoi { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày chuyển")]
        public DateTime NgayChuyen { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? LyDo { get; set; }
        [StringLength(255)]
        public string? GhiChu { get; set; }

        public List<SelectListItem>? LopHocs { get; set; }
    }
}

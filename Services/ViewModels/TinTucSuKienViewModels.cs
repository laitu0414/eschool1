using System.ComponentModel.DataAnnotations;
using eSchool.Models;

namespace eSchool.ViewModels
{
    public class TinTucSuKienFormViewModel
    {
        public int IdTinTucSuKien { get; set; }

        [Required(ErrorMessage = "Vui long nhap tieu de")]
        [StringLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui long nhap mo ta")]
        [StringLength(1000)]
        public string MoTa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui long nhap duong dan")]
        [StringLength(500)]
        [Url(ErrorMessage = "Duong dan khong hop le")]
        public string DuongDan { get; set; } = string.Empty;

        public string? AnhMinhHoa { get; set; }

        public IFormFile? AnhTaiLen { get; set; }

        public bool TrangThai { get; set; } = true;

        public LoaiTinTuc LoaiTin { get; set; } = LoaiTinTuc.TinTuc;
    }

    public class TrangChuViewModel
    {
        public List<TinTucSuKien> TinTucSuKiens { get; set; } = new();
        public List<TinTucSuKien> TopTinTucHomNay { get; set; } = new();
        public List<ThongBao> ThongBaos { get; set; } = new();
        public bool OpenLogin { get; set; }
        public bool OpenForgotPassword { get; set; }
    }
}

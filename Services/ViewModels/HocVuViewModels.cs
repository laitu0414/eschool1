using System.ComponentModel.DataAnnotations;
using eSchool.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eSchool.ViewModels
{
    public class LopHocFormViewModel
    {
        public int IdLop { get; set; }

        [Required(ErrorMessage = "Mã lớp không được để trống")]
        [StringLength(20, MinimumLength = 2)]
        public string MaLop { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(50, MinimumLength = 2)]
        public string TenLop { get; set; } = string.Empty;

        [StringLength(20)]
        [RegularExpression(@"^Khối [6-9]$", ErrorMessage = "Khối chỉ được chọn từ Khối 6 đến Khối 9")]
        public string? Khoi { get; set; }

        public string? BuoiHoc { get; set; }

        [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "Năm học phải có dạng 2026-2027")]
        public string? NamHoc { get; set; }
        public int? IdGiaoVienCN { get; set; }
        public int? IdPhongHoc { get; set; }
    }

    public class MonHocFormViewModel
    {
        public int IdMonHoc { get; set; }

        [Required(ErrorMessage = "Mã môn không được để trống")]
        [StringLength(20, MinimumLength = 2)]
        public string MaMon { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên môn không được để trống")]
        [StringLength(100, MinimumLength = 2)]
        public string TenMon { get; set; } = string.Empty;

        [Range(1, 500, ErrorMessage = "Số tiết phải lớn hơn 0")]
        public int SoTiet { get; set; } = 1;
    }

    public class ThoiKhoaBieuHocVuViewModel
    {
        public List<PhanCongGiangDay> DanhSach { get; set; } = new();

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn giáo viên")]
        public int IdGiaoVien { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học")]
        public int IdMonHoc { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp học")]
        public int IdLop { get; set; }

        public string? HocKy { get; set; } = "Cả năm";

        [Required(ErrorMessage = "Vui lòng chọn năm học")]
        public string NamHoc { get; set; } = string.Empty;

        [Range(2, 7)]
        public int Thu { get; set; } = 2;

        [Range(1, 15)]
        public int TietBatDau { get; set; } = 1;

        [Range(1, 5)]
        public int SoTiet { get; set; } = 1;

        public List<SelectListItem> GiaoViens { get; set; } = new();
        public List<SelectListItem> MonHocs { get; set; } = new();
        public List<SelectListItem> LopHocs { get; set; } = new();
        public List<SelectListItem> NamHocs { get; set; } = new();
        public List<SelectListItem> HocKys { get; set; } = new();
        public Dictionary<int, int?> GiaoVienMonHocIds { get; set; } = new();
    }

    public class HocKyPageViewModel
    {
        public List<HocKy> DanhSach { get; set; } = new();
        public List<SelectListItem> NamHocs { get; set; } = new();
    }
}

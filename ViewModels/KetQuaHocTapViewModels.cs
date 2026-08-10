using System.ComponentModel.DataAnnotations;
using eSchool.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eSchool.ViewModels
{
    public class DiemFormViewModel
    {
        public int IdDiem { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn học sinh")]
        public int IdHocSinh { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học")]
        public int IdMonHoc { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn năm học")]
        public int IdNamHoc { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn học kỳ")]
        public int IdHocKy { get; set; }

        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? Diem15Phut { get; set; }

        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? Diem1Tiet { get; set; }

        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? DiemGiuaKy { get; set; }

        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? DiemCuoiKy { get; set; }
    }

    public class DiemHocSinhViewModel
    {
        public int IdHocSinh { get; set; }
        public HocSinh? HocSinh { get; set; }
        public decimal? DiemTBHocKy { get; set; }
        public Dictionary<int, decimal?> DiemTBMon { get; set; } = new();
    }

    public class DiemPageViewModel
    {
        public List<Diem> DanhSach { get; set; } = new();
        public List<SelectListItem> HocSinhs { get; set; } = new();
        public List<SelectListItem> MonHocs { get; set; } = new();
        public List<SelectListItem> NamHocs { get; set; } = new();
        public List<SelectListItem> HocKys { get; set; } = new();
    }

    public class AdminDiemPageViewModel
    {
        public List<DiemHocSinhViewModel> DanhSach { get; set; } = new();
        public List<SelectListItem> LopHocs { get; set; } = new();
        public List<SelectListItem> NamHocs { get; set; } = new();
        public List<SelectListItem> HocKys { get; set; } = new();
        public List<MonHoc> MonHocsList { get; set; } = new();
    }

    public class DiemMonHocViewModel
    {
        public int IdMonHoc { get; set; }
        public string TenMon { get; set; } = string.Empty;
        public string? Diem15Phut { get; set; }
        public string? Diem1Tiet { get; set; }
        public string? DiemGiuaKy { get; set; }
        public string? DiemCuoiKy { get; set; }
        public bool IsEditable { get; set; } = true;
    }

    public class LuuDiemHocSinhRequest
    {
        public int IdHocSinh { get; set; }
        public int IdNamHoc { get; set; }
        public int IdHocKy { get; set; }
        public List<DiemMonHocViewModel> DiemMonHocs { get; set; } = new();
    }

    public class DiemDanhFormViewModel
    {
        [Range(1, int.MaxValue)] public int IdHocSinh { get; set; }
        [Range(1, int.MaxValue)] public int IdLop { get; set; }
        public DateTime NgayHoc { get; set; } = DateTime.Today;
        [Range(1, 15)] public int? IdTietHoc { get; set; }
        [Required] public string TrangThai { get; set; } = "Có mặt";
        public string? GhiChu { get; set; }
    }

    public class DiemDanhPageViewModel
    {
        public List<DiemDanh> DanhSach { get; set; } = new();
        public List<SelectListItem> HocSinhs { get; set; } = new();
        public List<SelectListItem> LopHocs { get; set; } = new();
    }

    public class GiaoVienDiemDanhPageViewModel
    {
        public int? IdPhanCong { get; set; }
        public DateTime NgayHoc { get; set; } = DateTime.Today;
        public List<GiaoVienDiemDanhPhanCongViewModel> PhanCongs { get; set; } = new();
        public GiaoVienDiemDanhPhanCongViewModel? PhanCongDangChon { get; set; }
        public List<GiaoVienDiemDanhHocSinhViewModel> HocSinhs { get; set; } = new();
    }

    public class GiaoVienDiemDanhPhanCongViewModel
    {
        public int IdPhanCong { get; set; }
        public int IdLop { get; set; }
        public int IdMonHoc { get; set; }
        public string TenLop { get; set; } = string.Empty;
        public string TenMonHoc { get; set; } = string.Empty;
        public string? NamHoc { get; set; }
        public string? HocKy { get; set; }
        public int? Thu { get; set; }
        public int? TietBatDau { get; set; }
        public int? SoTiet { get; set; }
    }

    public class GiaoVienDiemDanhHocSinhViewModel
    {
        public int IdHocSinh { get; set; }
        public string MaHS { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public bool CoMat { get; set; } = true;
        public string? GhiChu { get; set; }
    }

    public class GiaoVienLuuDiemDanhViewModel
    {
        [Range(1, int.MaxValue)]
        public int IdPhanCong { get; set; }

        public DateTime NgayHoc { get; set; } = DateTime.Today;

        public List<GiaoVienDiemDanhHocSinhViewModel> HocSinhs { get; set; } = new();
    }

    public class AdminDiemDanhPageViewModel
    {
        public List<SelectListItem> LopHocs { get; set; } = new();
        public List<SelectListItem> NamHocs { get; set; } = new();
        public List<DiemDanhBuoiHocViewModel> DanhSachBuoiHoc { get; set; } = new();
    }

    public class ChiTietDiemDanhPageViewModel
    {
        public DiemDanhBuoiHocViewModel BuoiHoc { get; set; } = new();
        public List<DiemDanhHocSinhChiTietViewModel> ChiTietHocSinhs { get; set; } = new();
    }

    public class DiemDanhBuoiHocViewModel
    {
        public int IdLop { get; set; }
        public string TenLop { get; set; } = string.Empty;
        public DateTime NgayHoc { get; set; }
        public int? IdTietHoc { get; set; }
        public int? IdMonHoc { get; set; }
        public string TenMonHoc { get; set; } = string.Empty;
        public string TenGiaoVien { get; set; } = string.Empty;
        public int TongHocSinh { get; set; }
        public int SoHocSinhCoMat { get; set; }
        public int SoHocSinhVang { get; set; }
    }

    public class DiemDanhHocSinhChiTietViewModel
    {
        public string MaHS { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }

    public class HocSinhDiemDanhPageViewModel
    {
        public List<SelectListItem> MonHocs { get; set; } = new();
        public int? IdMonHoc { get; set; }
        public string TenMonHocDangChon { get; set; } = string.Empty;
        public List<HocSinhDiemDanhChiTietViewModel> LichSuDiemDanh { get; set; } = new();
    }

    public class HocSinhDiemDanhChiTietViewModel
    {
        public DateTime NgayHoc { get; set; }
        public string TenMonHoc { get; set; } = string.Empty;
        public string TenGiaoVien { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public int? IdTietHoc { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }

    public class HocPhiFormViewModel
    {
        [Range(1, int.MaxValue)] public int IdHocSinh { get; set; }
        [Range(1, int.MaxValue)] public int IdNamHoc { get; set; }
        [Range(1, int.MaxValue)] public int IdHocKy { get; set; }
        [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Học phí không được âm")]
        public decimal SoTien { get; set; }
        public DateTime? NgayDuKien { get; set; }
        public DateTime? HanDongTien { get; set; }
        public DateTime? NgayDong { get; set; }
        public int TrangThai { get; set; }
        public string? PhuongThuc { get; set; }
        
        [Range(typeof(decimal), "0", "100", ErrorMessage = "Phần trăm miễn giảm từ 0-100")]
        public decimal? PhanTramMienGiam { get; set; }
        
        [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Số tiền miễn giảm không được âm")]
        public decimal? SoTienMienGiam { get; set; }
        
        public string? LyDoMienGiam { get; set; }
        
        public string? GhiChu { get; set; }
    }

    public class HocPhiTongHopViewModel
    {
        public string NamHoc { get; set; } = string.Empty;
        public string HocKy { get; set; } = string.Empty;
        public string Khoi { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public decimal SoTien { get; set; }
        public int TongSoHocSinh { get; set; }
        public int DaDong { get; set; }
        public DateTime? HanDongTien { get; set; }
    }

    public class HocPhiPageViewModel
    {
        public List<HocPhi> DanhSach { get; set; } = new();
        public List<HocPhiTongHopViewModel> DanhSachTongHop { get; set; } = new();
        public List<SelectListItem> HocSinhs { get; set; } = new();
        public List<SelectListItem> NamHocs { get; set; } = new();
        public List<SelectListItem> HocKys { get; set; } = new();
    }

    public class PhieuDiemPageViewModel
    {
        public List<PhieuDiem> DanhSach { get; set; } = new();
        public List<SelectListItem> HocSinhs { get; set; } = new();
        public List<SelectListItem> NamHocs { get; set; } = new();
        public List<SelectListItem> HocKys { get; set; } = new();
    }
}

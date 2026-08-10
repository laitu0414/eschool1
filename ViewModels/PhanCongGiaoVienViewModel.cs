using eSchool.Models;

namespace eSchool.ViewModels
{
    public class PhanCongGiaoVienViewModel
    {
        public LopHoc LopHoc { get; set; } = new();
        public List<GiaoVienInfoItem> DanhSachGiaoVien { get; set; } = new();
        public List<PhanCongMonHocItem> DanhSachMonHoc { get; set; } = new();
    }

    public class PhanCongMonHocItem
    {
        public int IdMonHoc { get; set; }
        public string TenMonHoc { get; set; } = string.Empty;
        public int? IdGiaoVien { get; set; }
        public string? TenGiaoVien { get; set; }
        public List<string> ChiTietTietHoc { get; set; } = new();
        public List<string> RequiredPeriods { get; set; } = new();
    }

    public class GiaoVienInfoItem
    {
        public GiaoVien GiaoVien { get; set; } = new();
        public int TongSoTiet { get; set; }
        public List<string> ChiTietLichDay { get; set; } = new();
        public List<string> BusyPeriods { get; set; } = new();
    }
}

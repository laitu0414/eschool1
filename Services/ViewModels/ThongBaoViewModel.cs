namespace eSchool.ViewModels
{
    public class ThongBaoViewModel
    {
        public int IdThongBao { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public int IdTaiKhoan { get; set; }
        public string? Username { get; set; }
    }
}
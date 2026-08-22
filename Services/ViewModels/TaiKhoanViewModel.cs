namespace eSchool.ViewModels
{
    public class TaiKhoanViewModel
    {
        public int IdTaiKhoan { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int IdChucVu { get; set; }
        public string? TenChucVu { get; set; }
        public bool TrangThai { get; set; }
    }
}
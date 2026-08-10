using eSchool.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class HocSinh
{
    [Key]
    public int IdHocSinh { get; set; }

    [Required]
    [StringLength(20)]
    public string MaHS { get; set; }

    [Required]
    [StringLength(100)]
    public string HoTen { get; set; }

    public DateTime NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không đúng định dạng")]
    [StringLength(15)]
    public string? SDT { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(100)]
    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? AnhDaiDien { get; set; }

    public string? NoiSinh { get; set; }

    public string? DanToc { get; set; }

    public string? TonGiao { get; set; }

    public string? QuocTich { get; set; }

    public DateTime? NgayNhapHoc { get; set; }

    public bool TrangThai { get; set; } = true;

    public DateTime? NgayTao { get; set; } = DateTime.Now;

    public string? GhiChu { get; set; }

    public int? IdTaiKhoan { get; set; }

    [ForeignKey(nameof(IdTaiKhoan))]
    public TaiKhoan? TaiKhoan { get; set; }

    public int? IdLopHoc { get; set; }

    [ForeignKey(nameof(IdLopHoc))]
    public LopHoc? LopHoc { get; set; }

    public ICollection<DangKyLop>? DangKyLops { get; set; }

    public ICollection<Diem>? Diems { get; set; }

    public ICollection<DiemDanh>? DiemDanhs { get; set; }

    public ICollection<HocPhi>? HocPhis { get; set; }

    public ICollection<PhieuDiem>? PhieuDiems { get; set; }

     public ICollection<HocSinhPhuHuynh>? HocSinhPhuHuynhs { get; set; }

    public ICollection<ChuyenLop>? ChuyenLops { get; set; }
}

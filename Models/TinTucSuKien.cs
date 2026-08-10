using System.ComponentModel.DataAnnotations;

namespace eSchool.Models
{
    public enum LoaiTinTuc
    {
        TuyenSinh = 1,
        TinTuc = 2,
        ThongBao = 3,
        ThanhTich = 4,
        ChuongTrinhDaoTao = 5
    }

    public class TinTucSuKien
    {
        [Key]
        public int IdTinTucSuKien { get; set; }

        [Required]
        [StringLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string MoTa { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string DuongDan { get; set; } = string.Empty;

        [StringLength(255)]
        public string? AnhMinhHoa { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public bool TrangThai { get; set; } = true;

        public int LuotXem { get; set; } = 0;
        
        public int ThoiGianDoc { get; set; } = 5;

        public LoaiTinTuc LoaiTin { get; set; } = LoaiTinTuc.TinTuc;
    }
}

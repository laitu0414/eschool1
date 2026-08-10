using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eSchool.Models
{
    public class Diem
    {
        [Key]
        public int IdDiem { get; set; }

        public int IdHocSinh { get; set; }

        [ForeignKey(nameof(IdHocSinh))]
        public HocSinh? HocSinh { get; set; }

        public int IdMonHoc { get; set; }

        [ForeignKey(nameof(IdMonHoc))]
        public MonHoc? MonHoc { get; set; }

        public int? IdHocKy { get; set; }

        [ForeignKey(nameof(IdHocKy))]
        public HocKy? HocKyInfo { get; set; }

        public int? IdNamHoc { get; set; }

        [ForeignKey(nameof(IdNamHoc))]
        public NamHoc? NamHocInfo { get; set; }

        [StringLength(20)]
        public string HocKy { get; set; } = string.Empty;

        [StringLength(255)]
        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? Diem15Phut { get; set; }

        [StringLength(255)]
        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? Diem1Tiet { get; set; }

        [StringLength(255)]
        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? DiemGiuaKy { get; set; }

        [StringLength(255)]
        [RegularExpression(@"^[0-9]+(\.[0-9]+)?(,\s*[0-9]+(\.[0-9]+)?)*$", ErrorMessage = "Điểm không hợp lệ")]
        public string? DiemCuoiKy { get; set; }

        [Range(typeof(decimal), "0", "10", ErrorMessage = "Điểm trung bình phải từ 0 đến 10")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal? DiemTB { get; set; }

        private (decimal Sum, int Count) GetSumAndCountFromString(string? gradesStr)
        {
            if (string.IsNullOrWhiteSpace(gradesStr)) return (0m, 0);

            var parts = gradesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var sum = 0m;
            var count = 0;

            foreach (var part in parts)
            {
                if (decimal.TryParse(part.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value) && value >= 0 && value <= 10)
                {
                    sum += value;
                    count++;
                }
            }

            return (sum, count);
        }

        public void TinhDiemTrungBinh()
        {
            var p15 = GetSumAndCountFromString(Diem15Phut);
            var p1t = GetSumAndCountFromString(Diem1Tiet);
            var gk = GetSumAndCountFromString(DiemGiuaKy);
            var ck = GetSumAndCountFromString(DiemCuoiKy);

            if (p15.Count == 0 || p1t.Count == 0 || gk.Count == 0 || ck.Count == 0)
            {
                DiemTB = null;
                return;
            }

            decimal totalSum = p15.Sum * 1 + p1t.Sum * 1 + gk.Sum * 2 + ck.Sum * 3;
            int totalWeight = p15.Count * 1 + p1t.Count * 1 + gk.Count * 2 + ck.Count * 3;

            DiemTB = Math.Round(totalSum / totalWeight, 2);
        }
    }
}

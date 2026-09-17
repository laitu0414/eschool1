using eSchool.Models;

namespace eSchool.Services;

public static class AttendanceRules
{
    public static bool IsPresent(string? status) =>
        string.Equals(status?.Trim(), "Có mặt", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status?.Trim(), "Co mat", StringComparison.OrdinalIgnoreCase);

    public static List<DiemDanh> Latest(IEnumerable<DiemDanh> records) => records
        .GroupBy(x => new { x.IdHocSinh, x.IdLop, Date = x.NgayHoc.Date, x.IdTietHoc })
        .Select(g => g.OrderByDescending(x => x.IdDiemDanh).First()).ToList();

    public static PhanCongGiangDay? Match(DiemDanh record, IEnumerable<PhanCongGiangDay> assignments)
    {
        if (!record.IdTietHoc.HasValue) return null;
        var day = record.NgayHoc.DayOfWeek == DayOfWeek.Sunday ? 8 : (int)record.NgayHoc.DayOfWeek + 1;
        var matches = assignments.Where(x => x.IdLop == record.IdLop && x.Thu == day
            && x.TietBatDau.HasValue && record.IdTietHoc >= x.TietBatDau
            && record.IdTietHoc < x.TietBatDau + (x.SoTiet ?? 1)).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}
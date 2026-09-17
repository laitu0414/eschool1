using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eSchool.Models;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Services;

public sealed class AnnualSubjectScore
{
    public int SubjectId { get; set; }
    public string Name { get; set; } = "";
    public decimal? Semester1 { get; set; }
    public decimal? Semester2 { get; set; }
    public decimal? Annual { get; set; }
    public string Error { get; set; } = "";
}

public sealed class AnnualStudentScore
{
    public HocSinh Student { get; set; } = null!;
    public int YearId { get; set; }
    public List<AnnualSubjectScore> Subjects { get; set; } = new();
    public decimal? Average { get; set; }
    public bool Complete { get; set; }
    public bool Eligible { get; set; }
    public bool Finalized { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public string Reason { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public string LogKey => $"TongKetNamHoc:{YearId}:{Student.IdHocSinh}";
}

public sealed class AnnualScoreService(AppDbContext context)
{
    public static int? ParseGradeLevel(string? value)
    {
        var text = value?.Trim() ?? "";
        foreach (var prefix in new[] { "Khối", "Khoi" })
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(prefix.Length).Trim();
                break;
            }
        return int.TryParse(text, out var level) && level >= 6 && level <= 9 ? level : null;
    }
    public static string NormalizeYear(string? value) => (value ?? "").Replace(" ", "").Trim();
    public static decimal? CalculateAnnual(decimal? first, decimal? second) =>
        first is >= 0 and <= 10 && second is >= 0 and <= 10
            ? Math.Round((first.Value + 2 * second.Value) / 3, 2, MidpointRounding.AwayFromZero) : null;

    public static AnnualStudentScore Evaluate(HocSinh student, NamHoc? year, IReadOnlyList<HocKy> semesters,
        IReadOnlyList<PhanCongGiangDay> assignments, IReadOnlyList<Diem> grades, IReadOnlyList<MonHoc> subjects)
    {
        var result = new AnnualStudentScore { Student = student, YearId = year?.IdNamHoc ?? 0 };
        if (student.DaTotNghiep) { result.Reason = "Học sinh đã tốt nghiệp."; return result; }
        if (!student.TrangThai) { result.Reason = "Học sinh đã ngừng học, không xét."; return result; }
        if (year == null || student.LopHoc == null) { result.Reason = "Chưa xác định được lớp và năm học."; return result; }
        var terms = semesters.Where(s => s.IdNamHoc == year.IdNamHoc).OrderBy(s => s.NgayBatDau).ThenBy(s => s.IdHocKy).ToList();
        if (terms.Count != 2 || terms[0].NgayBatDau >= terms[1].NgayBatDau)
        { result.Reason = "Năm học cần đúng hai học kỳ có ngày bắt đầu riêng biệt."; return result; }
        var required = assignments.Where(a => a.IdLop == student.IdLopHoc &&
            (string.IsNullOrWhiteSpace(a.NamHoc) || NormalizeYear(a.NamHoc) == NormalizeYear(year.TenNamHoc)))
            .Select(a => a.IdMonHoc).Distinct().OrderBy(id => id).ToList();
        var termIds = terms.Select(t => t.IdHocKy).ToList();
        // Chỉ dùng các dòng điểm có liên quan đến năm đang tổng kết.
        // Dữ liệu cũ có thể chưa có IdNamHoc nhưng vẫn hợp lệ nếu IdHocKy thuộc đúng năm.
        var rows = grades.Where(g => g.IdHocSinh == student.IdHocSinh &&
            (g.IdNamHoc == year.IdNamHoc ||
             (!g.IdNamHoc.HasValue && g.IdHocKy.HasValue && termIds.Contains(g.IdHocKy.Value)) ||
             (g.IdHocKy.HasValue && termIds.Contains(g.IdHocKy.Value)))).ToList();

        // Tổng kết chỉ dựa trên các môn được phân công cho lớp.
        // Điểm thừa của môn không còn được phân công không được làm hỏng cả kết quả năm.
        foreach (var subjectId in required)
        {
            var subject = new AnnualSubjectScore
            {
                SubjectId = subjectId,
                Name = subjects.FirstOrDefault(m => m.IdMonHoc == subjectId)?.TenMon ?? $"Môn {subjectId}"
            };
            var data = rows.Where(g => g.IdMonHoc == subjectId).ToList();

            decimal? ReadSemester(int term)
            {
                var matches = data.Where(g => g.IdHocKy == term &&
                    (!g.IdNamHoc.HasValue || g.IdNamHoc == year.IdNamHoc)).ToList();
                if (matches.Count != 1) return null;

                var grade = matches[0];
                // Luôn tính lại từ các đầu điểm để tránh dùng DiemTB cache cũ.
                grade.TinhDiemTrungBinh();
                return grade.DiemTB;
            }

            subject.Semester1 = ReadSemester(terms[0].IdHocKy);
            subject.Semester2 = ReadSemester(terms[1].IdHocKy);
            subject.Annual = CalculateAnnual(subject.Semester1, subject.Semester2);

            // Chỉ coi là sai năm khi IdNamHoc có giá trị và thực sự khác năm hiện tại.
            // IdNamHoc null được chấp nhận cho dữ liệu cũ nếu IdHocKy vẫn thuộc đúng năm.
            if (data.Any(g =>
                (g.IdNamHoc.HasValue && g.IdNamHoc != year.IdNamHoc) ||
                !g.IdHocKy.HasValue ||
                !termIds.Contains(g.IdHocKy.Value)))
            {
                subject.Annual = null;
                subject.Error = "Dữ liệu sai năm học/học kỳ.";
            }
            else if (!subject.Annual.HasValue)
            {
                subject.Error = string.Join("; ", new[] { subject.Semester1 == null ? "HK1 thiếu điểm hợp lệ hoặc trùng bản ghi" : null, subject.Semester2 == null ? "HK2 thiếu điểm hợp lệ hoặc trùng bản ghi" : null }.Where(x => x != null));
            }

            result.Subjects.Add(subject);
        }
        result.Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            Rule = "annual-v2", student.IdHocSinh, student.IdLopHoc, student.TrangThai, student.LopHoc.Khoi,
            year.IdNamHoc, Required = required,
            Terms = terms.Select(t => new { t.IdHocKy, t.NgayBatDau }),
            Grades = rows.Where(g => required.Contains(g.IdMonHoc))
                .OrderBy(g => g.IdMonHoc).ThenBy(g => g.IdHocKy).ThenBy(g => g.IdDiem)
                .Select(g => new { g.IdDiem, g.IdMonHoc, g.IdHocKy, g.IdNamHoc, g.Diem15Phut, g.Diem1Tiet, g.DiemGiuaKy, g.DiemCuoiKy })
        }))));
        if (required.Count == 0) { result.Reason = "Chưa phân công môn học cho lớp; chưa xác định đủ môn cần tổng kết."; return result; }
        if (result.Subjects.Count == 0 || result.Subjects.Any(s => !s.Annual.HasValue))
        { result.Reason = $"Còn {result.Subjects.Count(s => !s.Annual.HasValue)} môn thiếu điểm hợp lệ hoặc có dữ liệu trùng/sai kỳ."; return result; }
        result.Complete = true;
        result.Average = Math.Round(result.Subjects.Average(s => s.Annual!.Value), 2, MidpointRounding.AwayFromZero);
        if (!ParseGradeLevel(student.LopHoc.Khoi).HasValue)
        { result.Reason = "Đã đủ điểm; xét lên lớp hiện hỗ trợ khối 6–9."; return result; }
        result.Eligible = result.Average >= 5;
        result.Reason = result.Eligible ? "Đủ điều kiện học tập theo tiêu chí hiện tại: ĐTB năm từ 5,0."
            : "Chưa đạt điều kiện học tập: ĐTB năm dưới 5,0.";
        return result;
    }

    public async Task<List<AnnualStudentScore>> BuildAsync(IReadOnlyCollection<HocSinh> students)
    {
        if (students.Count == 0) return new();
        var ids = students.Select(s => s.IdHocSinh).ToList();
        var classIds = students.Where(s => s.IdLopHoc.HasValue).Select(s => s.IdLopHoc!.Value).Distinct().ToList();
        var years = await context.NamHocs.AsNoTracking().ToListAsync();
        var terms = await context.HocKys.AsNoTracking().ToListAsync();
        var assignments = await context.PhanCongGiangDays.AsNoTracking().Where(a => classIds.Contains(a.IdLop)).ToListAsync();
        var grades = await context.Diems.AsNoTracking().Where(g => ids.Contains(g.IdHocSinh)).ToListAsync();
        var subjects = await context.MonHocs.AsNoTracking().ToListAsync();
        var results = students.Select(student =>
        {
            var matches = years.Where(y => NormalizeYear(y.TenNamHoc) == NormalizeYear(student.LopHoc?.NamHoc)).ToList();
            return Evaluate(student, matches.Count == 1 ? matches[0] : null, terms, assignments, grades, subjects);
        }).ToList();
        var keys = results.Select(r => r.LogKey).ToList();
        var logs = await context.NhatKyHoatDongs.AsNoTracking().Where(l => keys.Contains(l.HanhDong))
            .OrderByDescending(l => l.IdNhatKy).ToListAsync();
        foreach (var result in results)
        {
            var log = logs.FirstOrDefault(l => l.HanhDong == result.LogKey);
            result.Finalized = result.Complete && log != null && log.NoiDung == result.Fingerprint;
            if (result.Finalized) result.FinalizedAt = log!.ThoiGian;
        }
        return results;
    }
}
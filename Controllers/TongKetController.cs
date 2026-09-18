using ClosedXML.Excel;
using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Controllers;

public sealed class AnnualScorePage
{
    public List<NamHoc> Years { get; set; } = new();
    public List<LopHoc> Classes { get; set; } = new();
    public int? YearId { get; set; }
    public int? ClassId { get; set; }
    public List<AnnualStudentScore> Students { get; set; } = new();
    public bool Locked { get; set; }
    public string Version => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(string.Join("|", Students.OrderBy(s => s.Student.IdHocSinh).Select(s => s.Fingerprint)))));
}

[RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
public sealed class TongKetController(AppDbContext context) : Controller
{
    private async Task<AnnualScorePage?> LoadAsync(int? yearId, int? classId)
    {
        var years = await context.NamHocs.AsNoTracking().OrderByDescending(y => y.NgayBatDau).ToListAsync();
        yearId ??= years.FirstOrDefault()?.IdNamHoc;
        var year = years.FirstOrDefault(y => y.IdNamHoc == yearId);
        if (yearId.HasValue && year == null) return null;
        var classes = await context.LopHocs.AsNoTracking().OrderBy(l => l.TenLop).ToListAsync();
        classes = classes.Where(l => year != null && AnnualScoreService.NormalizeYear(l.NamHoc) == AnnualScoreService.NormalizeYear(year.TenNamHoc)).ToList();
        if (HttpContext.Session.GetInt32("RoleId") == 2)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var teacher = await context.GiaoViens.AsNoTracking().FirstOrDefaultAsync(g => g.IdTaiKhoan == userId);
            if (teacher == null) return null;
            var allowed = await context.PhanCongGiangDays.AsNoTracking().Where(p => p.IdGiaoVien == teacher.IdGiaoVien)
                .Select(p => p.IdLop).Distinct().ToListAsync();
            classes = classes.Where(l => allowed.Contains(l.IdLop) || l.IdGiaoVienCN == teacher.IdGiaoVien).ToList();
        }
        if (classId.HasValue && classes.All(l => l.IdLop != classId)) return null;
        var page = new AnnualScorePage { Years = years, Classes = classes, YearId = yearId, ClassId = classId, Locked = PromotionLockService.IsLocked(context, yearId) };
        if (classId.HasValue)
        {
            var students = await context.HocSinhs.AsNoTracking().Include(h => h.LopHoc)
                .Where(h => h.IdLopHoc == classId && h.TrangThai).OrderBy(h => h.HoTen).ThenBy(h => h.MaHS).ToListAsync();
            page.Students = await new AnnualScoreService(context).BuildAsync(students);
        }
        return page;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? yearId, int? classId)
    {
        var page = await LoadAsync(yearId, classId);
        if (page == null) return BadRequest("Lớp/năm học không hợp lệ hoặc bạn không có quyền xem.");
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RoleAuthorize(SystemRoleIds.SystemAdmin)]
    public async Task<IActionResult> FinalizeClass(int yearId, int classId, string version)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var page = await LoadAsync(yearId, classId);
        if (page == null) return BadRequest();
        string? error = page.Locked ? "Kết quả đã khóa. Mở khóa trước khi tổng kết lại." :
            page.Students.Count == 0 ? "Lớp chưa có học sinh đang học." :
            page.Version != version ? "Dữ liệu đã thay đổi. Kiểm tra bảng điểm mới rồi tổng kết lại." :
            page.Students.Any(s => !s.Complete) ? "Còn học sinh thiếu dữ liệu hợp lệ. Chưa tổng kết học sinh nào." : null;
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index), new { yearId, classId });
        }
        var pending = page.Students.Where(s => !s.Finalized).ToList();
        var ids = pending.Select(s => s.Student.IdHocSinh).ToList();
        var students = await context.HocSinhs.Where(h => ids.Contains(h.IdHocSinh)).ToListAsync();
        foreach (var result in pending)
        {
            context.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                TenDangNhap = HttpContext.Session.GetString("Username") ?? "Admin",
                HanhDong = result.LogKey, NoiDung = result.Fingerprint
            });
            students.Single(s => s.IdHocSinh == result.Student.IdHocSinh).DaDuyet = false;
        }
        if (pending.Count > 0)
            context.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                TenDangNhap = HttpContext.Session.GetString("Username") ?? "Admin",
                HanhDong = "Tổng kết năm học",
                NoiDung = $"Lớp ID {classId}, năm ID {yearId}: {pending.Count} học sinh. " +
                    string.Join("; ", pending.Select(s => $"{s.Student.MaHS}: ĐTB {s.Average:0.00}, đạt học tập: {s.Eligible}"))
            });
        try { await context.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Chưa lưu được tổng kết. Vui lòng tải lại và thử lại.";
            return RedirectToAction(nameof(Index), new { yearId, classId });
        }
        TempData["Success"] = pending.Count == 0 ? "Kết quả đã tổng kết và còn khớp với điểm hiện tại." : $"Đã tổng kết {pending.Count} học sinh. Có thể chuyển sang xét lên lớp.";
        return RedirectToAction(nameof(Index), new { yearId, classId });
    }

    [HttpGet]
    public async Task<IActionResult> Export(int yearId, int classId)
    {
        var page = await LoadAsync(yearId, classId);
        if (page == null) return BadRequest();
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet("DiemCaNam");
        string[] headers = { "Mã HS", "Họ tên", "Lớp", "Năm học", "Môn", "HK1", "HK2", "Cả năm", "Tổng kết", "Điều kiện học tập" };
        for (var c = 0; c < headers.Length; c++) sheet.Cell(1, c + 1).Value = headers[c];
        var row = 2;
        foreach (var student in page.Students)
        foreach (var subject in student.Subjects)
        {
            sheet.Cell(row, 1).Value = student.Student.MaHS;
            sheet.Cell(row, 2).Value = student.Student.HoTen;
            sheet.Cell(row, 3).Value = student.Student.LopHoc?.TenLop;
            sheet.Cell(row, 4).Value = student.Student.LopHoc?.NamHoc;
            sheet.Cell(row, 5).Value = subject.Name;
            if (subject.Semester1.HasValue) sheet.Cell(row, 6).Value = subject.Semester1.Value;
            if (subject.Semester2.HasValue) sheet.Cell(row, 7).Value = subject.Semester2.Value;
            if (subject.Annual.HasValue) sheet.Cell(row, 8).Value = subject.Annual.Value;
            sheet.Cell(row, 9).Value = student.Finalized ? "Đã tổng kết" : "Chưa tổng kết / cần tổng kết lại";
            sheet.Cell(row, 10).Value = string.IsNullOrEmpty(subject.Error) ? student.Reason : subject.Error;
            row++;
        }
        eSchool.Infrastructure.ExcelHelper.ApplyTemplateStyle(sheet);
        sheet.Columns(6, 8).Style.NumberFormat.Format = "0.00";
        sheet.SheetView.FreezeRows(1); sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); book.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DiemCaNam_{yearId}_{classId}.xlsx");
    }
}

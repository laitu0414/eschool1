using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eSchool.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using eSchool.Infrastructure;

namespace eSchool.Controllers
{
    public sealed record FinalPromotionRow(int StudentId, string Code, string Name, string ClassName,
        int? Grade, decimal? Average, string Decision);
    public class LopStat
    {
        public string Khoi { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public int SiSo { get; set; }
        public int DaTongKet { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }

    public class KhoiStat
    {
        public string Khoi { get; set; } = string.Empty;
        public int TongSoHS { get; set; }
        public int DuDieuKien { get; set; }
        public int ChuaDuDieuKien { get; set; }
        public int BoHoc { get; set; }
        public double TyLe { get; set; }
    }

    public class PromotionAssessment
    {
        public bool IsComplete { get; set; }
        public bool IsEligible { get; set; }
        public decimal? AnnualAverage { get; set; }
        public string Decision { get; set; } = "Chưa xét";
        public string Reason { get; set; } = string.Empty;
    }

    [RoleAuthorize(SystemRoleIds.SystemAdmin)]
    public class LenLopController : Controller
    {
        private readonly AppDbContext _context;

        public LenLopController(AppDbContext context)
        {
            _context = context;
        }

        private NamHoc? _processingYear;
        private bool _yearResolved;
        private NamHoc? ResolveProcessingYear()
        {
            if (_yearResolved) return _processingYear;
            _yearResolved = true;
            var raw = Request.HasFormContentType ? Request.Form["yearId"].ToString() : Request.Query["yearId"].ToString();
            var years = _context.NamHocs.AsNoTracking().OrderByDescending(y => y.NgayBatDau).ToList();
            _processingYear = string.IsNullOrEmpty(raw)
                ? years.FirstOrDefault(y => y.TrangThai) ?? years.FirstOrDefault()
                : int.TryParse(raw, out var id) ? years.FirstOrDefault(y => y.IdNamHoc == id) : null;
            return _processingYear;
        }
        private Task<NamHoc?> GetProcessingYearAsync() => Task.FromResult(ResolveProcessingYear());
        private async Task<IQueryable<HocSinh>> ProcessingStudentsAsync()
        {
            var year = await GetProcessingYearAsync();
            if (year == null) return _context.HocSinhs.Where(h => false);
            var name = eSchool.Services.AnnualScoreService.NormalizeYear(year.TenNamHoc);
            return _context.HocSinhs.Where(h => !h.DaTotNghiep && h.LopHoc != null && h.LopHoc.NamHoc != null &&
                h.LopHoc.NamHoc.Replace(" ", "") == name);
        }
        public async Task<IActionResult> Index()
        {
            var academicYears = await _context.NamHocs.AsNoTracking()
                .OrderByDescending(y => y.NgayBatDau).ToListAsync();
            var processingYear = await GetProcessingYearAsync();
            if (processingYear == null && Request.Query.ContainsKey("yearId")) return BadRequest("Năm học không hợp lệ.");
            var processingYearName = processingYear?.TenNamHoc;
            var normalizedYear = eSchool.Services.AnnualScoreService.NormalizeYear(processingYearName);

            var allHocSinh = await _context.HocSinhs.Include(h => h.LopHoc)
                .Where(h => !h.DaTotNghiep && h.LopHoc != null && processingYear != null && h.LopHoc.NamHoc != null && h.LopHoc.NamHoc.Replace(" ", "") == normalizedYear)
                .ToListAsync();
            var allLops = await _context.LopHocs
                .Where(l => processingYear != null && l.NamHoc != null && l.NamHoc.Replace(" ", "") == normalizedYear)
                .ToListAsync();
            ViewBag.AcademicYears = academicYears;
            ViewBag.ProcessingYear = processingYearName;
            ViewBag.ProcessingYearId = processingYear?.IdNamHoc;
            var assessments = await BuildPromotionAssessmentsAsync(allHocSinh);

            int totalHS = allHocSinh.Count;
            int activeHS = allHocSinh.Count(h => h.TrangThai);
            int inactiveHS = allHocSinh.Count(h => !h.TrangThai); // Bỏ học/chuyển trường

            int chuaDuDieuKien = allHocSinh.Count(h => h.TrangThai &&
                (!assessments.TryGetValue(h.IdHocSinh, out var assessment) || !assessment.IsEligible));
            int duDieuKien = allHocSinh.Count(h => assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsEligible);
            int boHoc = inactiveHS;

            ViewBag.TotalHS = totalHS;
            ViewBag.DuDieuKien = duDieuKien;
            ViewBag.ChuaDuDieuKien = chuaDuDieuKien;
            ViewBag.BoHoc = boHoc;

            int daDuyet = allHocSinh.Count(h => h.TrangThai && h.DaDuyet &&
                assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsEligible);
            int choDuyet = allHocSinh.Count(h => !h.DaDuyet &&
                assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsEligible);
            ViewBag.DaDuyet = daDuyet;
            ViewBag.ChoDuyet = choDuyet;
            ViewBag.TyLeDuyet = duDieuKien > 0 ? Math.Round((double)daDuyet / duDieuKien * 100, 2) : 0;
            ViewBag.IsLocked = IsResultsLocked();
            var snapshotKey = $"LenLop.FinalResults:{processingYear?.IdNamHoc}";
            var snapshot = await _context.NhatKyHoatDongs.AsNoTracking().Where(l => l.HanhDong == snapshotKey)
                .OrderByDescending(l => l.IdNhatKy).FirstOrDefaultAsync();
            ViewBag.FinalResults = ReadFinalResults(snapshot?.NoiDung);
            ViewBag.FinalizedAt = snapshot?.ThoiGian;
            ViewBag.GraduatedCount = await _context.HocSinhs.CountAsync(h => h.DaTotNghiep && h.NamHocTotNghiep == processingYearName);

            int chuaTongKet = allHocSinh.Count(h => h.TrangThai &&
                (!assessments.TryGetValue(h.IdHocSinh, out var assessment) || !assessment.IsComplete));
            int daTongKet = activeHS - chuaTongKet;
            ViewBag.DaTongKet = daTongKet;
            ViewBag.ChuaTongKet = chuaTongKet;

            var lopStats = allLops.Select(l =>
            {
                int siso = allHocSinh.Count(h => h.IdLopHoc == l.IdLop);
                int ctk = allHocSinh.Count(h => h.IdLopHoc == l.IdLop && h.TrangThai &&
                    (!assessments.TryGetValue(h.IdHocSinh, out var assessment) || !assessment.IsComplete));
                int completed = allHocSinh.Count(h => h.IdLopHoc == l.IdLop && h.TrangThai &&
                    assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsComplete);
                return new LopStat
                {
                    Khoi = l.Khoi ?? "",
                    TenLop = l.TenLop,
                    SiSo = siso,
                    DaTongKet = completed,
                    TrangThai = ctk == 0 ? "Đã hoàn thành" : "Chưa hoàn thành"
                };
            }).OrderBy(l => l.Khoi).ThenBy(l => l.TenLop).ToList();
            ViewBag.LopStats = lopStats;

            var khoiStats = allHocSinh.Where(h => h.LopHoc != null).GroupBy(h => h.LopHoc.Khoi).Select(g =>
            {
                int total = g.Count();
                int cdk = g.Count(h => h.TrangThai &&
                    (!assessments.TryGetValue(h.IdHocSinh, out var assessment) || !assessment.IsEligible));
                int bh = g.Count(h => !h.TrangThai);
                int dk = g.Count(h => assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsEligible);
                return new KhoiStat
                {
                    Khoi = g.Key,
                    TongSoHS = total,
                    DuDieuKien = dk,
                    ChuaDuDieuKien = cdk,
                    BoHoc = bh,
                    TyLe = (total > 0) ? Math.Round((double)dk / total * 100, 2) : 0
                };
            }).OrderBy(g => g.Khoi).ToList();
            ViewBag.KhoiStats = khoiStats;

            ViewBag.HocSinhs = allHocSinh.OrderBy(h => h.LopHoc?.TenLop).ThenBy(h => h.HoTen).ToList();
            ViewBag.PromotionAssessments = assessments;

            var studentIds = allHocSinh.Select(h => h.IdHocSinh).ToList();
            ViewBag.NextYearRegistrations = await _context.DangKyLops.AsNoTracking()
                .Include(d => d.HocSinh).ThenInclude(h => h.LopHoc)
                .Include(d => d.LopHoc)
                .Where(d => studentIds.Contains(d.IdHocSinh) && d.HocSinh != null && d.HocSinh.LopHoc != null &&
                    d.LopHoc != null && d.LopHoc.NamHoc != d.HocSinh.LopHoc.NamHoc)
                .OrderBy(d => d.LopHoc.NamHoc).ThenBy(d => d.LopHoc.TenLop).ThenBy(d => d.HocSinh.HoTen).ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStudent(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var hs = await _context.HocSinhs.Include(h => h.LopHoc).FirstOrDefaultAsync(h => h.IdHocSinh == id);
            if (hs == null) return NotFound();
            if (hs != null)
            {
                var assessments = await BuildPromotionAssessmentsAsync(new[] { hs });
                if (!assessments.TryGetValue(hs.IdHocSinh, out var assessment) || !assessment.IsEligible)
                {
                    TempData["Error"] = $"Học sinh {hs.MaHS} không đủ điều kiện để duyệt.";
                    return RedirectToAction(nameof(Index));
                }

                hs.DaDuyet = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã duyệt học sinh {hs.MaHS}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectStudent(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var hs = await _context.HocSinhs.Include(h => h.LopHoc).FirstOrDefaultAsync(h => h.IdHocSinh == id);
            if (hs == null) return NotFound();
            if (hs != null)
            {
                hs.DaDuyet = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã hủy duyệt học sinh {hs.MaHS}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(string ids)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var idList = ParseStudentIds(ids);
            if (!idList.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một học sinh hợp lệ để duyệt.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            
            var students = await _context.HocSinhs
                .Include(h => h.LopHoc)
                .Where(h => idList.Contains(h.IdHocSinh))
                .ToListAsync();
            var assessments = await BuildPromotionAssessmentsAsync(students);
            var eligibleStudents = students
                .Where(h => assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsEligible)
                .ToList();

            foreach (var hs in eligibleStudents)
            {
                hs.DaDuyet = true;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = eligibleStudents.Any()
                ? $"Đã duyệt {eligibleStudents.Count} học sinh đủ điều kiện."
                : "Không có học sinh đủ điều kiện trong danh sách đã chọn.";
            return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReject(string ids)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var idList = ParseStudentIds(ids);
            if (!idList.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một học sinh hợp lệ để từ chối.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            
            var students = await _context.HocSinhs
                .Where(h => idList.Contains(h.IdHocSinh) && h.TrangThai)
                .ToListAsync();
            foreach (var hs in students)
            {
                hs.DaDuyet = false;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = $"Đã hủy duyệt {students.Count} học sinh.";
            return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApproveAllEligible()
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var processingYear = await GetProcessingYearAsync();
            if (processingYear == null)
            {
                TempData["Error"] = "Chưa có năm học để xét lên lớp.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            var candidates = await (await ProcessingStudentsAsync()).Include(h => h.LopHoc)
                .Where(h => h.TrangThai && !h.DaDuyet && h.LopHoc != null &&
                    h.LopHoc.NamHoc != null && h.LopHoc.NamHoc.Replace(" ", "") == processingYear.TenNamHoc.Replace(" ", "")).ToListAsync();
            var assessments = await BuildPromotionAssessmentsAsync(candidates);
            var students = candidates.Where(h => assessments[h.IdHocSinh].IsEligible).ToList();

            foreach (var hs in students)
            {
                hs.DaDuyet = true;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = $"Đã duyệt tất cả học sinh đủ điều kiện ({students.Count}).";
            return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockResults()
        {
            if (IsResultsLocked()) return RedirectWithLockedResultsMessage();
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var students = await _context.HocSinhs.Include(h => h.LopHoc).Where(h => h.TrangThai).ToListAsync();
            var assessments = await BuildPromotionAssessmentsAsync(students);
            if (students.Count == 0 || assessments.Values.Any(a => !a.IsComplete))
            {
                TempData["Error"] = "Chưa có học sinh hoặc còn thiếu dữ liệu tổng kết. Không thể khóa kết quả.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            if (students.Any(h => assessments[h.IdHocSinh].IsEligible && !h.DaDuyet))
            {
                TempData["Error"] = "Còn học sinh đủ điều kiện chưa được duyệt.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            foreach (var student in students.Where(h => !assessments[h.IdHocSinh].IsEligible))
                student.DaDuyet = false;
            RecordLockState(true);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Đã khóa kết quả xét lên lớp / tốt nghiệp.";
            return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
        }

        private bool IsResultsLocked()
        {
            var year = ResolveProcessingYear();
            if (year == null) return false;
            var key = eSchool.Services.PromotionLockService.Key(year.IdNamHoc);
            return _context.NhatKyHoatDongs.AsNoTracking()
                .Where(x => x.HanhDong == key)
                .OrderByDescending(x => x.IdNhatKy).Select(x => x.NoiDung).FirstOrDefault() == bool.TrueString;
        }

        private void RecordLockState(bool locked)
        {
            var year = ResolveProcessingYear() ?? throw new InvalidOperationException("Năm học không hợp lệ.");
            _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                TenDangNhap = HttpContext.Session.GetString("Username") ?? "Admin",
                HanhDong = eSchool.Services.PromotionLockService.Key(year.IdNamHoc), NoiDung = locked.ToString()
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockResults()
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (ResolveProcessingYear() == null) return BadRequest("Năm học không hợp lệ.");
            var year = ResolveProcessingYear()!;
            if (await _context.HocSinhs.AnyAsync(h => h.DaTotNghiep && h.NamHocTotNghiep == year.TenNamHoc))
            {
                TempData["Error"] = "Năm học đã xác nhận tốt nghiệp. Không thể mở khóa kết quả bằng thao tác thông thường.";
                return RedirectToAction(nameof(Index), new { yearId = year.IdNamHoc });
            }
            RecordLockState(false);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Đã mở khóa kết quả.";
            return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
        }

        private IActionResult RedirectWithLockedResultsMessage()
        {
            TempData["Error"] = "Kết quả đã được khóa, không thể thay đổi.";
            return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmGraduation(bool confirmed)
        {
            var year = ResolveProcessingYear();
            if (year == null) return BadRequest("Năm học không hợp lệ.");
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            IActionResult Fail(string message)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Index), new { yearId = year.IdNamHoc });
            }
            if (!confirmed) return Fail("Cần xác nhận đã kiểm tra các điều kiện tốt nghiệp ngoài điểm học tập.");
            if (!IsResultsLocked()) return Fail("Phải khóa kết quả trước khi xác nhận tốt nghiệp.");
            var key = $"LenLop.FinalResults:{year.IdNamHoc}";
            var log = await _context.NhatKyHoatDongs.AsNoTracking().Where(l => l.HanhDong == key)
                .OrderByDescending(l => l.IdNhatKy).FirstOrDefaultAsync();
            var approved = ReadFinalResults(log?.NoiDung).Where(r => r.Grade == 9 && r.Decision == "Đã duyệt điều kiện học tập tốt nghiệp").ToList();
            if (approved.Count == 0) return Fail("Chưa có học sinh khối 9 đã duyệt trong danh sách chốt.");
            var ids = approved.Select(r => r.StudentId).Distinct().ToList();
            var students = await _context.HocSinhs.Include(h => h.LopHoc).Where(h => ids.Contains(h.IdHocSinh)).ToListAsync();
            if (students.Count != ids.Count) return Fail("Danh sách học sinh đã thay đổi. Chưa xác nhận tốt nghiệp.");
            if (students.Any(h => h.DaTotNghiep && h.NamHocTotNghiep != year.TenNamHoc))
                return Fail("Có học sinh đã tốt nghiệp ở năm học khác.");
            var pending = students.Where(h => !h.DaTotNghiep).ToList();
            var assessments = await BuildPromotionAssessmentsAsync(pending);
            if (pending.Any(h => !h.TrangThai || !h.DaDuyet || eSchool.Services.AnnualScoreService.ParseGradeLevel(h.LopHoc?.Khoi) != 9 ||
                eSchool.Services.AnnualScoreService.NormalizeYear(h.LopHoc?.NamHoc) != eSchool.Services.AnnualScoreService.NormalizeYear(year.TenNamHoc) ||
                !assessments[h.IdHocSinh].IsEligible || assessments[h.IdHocSinh].AnnualAverage != approved.First(r => r.StudentId == h.IdHocSinh).Average))
                return Fail("Dữ liệu học sinh không còn khớp với kết quả đã chốt. Chưa xác nhận học sinh nào.");
            var confirmedAt = DateTime.Now;
            foreach (var student in pending)
                eSchool.Services.GraduationService.MarkGraduated(student, year.TenNamHoc, confirmedAt);
            if (pending.Count > 0) _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                TenDangNhap = HttpContext.Session.GetString("Username") ?? "Admin",
                HanhDong = "Xác nhận tốt nghiệp",
                NoiDung = $"Năm {year.TenNamHoc}, bản chốt {log!.IdNhatKy}; đã kiểm tra điều kiện ngoài học tập. Học sinh: {string.Join(", ", pending.Select(h => h.MaHS))}."
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = pending.Count == 0 ? "Danh sách đã được xác nhận tốt nghiệp trước đó." : $"Đã xác nhận tốt nghiệp {pending.Count} học sinh. Hồ sơ và điểm được giữ nguyên.";
            return RedirectToAction(nameof(Alumni), new { yearId = year.IdNamHoc });
        }

        [HttpGet]
        public async Task<IActionResult> Alumni(int? yearId, string? keyword)
        {
            var years = await _context.NamHocs.AsNoTracking().OrderByDescending(y => y.NgayBatDau).ToListAsync();
            var year = years.FirstOrDefault(y => y.IdNamHoc == yearId);
            if (yearId.HasValue && year == null) return BadRequest("Năm học không hợp lệ.");
            var query = _context.HocSinhs.AsNoTracking().Include(h => h.LopHoc).Where(h => h.DaTotNghiep);
            if (year != null) query = query.Where(h => h.NamHocTotNghiep == year.TenNamHoc);
            if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(h => h.MaHS.Contains(keyword) || h.HoTen.Contains(keyword));
            ViewBag.Years = years; ViewBag.YearId = yearId; ViewBag.Keyword = keyword;
            return View(await query.OrderByDescending(h => h.NgayTotNghiep).ThenBy(h => h.HoTen).ToListAsync());
        }
        private static List<FinalPromotionRow> ReadFinalResults(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new();
            try { return System.Text.Json.JsonSerializer.Deserialize<List<FinalPromotionRow>>(json) ?? new(); }
            catch (System.Text.Json.JsonException) { return new(); }
        }

        [HttpGet]
        public async Task<IActionResult> ExportFinalResults()
        {
            var year = ResolveProcessingYear();
            if (year == null) return BadRequest("Năm học không hợp lệ.");
            var key = $"LenLop.FinalResults:{year.IdNamHoc}";
            var log = await _context.NhatKyHoatDongs.AsNoTracking().Where(l => l.HanhDong == key)
                .OrderByDescending(l => l.IdNhatKy).FirstOrDefaultAsync();
            var results = ReadFinalResults(log?.NoiDung);
            if (results.Count == 0) return NotFound("Năm học chưa có danh sách chốt.");
            using var book = new XLWorkbook();
            var sheet = book.AddWorksheet("KetQuaDaChot");
            sheet.Cell(1, 1).Value = $"Kết quả chốt năm {year.TenNamHoc}, lúc {log!.ThoiGian:dd/MM/yyyy HH:mm}";
            sheet.Cell(2, 1).Value = IsResultsLocked() ? "Kết quả đang khóa" : "Bản chốt trước khi mở khóa; cần chốt lại để có kết quả mới";
            string[] headers = { "Mã HS", "Họ tên", "Lớp", "Khối", "ĐTB năm", "Kết quả" };
            for (var i = 0; i < headers.Length; i++) sheet.Cell(4, i + 1).Value = headers[i];
            var row = 5;
            foreach (var result in results)
            {
                sheet.Cell(row, 1).Value = result.Code; sheet.Cell(row, 2).Value = result.Name;
                sheet.Cell(row, 3).Value = result.ClassName;
                if (result.Grade.HasValue) sheet.Cell(row, 4).Value = result.Grade.Value;
                if (result.Average.HasValue) sheet.Cell(row, 5).Value = result.Average.Value;
                sheet.Cell(row++, 6).Value = result.Decision;
            }
            sheet.Row(4).Style.Font.Bold = true;
            sheet.Column(5).Style.NumberFormat.Format = "0.00";
            sheet.Columns().AdjustToContents();
            using var stream = new MemoryStream(); book.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KetQuaDaChot_{year.IdNamHoc}.xlsx");
        }
        private static List<int> ParseStudentIds(string? ids) =>
            string.IsNullOrWhiteSpace(ids)
                ? new List<int>()
                : ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => int.TryParse(value, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

        private async Task<Dictionary<int, PromotionAssessment>> BuildPromotionAssessmentsAsync(IReadOnlyCollection<HocSinh> students)
        {
            var annual = await new eSchool.Services.AnnualScoreService(_context).BuildAsync(students);
            return annual.ToDictionary(r => r.Student.IdHocSinh, r => new PromotionAssessment
            {
                AnnualAverage = r.Average,
                IsComplete = !r.Student.TrangThai || (r.Complete && r.Finalized),
                IsEligible = r.Student.TrangThai && r.Eligible && r.Finalized,
                Decision = !r.Student.TrangThai ? "Không xét" : !r.Complete ? "Chưa đủ dữ liệu" :
                    !r.Finalized ? "Chưa tổng kết / cần tổng kết lại" :
                    r.Eligible ? (eSchool.Services.AnnualScoreService.ParseGradeLevel(r.Student.LopHoc?.Khoi) == 9 ? "Đạt điều kiện học tập tốt nghiệp" : "Đạt điều kiện học tập lên lớp") : "Chưa đủ điều kiện",
                Reason = r.Reason + (r.Student.TrangThai && r.Complete && !r.Finalized
                    ? " Hãy tổng kết tại Điểm cả năm trước khi duyệt." : "")
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoAssignClass(string NamHoc, string Khoi, string TrangThai)
        {
            if (!IsResultsLocked())
            {
                TempData["Error"] = "Vui lòng khóa kết quả trước khi xếp lớp.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (!IsResultsLocked()) return RedirectWithLockedResultsMessage();
            var targetYear = await _context.NamHocs.SingleOrDefaultAsync(y => y.TenNamHoc == NamHoc);
            if (targetYear == null)
            {
                TempData["Error"] = "Năm học đích không tồn tại.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            var processingYear = await GetProcessingYearAsync();
            var years = await _context.NamHocs.AsNoTracking().ToListAsync();
            var sourceYear = years.Where(y => y.NgayBatDau < targetYear.NgayBatDau)
                .OrderByDescending(y => y.NgayBatDau).FirstOrDefault();
            if (sourceYear == null || sourceYear.IdNamHoc != processingYear?.IdNamHoc || sourceYear.NgayKetThuc >= targetYear.NgayBatDau)
            {
                TempData["Error"] = "Không xác định được năm học liền trước hợp lệ.";
                return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
            }
            var students = await _context.HocSinhs.Include(h => h.LopHoc)
                .Where(h => h.TrangThai && h.DaDuyet && h.LopHoc != null && h.LopHoc.NamHoc != null && h.LopHoc.NamHoc.Replace(" ", "") == sourceYear.TenNamHoc.Replace(" ", ""))
                .OrderBy(h => h.MaHS).ToListAsync();
            if (!string.IsNullOrWhiteSpace(Khoi) && Khoi != "All")
                students = students.Where(h => h.LopHoc!.Khoi == Khoi).ToList();
            var assessments = await BuildPromotionAssessmentsAsync(students);
            students = students.Where(h => assessments[h.IdHocSinh].IsEligible &&
                eSchool.Services.AnnualScoreService.ParseGradeLevel(h.LopHoc!.Khoi) is >= 6 and < 9).ToList();
            var targets = await _context.LopHocs.Where(l => l.NamHoc != null && l.NamHoc.Replace(" ", "") == NamHoc.Replace(" ", "")).OrderBy(l => l.TenLop).ToListAsync();
            var memberships = await _context.DangKyLops.Where(d => d.LopHoc!.NamHoc != null && d.LopHoc.NamHoc.Replace(" ", "") == NamHoc.Replace(" ", ""))
                .Select(d => new { StudentId = d.IdHocSinh, ClassId = d.IdLop })
                .Union(_context.HocSinhs.Where(h => h.IdLopHoc.HasValue && h.LopHoc!.NamHoc != null && h.LopHoc.NamHoc.Replace(" ", "") == NamHoc.Replace(" ", ""))
                    .Select(h => new { StudentId = h.IdHocSinh, ClassId = h.IdLopHoc!.Value })).ToListAsync();
            var counts = memberships.GroupBy(m => m.ClassId).ToDictionary(g => g.Key, g => g.Count());
            var registered = memberships.Select(m => m.StudentId).ToHashSet();
            foreach (var student in students)
            {
                var membershipsForStudent = memberships.Where(m => m.StudentId == student.IdHocSinh).ToList();
                var requiredGrade = eSchool.Services.AnnualScoreService.ParseGradeLevel(student.LopHoc?.Khoi) + 1;
                if (membershipsForStudent.Count > 1 || membershipsForStudent.Any(m =>
                    eSchool.Services.AnnualScoreService.ParseGradeLevel(targets.FirstOrDefault(t => t.IdLop == m.ClassId)?.Khoi) != requiredGrade))
                {
                    TempData["Error"] = $"Học sinh {student.MaHS} đã đăng ký nhiều lớp hoặc sai khối ở năm đích. Chưa lưu xếp lớp.";
                    return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
                }
            }            var assigned = 0;
            foreach (var student in students)
            {
                if (registered.Contains(student.IdHocSinh)) continue;
                var nextGrade = eSchool.Services.AnnualScoreService.ParseGradeLevel(student.LopHoc!.Khoi)!.Value + 1;
                var target = targets.Where(l => eSchool.Services.AnnualScoreService.ParseGradeLevel(l.Khoi) == nextGrade)
                    .OrderBy(l => counts.GetValueOrDefault(l.IdLop)).ThenBy(l => l.TenLop).FirstOrDefault();
                if (target == null)
                {
                    TempData["Error"] = $"Chưa có lớp khối {nextGrade} trong năm học {NamHoc}. Chưa lưu xếp lớp.";
                    return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
                }
                _context.DangKyLops.Add(new DangKyLop { IdHocSinh = student.IdHocSinh, IdLop = target.IdLop });
                counts[target.IdLop] = counts.GetValueOrDefault(target.IdLop) + 1;
                assigned++;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = $"Đã đăng ký lớp năm học mới cho {assigned} học sinh. Lớp hiện tại được giữ để bảo toàn kết quả năm đang xét.";
            return RedirectToAction(nameof(Index), new { yearId = ResolveProcessingYear()?.IdNamHoc });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintResults(string NamHoc, string Khoi, string Lop, string LoaiKetQua, string HinhThucIn, string MauIn)
        {
            var query = _context.HocSinhs.Include(h => h.LopHoc).AsQueryable();
            if (!string.IsNullOrWhiteSpace(NamHoc)) query = query.Where(h => h.LopHoc != null && h.LopHoc.NamHoc == NamHoc);
            if (!string.IsNullOrEmpty(Khoi)) query = query.Where(h => h.LopHoc != null && h.LopHoc.Khoi == Khoi);
            if (!string.IsNullOrEmpty(Lop)) query = query.Where(h => h.LopHoc != null && h.LopHoc.TenLop == Lop);
            var students = await query.OrderBy(h => h.LopHoc.TenLop).ThenBy(h => h.HoTen).ToListAsync();

            if (MauIn == "Danh sách tốt nghiệp lớp 9") students = students.Where(h => eSchool.Services.AnnualScoreService.ParseGradeLevel(h.LopHoc?.Khoi) == 9).ToList();
            var assessments = await BuildPromotionAssessmentsAsync(students);
            if (LoaiKetQua == "approved") students = students.Where(h => h.DaDuyet && assessments[h.IdHocSinh].IsEligible).ToList();
            if (LoaiKetQua == "eligible") students = students.Where(h => assessments[h.IdHocSinh].IsEligible).ToList();
            if (LoaiKetQua == "ineligible") students = students.Where(h => !assessments[h.IdHocSinh].IsEligible).ToList();
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Text(MauIn ?? "Danh sách kết quả").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        x.Spacing(10);
                        x.Item().Text($"Năm học: {NamHoc}");
                        x.Item().Text($"Khối: {Khoi}");
                        x.Item().Text($"Lớp: {Lop}");
                        x.Item().Text($"Loại kết quả: {LoaiKetQua}");
                        x.Item().PaddingTop(20).Text("Danh sách học sinh").Bold();

                        int i = 1;
                        foreach (var hs in students)
                        {
                            var assessment = assessments[hs.IdHocSinh];
                            string trangThai = assessment.Decision + (hs.DaDuyet && assessment.IsEligible ? " - Đã duyệt" : " - Chưa duyệt");
                            string lopName = hs.LopHoc != null ? hs.LopHoc.TenLop : "";
                            x.Item().Text($"{i++}. {hs.MaHS} - {hs.HoTen} - Lớp {lopName} - {trangThai}");
                        }
                    });
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            if (HinhThucIn == "Preview")
            {
                return File(pdfBytes, "application/pdf");
            }
            return File(pdfBytes, "application/pdf", "DanhSachKetQua.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcel(string NamHoc, string Khoi, string Lop, string LoaiDuLieu, string DinhDangFile, List<string> BaoGom)
        {
            var query = _context.HocSinhs.Include(h => h.LopHoc).AsQueryable();
            if (!string.IsNullOrWhiteSpace(NamHoc)) query = query.Where(h => h.LopHoc != null && h.LopHoc.NamHoc == NamHoc);
            if (!string.IsNullOrEmpty(Khoi)) query = query.Where(h => h.LopHoc != null && h.LopHoc.Khoi == Khoi);
            if (!string.IsNullOrEmpty(Lop)) query = query.Where(h => h.LopHoc != null && h.LopHoc.TenLop == Lop);
            var students = await query.OrderBy(h => h.LopHoc.TenLop).ThenBy(h => h.HoTen).ToListAsync();

            var assessments = await BuildPromotionAssessmentsAsync(students);
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("KetQua");
            worksheet.Cell(1, 1).Value = "Kết quả xét lên lớp - " + NamHoc;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;

            int col = 1;
            if (BaoGom != null && BaoGom.Count > 0)
            {
                foreach (var field in BaoGom)
                {
                    worksheet.Cell(3, col).Value = field;
                    
                    
                    col++;
                }
            }
            else
            {
                worksheet.Cell(3, 1).Value = "Mã HS";
                worksheet.Cell(3, 2).Value = "Họ tên";
                worksheet.Cell(3, 3).Value = "Lớp";
                worksheet.Cell(3, 4).Value = "Trạng thái";
            }

            int row = 4;
            foreach (var hs in students)
            {
                var assessment = assessments[hs.IdHocSinh];
                string trangThai = assessment.Decision + (hs.DaDuyet && assessment.IsEligible ? " - Đã duyệt" : " - Chưa duyệt");
                if (BaoGom != null && BaoGom.Count > 0)
                {
                    int c = 1;
                    foreach (var field in BaoGom)
                    {
                        if (field == "Thông tin học sinh") worksheet.Cell(row, c).Value = $"{hs.MaHS} - {hs.HoTen} - {hs.LopHoc?.TenLop}";
                        else if (field == "Kết quả học tập") worksheet.Cell(row, c).Value = assessment.AnnualAverage?.ToString("0.00") ?? "Chưa đủ dữ liệu";
                        else if (field == "Kết quả rèn luyện") worksheet.Cell(row, c).Value = "Chưa có dữ liệu";
                        else if (field == "Điều kiện xét") worksheet.Cell(row, c).Value = assessment.Reason;
                        else if (field == "Ghi chú") worksheet.Cell(row, c).Value = hs.GhiChu ?? "";
                        else if (field.Contains("Mã", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = hs.MaHS;
                        else if (field.Contains("Tên", StringComparison.OrdinalIgnoreCase) || field.Contains("Họ", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = hs.HoTen;
                        else if (field.Contains("Lớp", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = hs.LopHoc?.TenLop;
                        else if (field.Contains("Trạng", StringComparison.OrdinalIgnoreCase) || field.Contains("Kết quả", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = trangThai;
                        else worksheet.Cell(row, c).Value = "";
                        c++;
                    }
                }
                else
                {
                    worksheet.Cell(row, 1).Value = hs.MaHS;
                    worksheet.Cell(row, 2).Value = hs.HoTen;
                    worksheet.Cell(row, 3).Value = hs.LopHoc?.TenLop;
                    worksheet.Cell(row, 4).Value = trangThai;
                }
                row++;
            }

            worksheet.Columns().AdjustToContents();

            eSchool.Infrastructure.ExcelHelper.ApplyTemplateStyle(worksheet, 3);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KetQua_{NamHoc}.xlsx");
        }
    }
}


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
        private const string ResultsLockedSessionKey = "LenLop.ResultsLocked";
        private readonly AppDbContext _context;

        public LenLopController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var allHocSinh = await _context.HocSinhs.Include(h => h.LopHoc).ToListAsync();
            var allLops = await _context.LopHocs.ToListAsync();
            var assessments = await BuildPromotionAssessmentsAsync(allHocSinh);

            int totalHS = allHocSinh.Count;
            int activeHS = allHocSinh.Count(h => h.TrangThai);
            int inactiveHS = allHocSinh.Count(h => !h.TrangThai); // Bo hoc/chuyen truong

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

            int chuaTongKet = allHocSinh.Count(h => h.TrangThai &&
                (!assessments.TryGetValue(h.IdHocSinh, out var assessment) || !assessment.IsComplete));
            int daTongKet = activeHS - chuaTongKet;
            ViewBag.DaTongKet = daTongKet;
            ViewBag.ChuaTongKet = chuaTongKet;

            var lopStats = allLops.Select(l => {
                int siso = allHocSinh.Count(h => h.IdLopHoc == l.IdLop);
                int ctk = allHocSinh.Count(h => h.IdLopHoc == l.IdLop && h.TrangThai &&
                    (!assessments.TryGetValue(h.IdHocSinh, out var assessment) || !assessment.IsComplete));
                int completed = allHocSinh.Count(h => h.IdLopHoc == l.IdLop && h.TrangThai &&
                    assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsComplete);
                return new LopStat {
                    Khoi = l.Khoi ?? "",
                    TenLop = l.TenLop,
                    SiSo = siso,
                    DaTongKet = completed,
                    TrangThai = ctk == 0 ? "Đã hoàn thành" : "Chưa hoàn thành"
                };
            }).OrderBy(l => l.Khoi).ThenBy(l => l.TenLop).ToList();
            ViewBag.LopStats = lopStats;

            // Stats for Step 2
            var khoiStats = allHocSinh.Where(h => h.LopHoc != null).GroupBy(h => h.LopHoc.Khoi).Select(g => {
                int total = g.Count();
                int cdk = g.Count(h => h.TrangThai &&
                    (!assessments.TryGetValue(h.IdHocSinh, out var assessment) || !assessment.IsEligible));
                int bh = g.Count(h => !h.TrangThai);
                int dk = g.Count(h => assessments.TryGetValue(h.IdHocSinh, out var assessment) && assessment.IsEligible);
                return new KhoiStat {
                    Khoi = g.Key,
                    TongSoHS = total,
                    DuDieuKien = dk,
                    ChuaDuDieuKien = cdk,
                    BoHoc = bh,
                    TyLe = (total > 0) ? Math.Round((double)dk / total * 100, 2) : 0
                };
            }).OrderBy(g => g.Khoi).ToList();
            ViewBag.KhoiStats = khoiStats;

            // List of students for Step 3
            ViewBag.HocSinhs = allHocSinh.OrderBy(h => h.LopHoc?.TenLop).ThenBy(h => h.HoTen).ToList();
            ViewBag.PromotionAssessments = assessments;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStudent(int id)
        {
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var hs = await _context.HocSinhs
                .Include(h => h.LopHoc)
                .FirstOrDefaultAsync(h => h.IdHocSinh == id);
            if (hs != null)
            {
                var assessment = (await BuildPromotionAssessmentsAsync(new[] { hs }))[hs.IdHocSinh];
                if (!assessment.IsEligible)
                {
                    TempData["Error"] = $"Học sinh {hs.MaHS} không đủ điều kiện để duyệt.";
                    return RedirectToAction(nameof(Index));
                }

                hs.DaDuyet = true;
                hs.GhiChu = "Đã duyệt";
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã duyệt học sinh {hs.MaHS}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectStudent(int id)
        {
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var hs = await _context.HocSinhs.FindAsync(id);
            if (hs != null)
            {
                hs.DaDuyet = false;
                hs.GhiChu = "Không đủ điều kiện";
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã từ chối học sinh {hs.MaHS}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(string ids)
        {
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var idList = ParseStudentIds(ids);
            if (!idList.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một học sinh hợp lệ để duyệt.";
                return RedirectToAction(nameof(Index));
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
                hs.GhiChu = "Đã duyệt nhóm";
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = eligibleStudents.Any()
                ? $"Đã duyệt {eligibleStudents.Count} học sinh đủ điều kiện."
                : "Không có học sinh đủ điều kiện trong danh sách đã chọn.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReject(string ids)
        {
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var idList = ParseStudentIds(ids);
            if (!idList.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một học sinh hợp lệ để từ chối.";
                return RedirectToAction(nameof(Index));
            }
            
            var students = await _context.HocSinhs
                .Where(h => idList.Contains(h.IdHocSinh) && h.TrangThai)
                .ToListAsync();
            foreach (var hs in students)
            {
                hs.DaDuyet = false;
                hs.GhiChu = "Không đủ điều kiện";
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã từ chối {students.Count} học sinh.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApproveAllEligible()
        {
            if (IsResultsLocked())
                return RedirectWithLockedResultsMessage();

            var students = await _context.HocSinhs
                .Where(h => h.TrangThai && (h.GhiChu == null || !h.GhiChu.Contains("Không đủ điều kiện")) && !h.DaDuyet)
                .ToListAsync();
            foreach (var hs in students)
            {
                hs.DaDuyet = true;
                hs.GhiChu = "Đã duyệt";
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã duyệt tất cả học sinh đủ điều kiện ({students.Count}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LockResults()
        {
            var hasPendingEligibleStudents = _context.HocSinhs
                .Any(h => h.TrangThai &&
                          (h.GhiChu == null || !h.GhiChu.Contains("Không đủ điều kiện")) &&
                          !h.DaDuyet);
            if (hasPendingEligibleStudents)
            {
                TempData["Error"] = "Còn học sinh đủ điều kiện chưa được duyệt. Vui lòng hoàn tất duyệt trước khi khóa kết quả.";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetString(ResultsLockedSessionKey, bool.TrueString);
            TempData["Success"] = "Đã khóa kết quả xét lên lớp / tốt nghiệp.";
            return RedirectToAction(nameof(Index));
        }

        private bool IsResultsLocked() =>
            bool.TryParse(HttpContext.Session.GetString(ResultsLockedSessionKey), out var isLocked) && isLocked;

        private IActionResult RedirectWithLockedResultsMessage()
        {
            TempData["Error"] = "Kết quả đã được khóa, không thể thay đổi.";
            return RedirectToAction(nameof(Index));
        }

        private static List<int> ParseStudentIds(string? ids) =>
            string.IsNullOrWhiteSpace(ids)
                ? new List<int>()
                : ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => int.TryParse(value, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

        private static bool IsEligibleForPromotion(HocSinh student)
        {
            return student.TrangThai &&
                (string.IsNullOrWhiteSpace(student.GhiChu) ||
                 !student.GhiChu.Contains("Không đủ điều kiện", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Dictionary<int, PromotionAssessment>> BuildPromotionAssessmentsAsync(
            IReadOnlyCollection<HocSinh> students)
        {
            var results = new Dictionary<int, PromotionAssessment>();
            if (students.Count == 0)
                return results;

            var studentIds = students.Select(x => x.IdHocSinh).ToList();
            var academicYearNames = students
                .Select(x => x.LopHoc?.NamHoc)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Distinct()
                .ToList();
            var academicYears = await _context.NamHocs
                .AsNoTracking()
                .Where(x => academicYearNames.Contains(x.TenNamHoc))
                .ToListAsync();
            var academicYearByName = academicYears.ToDictionary(x => x.TenNamHoc, x => x);
            var academicYearIds = academicYears.Select(x => x.IdNamHoc).ToList();
            var semesters = await _context.HocKys
                .AsNoTracking()
                .Where(x => academicYearIds.Contains(x.IdNamHoc))
                .OrderBy(x => x.NgayBatDau)
                .ToListAsync();
            var grades = await _context.Diems
                .AsNoTracking()
                .Where(x => studentIds.Contains(x.IdHocSinh) && x.IdNamHoc.HasValue && academicYearIds.Contains(x.IdNamHoc.Value))
                .ToListAsync();

            foreach (var student in students)
            {
                var assessment = new PromotionAssessment();
                results[student.IdHocSinh] = assessment;

                if (!student.TrangThai)
                {
                    assessment.IsComplete = true;
                    assessment.Decision = "Không xét";
                    assessment.Reason = "Học sinh đã ngừng học hoặc chuyển trường.";
                    continue;
                }

                if (student.LopHoc == null || string.IsNullOrWhiteSpace(student.LopHoc.NamHoc) ||
                    !academicYearByName.TryGetValue(student.LopHoc.NamHoc, out var academicYear))
                {
                    assessment.Decision = "Chưa đủ dữ liệu";
                    assessment.Reason = "Chưa xác định được năm học của lớp.";
                    continue;
                }

                var yearSemesters = semesters
                    .Where(x => x.IdNamHoc == academicYear.IdNamHoc)
                    .OrderBy(x => x.NgayBatDau)
                    .Take(2)
                    .ToList();
                if (yearSemesters.Count < 2)
                {
                    assessment.Decision = "Chưa đủ dữ liệu";
                    assessment.Reason = "Năm học chưa có đủ hai học kỳ.";
                    continue;
                }

                var yearlyGrades = grades
                    .Where(x => x.IdHocSinh == student.IdHocSinh && x.IdNamHoc == academicYear.IdNamHoc)
                    .GroupBy(x => x.IdMonHoc)
                    .ToList();
                if (!yearlyGrades.Any())
                {
                    assessment.Decision = "Chưa đủ dữ liệu";
                    assessment.Reason = "Chưa nhập điểm cho năm học này.";
                    continue;
                }

                var subjectAverages = new List<decimal>();
                var incompleteSubjects = 0;
                foreach (var subjectGrades in yearlyGrades)
                {
                    var firstSemester = subjectGrades
                        .Where(x => x.IdHocKy == yearSemesters[0].IdHocKy)
                        .OrderByDescending(x => x.IdDiem)
                        .FirstOrDefault();
                    var secondSemester = subjectGrades
                        .Where(x => x.IdHocKy == yearSemesters[1].IdHocKy)
                        .OrderByDescending(x => x.IdDiem)
                        .FirstOrDefault();

                    if (!firstSemester?.DiemTB.HasValue == true || !secondSemester?.DiemTB.HasValue == true)
                    {
                        incompleteSubjects++;
                        continue;
                    }

                    subjectAverages.Add(Math.Round((firstSemester.DiemTB.Value + secondSemester.DiemTB.Value * 2) / 3, 2));
                }

                if (incompleteSubjects > 0)
                {
                    assessment.Decision = "Chưa đủ dữ liệu";
                    assessment.Reason = $"Còn {incompleteSubjects} môn chưa có điểm trung bình của cả hai học kỳ.";
                    continue;
                }

                assessment.IsComplete = true;
                assessment.AnnualAverage = Math.Round(subjectAverages.Average(), 2);
                assessment.IsEligible = assessment.AnnualAverage >= 5m;
                var isGraduation = string.Equals(student.LopHoc.Khoi?.Trim(), "9", StringComparison.OrdinalIgnoreCase);
                assessment.Decision = assessment.IsEligible
                    ? (isGraduation ? "Đủ điều kiện tốt nghiệp" : "Đủ điều kiện lên lớp")
                    : "Chưa đủ điều kiện";
                assessment.Reason = assessment.IsEligible
                    ? "Đã đủ điểm trung bình cả hai học kỳ và ĐTB cả năm đạt từ 5.0."
                    : "ĐTB cả năm dưới 5.0.";
            }

            return results;
        }

        [HttpPost]
        public async Task<IActionResult> AutoAssignClass(string NamHoc, string Khoi, string TrangThai)
        {
            // Dummy logic cho demo: Thông báo xếp lớp thành công
            // Trong thực tế sẽ cần lấy danh sách học sinh đủ điều kiện và chia đều vào các lớp mới sinh ra
            TempData["Success"] = $"Đã xếp lớp tự động cho năm học {NamHoc} thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> PrintResults(string NamHoc, string Khoi, string Lop, string LoaiKetQua, string HinhThucIn, string MauIn)
        {
            var query = _context.HocSinhs.Include(h => h.LopHoc).AsQueryable();
            if (!string.IsNullOrEmpty(Khoi)) query = query.Where(h => h.LopHoc != null && h.LopHoc.Khoi == Khoi);
            if (!string.IsNullOrEmpty(Lop)) query = query.Where(h => h.LopHoc != null && h.LopHoc.TenLop == Lop);
            var students = await query.OrderBy(h => h.LopHoc.TenLop).ThenBy(h => h.HoTen).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container => {
                container.Page(page => {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));
                    
                    page.Header().Text(MauIn ?? "Danh sách kết quả").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x => {
                        x.Spacing(10);
                        x.Item().Text($"Năm học: {NamHoc}");
                        x.Item().Text($"Khối: {Khoi}");
                        x.Item().Text($"Lớp: {Lop}");
                        x.Item().Text($"Loại kết quả: {LoaiKetQua}");
                        x.Item().PaddingTop(20).Text("Danh sách học sinh").Bold();
                        
                        int i = 1;
                        foreach (var hs in students)
                        {
                            string trangThai = hs.DaDuyet ? "Đã duyệt" : (hs.GhiChu ?? (hs.TrangThai ? "Đủ điều kiện" : "Bỏ học"));
                            string lopName = hs.LopHoc != null ? hs.LopHoc.TenLop : "";
                            x.Item().Text($"{i++}. {hs.MaHS} - {hs.HoTen} - Lớp {lopName} - {trangThai}");
                        }
                    });
                    page.Footer().AlignCenter().Text(x => {
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
        public async Task<IActionResult> ExportExcel(string NamHoc, string Khoi, string Lop, string LoaiDuLieu, string DinhDangFile, List<string> BaoGom)
        {
            var query = _context.HocSinhs.Include(h => h.LopHoc).AsQueryable();
            if (!string.IsNullOrEmpty(Khoi)) query = query.Where(h => h.LopHoc != null && h.LopHoc.Khoi == Khoi);
            if (!string.IsNullOrEmpty(Lop)) query = query.Where(h => h.LopHoc != null && h.LopHoc.TenLop == Lop);
            var students = await query.OrderBy(h => h.LopHoc.TenLop).ThenBy(h => h.HoTen).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("KetQua");
            worksheet.Cell(1, 1).Value = "Kết quả xét lên lớp - " + NamHoc;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            
            int col = 1;
            if (BaoGom != null && BaoGom.Count > 0)
            {
                foreach(var field in BaoGom)
                {
                    worksheet.Cell(3, col).Value = field;
                    worksheet.Cell(3, col).Style.Font.Bold = true;
                    worksheet.Cell(3, col).Style.Fill.BackgroundColor = XLColor.LightGray;
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
                string trangThai = hs.DaDuyet ? "Đã duyệt" : (hs.GhiChu ?? (hs.TrangThai ? "Đủ điều kiện" : "Bỏ học"));
                if (BaoGom != null && BaoGom.Count > 0)
                {
                    int c = 1;
                    foreach(var field in BaoGom)
                    {
                        if (field.Contains("Mã", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = hs.MaHS;
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
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KetQua_{NamHoc}.xlsx");
        }
    }
}

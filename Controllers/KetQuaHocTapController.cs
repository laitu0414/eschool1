using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.ViewModels;
using eSchool.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace eSchool.Controllers
{
    [RoleAuthorize(SystemRoleIds.SystemAdmin, 2, 3, 4)]
    public class KetQuaHocTapController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INhatKyService _nhatKyService;
        private readonly IEmailSender _emailSender;

        public KetQuaHocTapController(AppDbContext context, INhatKyService nhatKyService, IEmailSender emailSender)
        {
            _context = context;
            _nhatKyService = nhatKyService;
            _emailSender = emailSender;
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult NhatKySuaDiem()
        {
            var logs = _context.NhatKyHoatDongs
                .Where(x => x.HanhDong == "Sửa điểm" || x.HanhDong == "Nhập điểm")
                .OrderByDescending(x => x.ThoiGian)
                .ToList();

            return View(logs);
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
        public IActionResult Diem(int? lopId, int? hocKyId)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var isGiaoVien = roleId == 2;
            var giaoVien = isGiaoVien ? GetCurrentGiaoVien() : null;
            if (isGiaoVien && giaoVien == null) return Forbid();

            var vm = new AdminDiemPageViewModel
            {
                NamHocs = GetNamHocSelectList(),
                HocKys = GetHocKySelectList(),
                LopHocs = GetLopSelectList(),
                MonHocsList = _context.MonHocs.OrderBy(x => x.TenMon).ToList()
            };

            if (isGiaoVien && giaoVien != null)
            {
                var assignedLopIds = _context.PhanCongGiangDays
                    .Where(x => x.IdGiaoVien == giaoVien.IdGiaoVien)
                    .Select(x => x.IdLop)
                    .Distinct()
                    .ToList();
                vm.LopHocs = vm.LopHocs.Where(x => assignedLopIds.Contains(int.Parse(x.Value))).ToList();
            }

            if (lopId.HasValue && hocKyId.HasValue)
            {
                var selectedClass = _context.LopHocs.Find(lopId.Value);
                var selectedSemester = _context.HocKys.Include(h => h.NamHoc).FirstOrDefault(h => h.IdHocKy == hocKyId);
                if (selectedClass == null || selectedSemester?.NamHoc == null ||
                    selectedClass.NamHoc?.Replace(" ", "") != selectedSemester.NamHoc.TenNamHoc.Replace(" ", ""))
                {
                    TempData["Error"] = "Lớp và học kỳ phải thuộc cùng năm học.";
                    return RedirectToAction(nameof(Diem));
                }
            }
            foreach (var option in vm.LopHocs) option.Selected = option.Value == lopId?.ToString();
            foreach (var option in vm.HocKys) option.Selected = option.Value == hocKyId?.ToString();
            var hocSinhsQuery = _context.HocSinhs.Include(x => x.LopHoc).AsNoTracking().AsQueryable();
            if (lopId.HasValue)
            {
                if (isGiaoVien && giaoVien != null)
                {
                    var isAssigned = _context.PhanCongGiangDays.Any(x => x.IdGiaoVien == giaoVien.IdGiaoVien && x.IdLop == lopId.Value);
                    if (!isAssigned) hocSinhsQuery = hocSinhsQuery.Where(x => false);
                    else hocSinhsQuery = hocSinhsQuery.Where(x => x.IdLopHoc == lopId.Value);
                }
                else
                {
                    hocSinhsQuery = hocSinhsQuery.Where(x => x.IdLopHoc == lopId.Value);
                }
            }
            else if (isGiaoVien && giaoVien != null)
            {
                var assignedLopIds = _context.PhanCongGiangDays
                    .Where(x => x.IdGiaoVien == giaoVien.IdGiaoVien)
                    .Select(x => x.IdLop)
                    .ToList();
                hocSinhsQuery = hocSinhsQuery.Where(x => assignedLopIds.Contains(x.IdLopHoc ?? 0));
            }

            var hocSinhs = hocSinhsQuery.OrderBy(x => x.LopHoc!.TenLop).ThenBy(x => x.HoTen).ToList();

            var danhSach = new List<DiemHocSinhViewModel>();

            if (lopId.HasValue && hocKyId.HasValue)
            {
                var studentIds = hocSinhs.Select(h => h.IdHocSinh).ToList();
                var diemsQuery = _context.Diems.AsNoTracking().Where(x => x.IdHocKy == hocKyId.Value && studentIds.Contains(x.IdHocSinh));
                var diems = diemsQuery.ToList().GroupBy(d => new { d.IdHocSinh, d.IdMonHoc }).Select(g => g.OrderByDescending(d => d.IdDiem).First()).ToList();
                var diemGroup = diems.GroupBy(x => x.IdHocSinh).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var hs in hocSinhs)
                {
                    decimal? tbHocKy = null;
                    var diemMonHoc = new Dictionary<int, decimal?>();
                    if (diemGroup.TryGetValue(hs.IdHocSinh, out var hsDiems))
                    {
                        if (hsDiems.Any(x => x.DiemTB.HasValue))
                        {
                            tbHocKy = Math.Round(hsDiems.Where(x => x.DiemTB.HasValue).Average(x => x.DiemTB)!.Value, 2);
                        }

                        foreach (var d in hsDiems)
                        {
                            diemMonHoc[d.IdMonHoc] = d.DiemTB;
                        }
                    }

                    danhSach.Add(new DiemHocSinhViewModel
                    {
                        IdHocSinh = hs.IdHocSinh,
                        HocSinh = hs,
                        DiemTBHocKy = tbHocKy,
                        DiemTBMon = diemMonHoc
                    });
                }
            }

            vm.DanhSach = danhSach;
            ViewBag.FilterLopId = lopId;
            ViewBag.FilterHocKyId = hocKyId;

            // Lấy danh sách môn học cho dropdown Nhập từ Excel
            if (isGiaoVien && giaoVien != null && lopId.HasValue)
            {
                var assignedMonHocs = _context.PhanCongGiangDays
                    .Where(x => x.IdGiaoVien == giaoVien.IdGiaoVien && x.IdLop == lopId.Value)
                    .Select(x => x.IdMonHoc)
                    .ToList();
                ViewBag.MonHocs = new SelectList(_context.MonHocs.Where(x => assignedMonHocs.Contains(x.IdMonHoc)).ToList(), "IdMonHoc", "TenMon");
            }
            else
            {
                ViewBag.MonHocs = new SelectList(_context.MonHocs.ToList(), "IdMonHoc", "TenMon");
            }

            return View(vm);
        }

        private bool AreGradesLocked(int? yearId) => PromotionLockService.IsLocked(_context, yearId);

        private List<int> EditableSubjects(HocSinh student, HocKy semester)
        {
            if (HttpContext.Session.GetInt32("RoleId") == SystemRoleIds.SystemAdmin)
                return _context.MonHocs.Select(m => m.IdMonHoc).ToList();
            var teacher = GetCurrentGiaoVien();
            if (teacher == null || student.IdLopHoc == null) return new();
            return _context.PhanCongGiangDays.Where(p => p.IdGiaoVien == teacher.IdGiaoVien &&
                p.IdLop == student.IdLopHoc && (p.NamHoc == null || p.NamHoc == "" || p.NamHoc == student.LopHoc!.NamHoc) &&
                (p.HocKy == null || p.HocKy == "" || p.HocKy == "Cả năm" || p.HocKy == semester.TenHocKy))
                .Select(p => p.IdMonHoc).Distinct().ToList();
        }

        private static bool MatchesYear(HocSinh student, HocKy semester) =>
            student.LopHoc?.NamHoc != null && semester.NamHoc != null &&
            student.LopHoc.NamHoc.Replace(" ", "") == semester.NamHoc.TenNamHoc.Replace(" ", "");

        private static string GradeSnapshot(IEnumerable<Diem> grades) =>
            System.Text.Json.JsonSerializer.Serialize(grades.OrderBy(d => d.IdMonHoc).ThenBy(d => d.IdDiem)
                .Select(d => new { d.IdDiem, d.IdMonHoc, d.IdNamHoc, d.Diem15Phut, d.Diem1Tiet, d.DiemGiuaKy, d.DiemCuoiKy }));

        private static string GradeVersion(IEnumerable<Diem> grades) =>
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(GradeSnapshot(grades))));

        [HttpGet]
        [RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
        public IActionResult GetDiemHocSinh(int idHocSinh, int idHocKy)
        {
            var semester = _context.HocKys.Include(h => h.NamHoc).FirstOrDefault(h => h.IdHocKy == idHocKy);
            var student = _context.HocSinhs.Include(h => h.LopHoc).FirstOrDefault(h => h.IdHocSinh == idHocSinh);
            if (student == null || semester == null) return NotFound();
            if (!MatchesYear(student, semester)) return BadRequest(new { message = "Học kỳ không thuộc năm học của lớp." });
            var allowed = EditableSubjects(student, semester);
            if (HttpContext.Session.GetInt32("RoleId") == 2 && allowed.Count == 0) return Forbid();
            var grades = _context.Diems.AsNoTracking().Where(d => d.IdHocSinh == idHocSinh && d.IdHocKy == idHocKy).ToList();
            var locked = AreGradesLocked(semester.IdNamHoc);
            var subjects = _context.MonHocs.OrderBy(m => m.TenMon).ToList();
            if (HttpContext.Session.GetInt32("RoleId") == 2) subjects = subjects.Where(m => allowed.Contains(m.IdMonHoc)).ToList();
            return Ok(new
            {
                idNamHoc = semester.IdNamHoc, version = GradeVersion(grades), isLocked = locked,
                diemMonHocs = subjects.Select(m =>
                {
                    var grade = grades.Where(d => d.IdMonHoc == m.IdMonHoc).OrderByDescending(d => d.IdDiem).FirstOrDefault();
                    return new DiemMonHocViewModel
                    {
                        IdMonHoc = m.IdMonHoc, TenMon = m.TenMon,
                        Diem15Phut = grade?.Diem15Phut, Diem1Tiet = grade?.Diem1Tiet,
                        DiemGiuaKy = grade?.DiemGiuaKy, DiemCuoiKy = grade?.DiemCuoiKy,
                        IsEditable = !locked && student.TrangThai && allowed.Contains(m.IdMonHoc)
                    };
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
        public IActionResult LuuDiemHocSinh([FromBody] LuuDiemHocSinhRequest req)
        {
            if (!ModelState.IsValid || req == null || req.IdHocSinh <= 0 || req.IdHocKy <= 0 || req.IdNamHoc <= 0 ||
                req.DiemMonHocs == null || req.DiemMonHocs.Count == 0 || req.DiemMonHocs.Any(m => m == null) ||
                req.DiemMonHocs.GroupBy(m => m.IdMonHoc).Any(g => g.Count() > 1))
                return BadRequest(new { success = false, message = "Danh sách điểm không hợp lệ hoặc có môn bị trùng." });
            using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            if (AreGradesLocked(req.IdNamHoc)) return BadRequest(new { success = false, message = "Kết quả đã khóa. Không thể sửa điểm." });
            var student = _context.HocSinhs.Include(h => h.LopHoc).FirstOrDefault(h => h.IdHocSinh == req.IdHocSinh);
            var semester = _context.HocKys.Include(h => h.NamHoc).FirstOrDefault(h => h.IdHocKy == req.IdHocKy && h.IdNamHoc == req.IdNamHoc);
            if (student == null || !student.TrangThai || semester == null || !MatchesYear(student, semester))
                return BadRequest(new { success = false, message = "Học sinh đã ngừng học hoặc lớp, học kỳ, năm học không khớp." });
            var allowed = EditableSubjects(student, semester);
            if (req.DiemMonHocs.Any(m => !allowed.Contains(m.IdMonHoc))) return Forbid();
            try
            {
                foreach (var item in req.DiemMonHocs)
                {
                    item.Diem15Phut = DiemExcelReader.NormalizeInput(item.Diem15Phut);
                    item.Diem1Tiet = DiemExcelReader.NormalizeInput(item.Diem1Tiet);
                    item.DiemGiuaKy = DiemExcelReader.NormalizeInput(item.DiemGiuaKy);
                    item.DiemCuoiKy = DiemExcelReader.NormalizeInput(item.DiemCuoiKy);
                }
            }
            catch (FormatException ex) { return BadRequest(new { success = false, message = ex.Message }); }
            var existing = _context.Diems.Where(d => d.IdHocSinh == req.IdHocSinh && d.IdHocKy == req.IdHocKy).ToList();
            if (existing.GroupBy(d => d.IdMonHoc).Any(g => g.Count() > 1) ||
                existing.Any(d => d.IdNamHoc.HasValue && d.IdNamHoc != req.IdNamHoc))
                return Conflict(new { success = false, message = "Dữ liệu đang trùng môn hoặc sai năm học. Cần kiểm tra trước khi lưu." });
            if (string.IsNullOrEmpty(req.Version) || req.Version != GradeVersion(existing))
                return Conflict(new { success = false, message = "Điểm đã thay đổi hoặc phiên nhập đã cũ. Đóng và mở lại bảng điểm trước khi lưu." });
            var before = GradeSnapshot(existing);
            foreach (var item in req.DiemMonHocs)
            {
                var grade = existing.FirstOrDefault(d => d.IdMonHoc == item.IdMonHoc);
                var hasScores = new[] { item.Diem15Phut, item.Diem1Tiet, item.DiemGiuaKy, item.DiemCuoiKy }.Any(s => s != null);
                if (grade == null && !hasScores) continue;
                if (grade == null)
                {
                    grade = new Diem { IdHocSinh = req.IdHocSinh, IdMonHoc = item.IdMonHoc, IdHocKy = req.IdHocKy };
                    _context.Diems.Add(grade);
                    existing.Add(grade);
                }
                grade.IdNamHoc = req.IdNamHoc;
                grade.HocKy = semester.TenHocKy;
                grade.Diem15Phut = item.Diem15Phut; grade.Diem1Tiet = item.Diem1Tiet;
                grade.DiemGiuaKy = item.DiemGiuaKy; grade.DiemCuoiKy = item.DiemCuoiKy;
                grade.TinhDiemTrungBinh();
            }
            var after = GradeSnapshot(existing);
            if (before == after) return Ok(new { success = true, message = "Không có điểm thay đổi." });
            student.DaDuyet = false;
            _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                TenDangNhap = HttpContext.Session.GetString("Username") ?? "Admin", HanhDong = "Sửa điểm",
                NoiDung = $"Học sinh {student.MaHS}, {semester.TenHocKy}, {semester.NamHoc!.TenNamHoc}. Trước: {before}. Sau: {after}."
            });
            try { _context.SaveChanges(); transaction.Commit(); }
            catch (DbUpdateException) { transaction.Rollback(); return Conflict(new { success = false, message = "Chưa lưu được điểm. Vui lòng tải lại và thử lại." }); }
            return Ok(new { success = true, message = "Đã lưu điểm và nhật ký thay đổi." });
        }
        [RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
        public IActionResult DownloadDiemTemplate(int lopId)
        {
            if (!_context.LopHocs.Any(l => l.IdLop == lopId)) return BadRequest("Vui lòng chọn lớp hợp lệ.");
            if (HttpContext.Session.GetInt32("RoleId") == 2)
            {
                var teacher = GetCurrentGiaoVien();
                if (teacher == null || !_context.PhanCongGiangDays.Any(p => p.IdGiaoVien == teacher.IdGiaoVien && p.IdLop == lopId))
                    return Forbid();
            }
            var hocSinhs = _context.HocSinhs
                .Where(x => x.IdLopHoc == lopId && x.TrangThai)
                .OrderBy(x => x.HoTen)
                .ToList();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("NhapDiem");

            worksheet.Cell(1, 1).Value = "ID Học sinh (*Không sửa*)";
            worksheet.Cell(1, 2).Value = "Mã Học sinh";
            worksheet.Cell(1, 3).Value = "Họ Tên";
            worksheet.Cell(1, 4).Value = "Điểm 15 Phút";
            worksheet.Cell(1, 5).Value = "Điểm 1 Tiết";
            worksheet.Cell(1, 6).Value = "Điểm Giữa Kỳ";
            worksheet.Cell(1, 7).Value = "Điểm Cuối Kỳ";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            int row = 2;
            foreach (var hs in hocSinhs)
            {
                worksheet.Cell(row, 1).Value = hs.IdHocSinh;
                worksheet.Cell(row, 2).Value = hs.MaHS;
                worksheet.Cell(row, 3).Value = hs.HoTen;
                row++;
            }

            worksheet.Columns(4, 7).Style.NumberFormat.Format = "@";
            worksheet.SheetView.FreezeRows(1);
            var help = workbook.Worksheets.Add("HuongDan");
            help.Cell(1, 1).Value = "Nhập điểm trên trang NhapDiem; không sửa tiêu đề, ID hoặc mã học sinh.";
            help.Cell(2, 1).Value = "Điểm từ 0 đến 10. Số thập phân: 7.5 hoặc 7,5. Nhiều điểm: 8; 7,5; 9 (dùng dấu chấm phẩy).";
            help.Cell(3, 1).Value = "Ô trống giữ nguyên điểm cũ. Ô có điểm thay thế toàn bộ danh sách điểm của cột tương ứng.";
            help.Cell(4, 1).Value = "Không dùng công thức, ngày tháng hoặc ký hiệu. Bất kỳ lỗi nào cũng hủy toàn bộ lần nhập.";
            help.Cell(5, 1).Value = "Chọn đúng môn và học kỳ trên website trước khi nhập. Điểm trung bình được tính tự động khi đủ bốn nhóm điểm.";
            help.Column(1).Width = 110;
            help.Column(1).Style.Alignment.WrapText = true;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_NhapDiem.xlsx");
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportDiemExcel(IFormFile? file, int lopId, int hocKyId, int monHocId)
        {
            IActionResult ImportError(string message)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Diem), new { lopId, hocKyId });
            }
            if (file == null || file.Length <= 0) return ImportError("Vui lòng chọn file Excel.");
            if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                file.Length > 5 * 1024 * 1024)
                return ImportError("Chỉ nhận file .xlsx dung lượng tối đa 5 MB.");
            var lop = await _context.LopHocs.FindAsync(lopId);
            var hocKy = await _context.HocKys.Include(h => h.NamHoc).SingleOrDefaultAsync(h => h.IdHocKy == hocKyId);
            if (lop == null || hocKy?.NamHoc == null || !await _context.MonHocs.AnyAsync(m => m.IdMonHoc == monHocId))
                return ImportError("Vui lòng chọn lớp, học kỳ và môn học hợp lệ.");
            if (lop.NamHoc?.Replace(" ", "") != hocKy.NamHoc.TenNamHoc.Replace(" ", ""))
                return ImportError("Học kỳ không thuộc năm học của lớp đã chọn.");
            if (HttpContext.Session.GetInt32("RoleId") == 2)
            {
                var teacher = GetCurrentGiaoVien();
                if (teacher == null || !await _context.PhanCongGiangDays.AnyAsync(p =>
                    p.IdGiaoVien == teacher.IdGiaoVien && p.IdLop == lopId && p.IdMonHoc == monHocId &&
                    (p.NamHoc == null || p.NamHoc == "" || p.NamHoc == lop.NamHoc) &&
                    (p.HocKy == null || p.HocKy == "" || p.HocKy == "Cả năm" || p.HocKy == hocKy.TenHocKy)))
                    return Forbid();
            }
            if (AreGradesLocked(hocKy.IdNamHoc)) return ImportError("Kết quả năm học đã khóa. Mở khóa trước khi nhập điểm.");
            List<DiemExcelRow> rows;
            var errors = new List<string>();
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;
                using var workbook = new XLWorkbook(stream);
                rows = DiemExcelReader.Read(workbook, errors);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return ImportError("Không đọc được file Excel. Hãy dùng file mẫu .xlsx, không đặt mật khẩu và kiểm tra file không bị hỏng.");
            }
            var students = await _context.HocSinhs.Where(h => h.IdLopHoc == lopId && h.TrangThai)
                .ToDictionaryAsync(h => h.IdHocSinh);
            foreach (var row in rows)
            {
                if (!students.TryGetValue(row.StudentId, out var student))
                    errors.Add($"Dòng {row.RowNumber}: học sinh không thuộc lớp hoặc đã ngừng học.");
                else if (!string.Equals(student.MaHS.Trim(), row.StudentCode, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"Dòng {row.RowNumber}: mã học sinh không khớp ID.");
            }
            if (errors.Count > 0)
                return ImportError("Chưa lưu điểm. " + string.Join(" | ", errors.Take(12)) +
                    (errors.Count > 12 ? $" | Còn {errors.Count - 12} lỗi khác." : ""));
            rows = rows.Where(r => r.Grades.Any(g => g != null)).ToList();
            if (rows.Count == 0) return ImportError("File chưa có điểm để nhập. Ô trống giữ nguyên điểm cũ.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                if (AreGradesLocked(hocKy.IdNamHoc)) return ImportError("Kết quả đã khóa. Chưa lưu điểm.");
                var ids = rows.Select(r => r.StudentId).ToList();
                var existing = await _context.Diems.Where(d => ids.Contains(d.IdHocSinh) &&
                    d.IdMonHoc == monHocId && d.IdHocKy == hocKyId).ToListAsync();
                if (existing.GroupBy(d => d.IdHocSinh).Any(g => g.Count() > 1) ||
                    existing.Any(d => d.IdNamHoc.HasValue && d.IdNamHoc != hocKy.IdNamHoc))
                    return ImportError("Dữ liệu điểm hiện tại bị trùng hoặc sai năm học. Chưa lưu; cần kiểm tra dữ liệu trước.");
                var beforeImport = GradeSnapshot(existing);
                var byStudent = existing.ToDictionary(d => d.IdHocSinh);
                var created = 0;
                foreach (var row in rows)
                {
                    if (!byStudent.TryGetValue(row.StudentId, out var diem))
                    {
                        diem = new Diem { IdHocSinh = row.StudentId, IdMonHoc = monHocId, IdHocKy = hocKyId };
                        _context.Diems.Add(diem);
                        existing.Add(diem);
                        created++;
                    }
                    diem.IdNamHoc = hocKy.IdNamHoc;
                    diem.HocKy = hocKy.TenHocKy;
                    if (row.Grades[0] != null) diem.Diem15Phut = row.Grades[0];
                    if (row.Grades[1] != null) diem.Diem1Tiet = row.Grades[1];
                    if (row.Grades[2] != null) diem.DiemGiuaKy = row.Grades[2];
                    if (row.Grades[3] != null) diem.DiemCuoiKy = row.Grades[3];
                    diem.TinhDiemTrungBinh();
                    students[row.StudentId].DaDuyet = false;
                }
                _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
                {
                    TenDangNhap = HttpContext.Session.GetString("Username") ?? "Admin",
                    HanhDong = "Nhập điểm",
                    NoiDung = $"Excel: lớp {lop.TenLop}, môn ID {monHocId}, học kỳ {hocKy.TenHocKy}, năm {hocKy.NamHoc.TenNamHoc}: {created} mới, {rows.Count - created} cập nhật. Trước: {beforeImport}. Sau: {GradeSnapshot(existing)}."
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = $"Đã nhập điểm cho {rows.Count} học sinh ({created} mới, {rows.Count - created} cập nhật). Ô trống giữ nguyên điểm cũ; kết quả duyệt của học sinh được đặt lại để xét theo điểm mới.";
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                await transaction.RollbackAsync();
                return ImportError("Không lưu được điểm. Toàn bộ lần nhập đã hủy; vui lòng thử lại.");
            }
            return RedirectToAction(nameof(Diem), new { lopId, hocKyId });
        }
        [RoleAuthorize(3, 4)]
        public IActionResult XemDiem(int? namHocId, int? hocKyId)
        {
            var hocSinh = GetCurrentHocSinh();
            if (hocSinh == null)
            {
                return NotFound("Tai khoan nay chua duoc lien ket voi ho so hoc sinh.");
            }

            var query = _context.Diems
                .Include(x => x.MonHoc)
                .Include(x => x.HocKyInfo)
                .Include(x => x.NamHocInfo)
                .AsNoTracking()
                .Where(x => x.IdHocSinh == hocSinh.IdHocSinh);

            if (namHocId.HasValue)
            {
                query = query.Where(x => x.IdNamHoc == namHocId.Value);
            }

            if (hocKyId.HasValue)
            {
                query = query.Where(x => x.IdHocKy == hocKyId.Value);
            }

            var vm = new DiemPageViewModel
            {
                DanhSach = query
                    .OrderByDescending(x => x.NamHocInfo!.NgayBatDau)
                    .ThenBy(x => x.HocKyInfo!.NgayBatDau)
                    .ThenBy(x => x.MonHoc!.TenMon)
                    .ToList(),
                NamHocs = GetNamHocSelectList(),
                HocKys = GetHocKySelectList()
            };

            foreach (var option in vm.NamHocs) option.Selected = option.Value == namHocId?.ToString();
            foreach (var option in vm.HocKys) option.Selected = option.Value == hocKyId?.ToString();
            ViewBag.HocSinh = hocSinh;
            ViewBag.FilterNamHocId = namHocId;
            ViewBag.FilterHocKyId = hocKyId;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult LuuDiem(DiemFormViewModel vm)
        {
            if (!ModelState.IsValid) return Error(nameof(Diem), "Dữ liệu điểm không hợp lệ.");
            if (vm.IdDiem > 0 && !_context.Diems.Any(d => d.IdDiem == vm.IdDiem && d.IdHocSinh == vm.IdHocSinh &&
                d.IdMonHoc == vm.IdMonHoc && d.IdHocKy == vm.IdHocKy && d.IdNamHoc == vm.IdNamHoc)) return NotFound();
            var current = _context.Diems.AsNoTracking().Where(d => d.IdHocSinh == vm.IdHocSinh && d.IdHocKy == vm.IdHocKy).ToList();
            var result = LuuDiemHocSinh(new LuuDiemHocSinhRequest
            {
                IdHocSinh = vm.IdHocSinh, IdHocKy = vm.IdHocKy, IdNamHoc = vm.IdNamHoc, Version = GradeVersion(current),
                DiemMonHocs = new() { new DiemMonHocViewModel
                {
                    IdMonHoc = vm.IdMonHoc, Diem15Phut = vm.Diem15Phut?.Replace(",", ";"),
                    Diem1Tiet = vm.Diem1Tiet?.Replace(",", ";"), DiemGiuaKy = vm.DiemGiuaKy?.Replace(",", ";"),
                    DiemCuoiKy = vm.DiemCuoiKy?.Replace(",", ";")
                }}
            });
            if (result is not OkObjectResult) return result;
            return Success(nameof(Diem), "Đã lưu điểm.");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult XoaDiem(int id)
        {
            using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);

            var diem = _context.Diems.Find(id);
            if (diem == null)
            {
                return NotFound();
            }

            var gradeYear = diem.IdNamHoc ?? _context.HocKys.Where(h => h.IdHocKy == diem.IdHocKy).Select(h => (int?)h.IdNamHoc).FirstOrDefault();
            if (AreGradesLocked(gradeYear)) return Error(nameof(Diem), "Kết quả năm học đã khóa. Không thể xóa điểm.");
            var student = _context.HocSinhs.Find(diem.IdHocSinh);
            if (student != null) student.DaDuyet = false;
            _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                TenDangNhap = HttpContext.Session.GetString("Username") ?? "Admin",
                HanhDong = "Sửa điểm", NoiDung = $"Xóa điểm học sinh ID {diem.IdHocSinh}, học kỳ ID {diem.IdHocKy}: {GradeSnapshot(new[] { diem })}"
            });
            _context.Diems.Remove(diem);
            _context.SaveChanges();
            transaction.Commit();
            return Success(nameof(Diem), "Đã xóa điểm và lưu nhật ký.");
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public async Task<IActionResult> HocPhi(int? trangThai, int? namHocId)
        {
            await UpdateQuaHanHocPhi();

            var vm = new HocPhiPageViewModel
            {
                HocSinhs = GetHocSinhSelectList(),
                NamHocs = GetNamHocSelectList(),
                HocKys = GetHocKySelectList()
            };

            var query = _context.HocPhis
                .Include(x => x.HocSinh).ThenInclude(x => x!.LopHoc)
                .Include(x => x.NamHoc)
                .Include(x => x.HocKyInfo)
                .AsNoTracking()
                .AsQueryable();

            if (trangThai.HasValue)
            {
                query = query.Where(x => x.TrangThai == trangThai.Value);
            }

            if (namHocId.HasValue)
            {
                query = query.Where(x => x.IdNamHoc == namHocId.Value);
            }

            var hocPhis = query.ToList();

            // Group the data by NamHoc, HocKy, Lop, SoTien, HanDongTien
            var grouped = hocPhis
                .GroupBy(x => new
                {
                    NamHoc = x.NamHoc?.TenNamHoc ?? "",
                    HocKy = x.HocKyInfo?.TenHocKy ?? "",
                    IdLop = x.HocSinh?.IdLopHoc,
                    TenLop = x.HocSinh?.LopHoc?.TenLop ?? "Chưa xếp lớp",
                    Khoi = x.HocSinh?.LopHoc?.Khoi ?? "",
                    x.SoTien,
                    x.HanDongTien
                })
                .Select(g => new HocPhiTongHopViewModel
                {
                    NamHoc = g.Key.NamHoc,
                    HocKy = g.Key.HocKy,
                    Khoi = g.Key.Khoi,
                    TenLop = g.Key.TenLop,
                    SoTien = g.Key.SoTien,
                    HanDongTien = g.Key.HanDongTien,
                    TongSoHocSinh = g.Count(),
                    DaDong = g.Count(x => x.TrangThai == 1)
                })
                .OrderByDescending(x => x.HanDongTien)
                .ThenBy(x => x.Khoi)
                .ThenBy(x => x.TenLop)
                .ToList();

            vm.DanhSachTongHop = grouped;
            ViewBag.TrangThai = trangThai;
            ViewBag.NamHocId = namHocId;
            ViewBag.LopHocs = GetLopSelectList();
            return View(vm);
        }

        [RoleAuthorize(3, 4)]
        public async Task<IActionResult> XemHocPhi(int? trangThai, int? namHocId)
        {
            await UpdateQuaHanHocPhi();

            var hocSinh = GetCurrentHocSinh();
            if (hocSinh == null)
            {
                return NotFound("Tai khoan nay chua duoc lien ket voi ho so hoc sinh.");
            }

            var vm = new HocPhiPageViewModel
            {
                NamHocs = GetNamHocSelectList()
            };

            var query = _context.HocPhis
                .Include(x => x.HocSinh).ThenInclude(x => x!.LopHoc)
                .Include(x => x.NamHoc)
                .Include(x => x.HocKyInfo)
                .AsNoTracking()
                .Where(x => x.IdHocSinh == hocSinh.IdHocSinh);

            if (trangThai.HasValue)
            {
                query = query.Where(x => x.TrangThai == trangThai.Value);
            }

            if (namHocId.HasValue)
            {
                query = query.Where(x => x.IdNamHoc == namHocId.Value);
            }

            vm.DanhSach = query
                .OrderBy(x => x.TrangThai == 0 ? 0 : x.TrangThai == 2 ? 1 : 2)
                .ThenByDescending(x => x.HanDongTien)
                .ToList();

            ViewBag.HocSinh = hocSinh;
            ViewBag.TrangThai = trangThai;
            ViewBag.NamHocId = namHocId;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public async Task<IActionResult> LuuHocPhi(HocPhiFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Error(nameof(HocPhi), "Thong tin hoc phi chua hop le.");
            }

            var hocKy = _context.HocKys.FirstOrDefault(x => x.IdHocKy == vm.IdHocKy && x.IdNamHoc == vm.IdNamHoc);
            if (hocKy == null)
            {
                return Error(nameof(HocPhi), "Học kỳ không thuộc năm học đã chọn.");
            }

            if (!_context.HocSinhs.Any(x => x.IdHocSinh == vm.IdHocSinh))
                return Error(nameof(HocPhi), "Học sinh không tồn tại.");
            if (vm.TrangThai is < 0 or > 2 || (vm.TrangThai == 1 && (!vm.NgayDong.HasValue || vm.NgayDong.Value.Date > DateTime.Today)))
                return Error(nameof(HocPhi), "Trạng thái hoặc ngày thanh toán không hợp lệ.");
            if (_context.HocPhis.Any(x => x.IdHocSinh == vm.IdHocSinh && x.IdHocKy == vm.IdHocKy))
                return Error(nameof(HocPhi), "Học sinh đã có khoản học phí trong học kỳ này.");

            var hp = new HocPhi
            {
                IdHocSinh = vm.IdHocSinh,
                IdNamHoc = vm.IdNamHoc,
                IdHocKy = vm.IdHocKy,
                HocKy = hocKy.TenHocKy,
                SoTien = vm.SoTien,
                NgayDuKien = vm.NgayDuKien,
                HanDongTien = vm.HanDongTien,
                NgayDong = vm.NgayDong,
                TrangThai = vm.TrangThai,
                PhuongThuc = vm.PhuongThuc,
                PhanTramMienGiam = vm.PhanTramMienGiam,
                SoTienMienGiam = vm.SoTienMienGiam,
                LyDoMienGiam = vm.LyDoMienGiam,
                GhiChu = vm.GhiChu
            };
            _context.HocPhis.Add(hp);
            _context.SaveChanges();

            if (hp.TrangThai == 0)
            {
                var hs = _context.HocSinhs.Find(vm.IdHocSinh);
                var emails = _context.HocSinhPhuHuynhs
                    .Include(x => x.PhuHuynh)
                    .Where(x => x.IdHocSinh == vm.IdHocSinh && x.PhuHuynh != null && x.PhuHuynh.Email != null && x.PhuHuynh.Email != "")
                    .Select(x => x.PhuHuynh.Email)
                    .ToList();
                foreach (var email in emails)
                {
                    if (!string.IsNullOrEmpty(email))
                    {
                        string subject = $"[Thông báo] Học phí mới của học sinh {hs?.HoTen}";
                        string body = $"Kính gửi Phụ huynh,\n\nNhà trường xin thông báo khoản học phí mới cho học sinh {hs?.HoTen}:\n- Số tiền: {hp.SoTien:N0} đ\n- Hạn đóng: {hp.HanDongTien?.ToString("dd/MM/yyyy")}\n\nVui lòng hoàn tất thanh toán đúng hạn.\n\nTrân trọng,\nNhà trường.";
                        try { await _emailSender.SendAsync(email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); /* Ignore email errors so it doesn't block the UI */ }
                    }
                }

                if (!emails.Any() && hs != null && !string.IsNullOrWhiteSpace(hs.Email))
                {
                    string subject = $"[Thông báo] Học phí mới của học sinh {hs.HoTen}";
                    string body = $"Kính gửi Học sinh / Phụ huynh,\n\nNhà trường xin thông báo khoản học phí mới cho học sinh {hs.HoTen}:\n- Số tiền: {hp.SoTien:N0} đ\n- Hạn đóng: {hp.HanDongTien?.ToString("dd/MM/yyyy")}\n\nVui lòng hoàn tất thanh toán đúng hạn.\n\nTrân trọng,\nNhà trường.";
                    try { await _emailSender.SendAsync(hs.Email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); }
                }
            }

            return Success(nameof(HocPhi), "Da tao khoan hoc phi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public async Task<IActionResult> LuuHocPhiHangLoat(int IdNamHoc, int IdHocKy, string? Khoi, int? IdLop, decimal SoTien, DateTime? HanDongTien, string? GhiChu)
        {
            var hocKy = _context.HocKys.FirstOrDefault(x => x.IdHocKy == IdHocKy && x.IdNamHoc == IdNamHoc);
            if (hocKy == null)
                return Error(nameof(HocPhi), "Học kỳ hoặc năm học không hợp lệ.");

            if (!ModelState.IsValid || SoTien < 0 || SoTien > 999999999999m)
                return Error(nameof(HocPhi), "Số tiền học phí không hợp lệ.");
            var yearName = _context.NamHocs.Where(x => x.IdNamHoc == IdNamHoc).Select(x => x.TenNamHoc).FirstOrDefault();
            var hocSinhsQuery = _context.HocSinhs.Include(x => x.LopHoc)
                .Where(x => x.TrangThai && !x.DaTotNghiep && x.LopHoc != null && x.LopHoc.NamHoc == yearName)
                .Where(x => !_context.HocPhis.Any(f => f.IdHocSinh == x.IdHocSinh && f.IdHocKy == IdHocKy));
            if (IdLop.HasValue)
                hocSinhsQuery = hocSinhsQuery.Where(x => x.IdLopHoc == IdLop.Value);
            else if (!string.IsNullOrEmpty(Khoi))
                hocSinhsQuery = hocSinhsQuery.Where(x => x.LopHoc != null && x.LopHoc.Khoi == Khoi);
            else
                return Error(nameof(HocPhi), "Vui lòng chọn khối hoặc lớp.");

            var hocSinhs = hocSinhsQuery.ToList();
            if (!hocSinhs.Any())
                return Error(nameof(HocPhi), "Không tìm thấy học sinh nào phù hợp.");

            var hsIds = hocSinhs.Select(x => x.IdHocSinh).ToList();
            var policies = _context.ChinhSachMienGiams
                                   .Where(x => hsIds.Contains(x.IdHocSinh) && (x.HieuLuc == yearName || x.HieuLuc == null || x.HieuLuc == ""))
                                   .ToList();

            if (policies.Any(x => x.PhanTramGiam < 0 || x.PhanTramGiam > 100))
                return Error(nameof(HocPhi), "Có chính sách miễn giảm ngoài khoảng 0–100%. Vui lòng sửa trước khi tạo học phí.");
            var phuHuynhEmails = _context.HocSinhPhuHuynhs
                .Include(x => x.PhuHuynh)
                .Where(x => hsIds.Contains(x.IdHocSinh) && x.PhuHuynh != null && x.PhuHuynh.Email != null && x.PhuHuynh.Email != "")
                .Select(x => new { x.IdHocSinh, x.PhuHuynh.Email })
                .ToList();

            foreach (var hs in hocSinhs)
            {
                var p = policies.FirstOrDefault(x => x.IdHocSinh == hs.IdHocSinh);

                decimal finalSoTien = SoTien;
                decimal? soTienMienGiam = null;
                
                if (p != null && p.PhanTramGiam > 0)
                {
                    soTienMienGiam = SoTien * (p.PhanTramGiam / 100m);
                    finalSoTien = SoTien - soTienMienGiam.Value;
                }

                var hp = new HocPhi
                {
                    IdHocSinh = hs.IdHocSinh,
                    IdNamHoc = IdNamHoc,
                    IdHocKy = IdHocKy,
                    HocKy = hocKy.TenHocKy,
                    SoTien = finalSoTien,
                    HanDongTien = HanDongTien,
                    TrangThai = 0,
                    GhiChu = GhiChu,
                    PhanTramMienGiam = p?.PhanTramGiam,
                    SoTienMienGiam = soTienMienGiam,
                    LyDoMienGiam = p?.LyDo
                };
                _context.HocPhis.Add(hp);

                var emails = phuHuynhEmails.Where(x => x.IdHocSinh == hs.IdHocSinh).Select(x => x.Email).ToList();
                foreach (var email in emails)
                {
                    if (!string.IsNullOrEmpty(email))
                    {
                        string subject = $"[Thông báo] Học phí mới của học sinh {hs.HoTen}";
                        string body = $"Kính gửi Phụ huynh,\n\nNhà trường xin thông báo khoản học phí mới cho học sinh {hs.HoTen}:\n- Số tiền: {hp.SoTien:N0} đ\n- Hạn đóng: {hp.HanDongTien?.ToString("dd/MM/yyyy")}\n\nVui lòng hoàn tất thanh toán đúng hạn.\n\nTrân trọng,\nNhà trường.";
                        try { await _emailSender.SendAsync(email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); /* Ignore email errors so it doesn't block the UI */ }
                    }
                }

                if (!emails.Any() && !string.IsNullOrWhiteSpace(hs.Email))
                {
                    string subject = $"[Thông báo] Học phí mới của học sinh {hs.HoTen}";
                    string body = $"Kính gửi Học sinh / Phụ huynh,\n\nNhà trường xin thông báo khoản học phí mới cho học sinh {hs.HoTen}:\n- Số tiền: {hp.SoTien:N0} đ\n- Hạn đóng: {hp.HanDongTien?.ToString("dd/MM/yyyy")}\n\nVui lòng hoàn tất thanh toán đúng hạn.\n\nTrân trọng,\nNhà trường.";
                    try { await _emailSender.SendAsync(hs.Email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); }
                }
            }

            if (!IdLop.HasValue && !string.IsNullOrEmpty(Khoi))
            {
                var tb = new ThongBao
                {
                    TieuDe = $"Thông báo đóng học phí Khối {Khoi}",
                    NoiDung = $"Đã có thông báo đóng học phí mới ({SoTien:N0} đ). Hạn đóng: {HanDongTien?.ToString("dd/MM/yyyy")}. {GhiChu}",
                    NgayTao = DateTime.Now,
                    IdTaiKhoan = HttpContext.Session.GetInt32("UserId") ?? 1,
                    DoiTuongNhan = 2 // 2 = Học sinh
                };
                _context.ThongBaos.Add(tb);
            }

            _context.SaveChanges();
            return Success(nameof(HocPhi), $"Đã tạo học phí cho {hocSinhs.Count} học sinh.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult LuuMienGiam(ChinhSachMienGiam model)
        {
            if (!ModelState.IsValid || model.PhanTramGiam < 0 || model.PhanTramGiam > 100 ||
                !_context.HocSinhs.Any(x => x.IdHocSinh == model.IdHocSinh))
                return Error(nameof(DanhSachMienGiam), "Học sinh hoặc phần trăm miễn giảm không hợp lệ (0–100%).");
            if (_context.ChinhSachMienGiams.Any(x => x.IdHocSinh == model.IdHocSinh && x.HieuLuc == model.HieuLuc))
                return Error(nameof(DanhSachMienGiam), "Học sinh đã có chính sách miễn giảm trong thời gian này.");
            _context.ChinhSachMienGiams.Add(model);
            _context.SaveChanges();
            return RedirectToAction(nameof(DanhSachMienGiam));
        }
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult DownloadMienGiamTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("MienGiam");
            ws.Cell(1, 1).Value = "Mã học sinh";
            ws.Cell(1, 2).Value = "Phần trăm giảm";
            ws.Cell(1, 3).Value = "Lý do";
            ws.Cell(1, 4).Value = "Hiệu lực";
            ws.Cell(1, 5).Value = "Ghi chú";

            ws.Cell(2, 1).Value = "HS001";
            ws.Cell(2, 2).Value = "50";
            ws.Cell(2, 3).Value = "Hộ nghèo";
            ws.Cell(2, 4).Value = "2026-2027";
            ws.Cell(2, 5).Value = "Ghi chú mẫu...";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Mau_Nhap_Mien_Giam.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult ImportMienGiamExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return Error(nameof(DanhSachMienGiam), "Vui lòng chọn file Excel.");

            try
            {
                using var stream = new MemoryStream();
                excelFile.CopyTo(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                int count = 0;
                foreach (var row in rows)
                {
                    var maHs = row.Cell(1).GetValue<string>();
                    var phanTramGiamStr = row.Cell(2).GetValue<string>();
                    var lyDo = row.Cell(3).GetValue<string>();
                    var hieuLuc = row.Cell(4).GetValue<string>();
                    var ghiChu = row.Cell(5).GetValue<string>();

                    if (string.IsNullOrWhiteSpace(maHs)) continue;

                    var hocSinh = _context.HocSinhs.FirstOrDefault(x => x.MaHS == maHs.Trim());
                    if (hocSinh != null)
                    {
                        decimal.TryParse(phanTramGiamStr, out decimal phanTramGiam);
                        var policy = new ChinhSachMienGiam
                        {
                            IdHocSinh = hocSinh.IdHocSinh,
                            PhanTramGiam = phanTramGiam,
                            LyDo = lyDo,
                            HieuLuc = hieuLuc,
                            GhiChu = ghiChu
                        };
                        _context.ChinhSachMienGiams.Add(policy);
                        count++;
                    }
                }

                _context.SaveChanges();
                return Success(nameof(DanhSachMienGiam), $"Đã nhập thành công {count} chính sách miễn giảm từ Excel.");
            }
            catch (Exception ex)
            {
                return Error(nameof(DanhSachMienGiam), "Lỗi khi xử lý file Excel: " + ex.Message);
            }
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult DanhSachMienGiam()
        {
            var data = _context.ChinhSachMienGiams
                               .Include(x => x.HocSinh)
                               .ThenInclude(x => x!.LopHoc)
                               .OrderBy(x => x.HocSinh!.LopHoc!.TenLop)
                               .ThenBy(x => x.HocSinh!.HoTen)
                               .ToList();

            ViewBag.LopHocs = GetLopSelectList();
            ViewBag.NamHocs = GetNamHocSelectList();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult XoaMienGiam(int id)
        {
            var policy = _context.ChinhSachMienGiams.Find(id);
            if (policy != null)
            {
                _context.ChinhSachMienGiams.Remove(policy);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(DanhSachMienGiam));
        }

        [HttpGet]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult GetHocSinhByLop(int lopId)
        {
            var data = _context.HocSinhs
                               .Where(x => x.IdLopHoc == lopId)
                               .Select(x => new { id = x.IdHocSinh, ten = x.HoTen })
                               .OrderBy(x => x.ten)
                               .ToList();
            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult XacNhanDongHocPhi(int id, string? phuongThuc)
        {
            var hocPhi = _context.HocPhis.Find(id);
            if (hocPhi == null)
            {
                return NotFound();
            }

            if (hocPhi.TrangThai == 1)
                return Success(nameof(HocPhi), "Khoản học phí đã được xác nhận trước đó.");
            hocPhi.TrangThai = 1;
            hocPhi.NgayDong = DateTime.Today;
            hocPhi.PhuongThuc = phuongThuc;
            _context.SaveChanges();
            return Success(nameof(HocPhi), "Da xac nhan dong hoc phi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult XoaHocPhi(int id)
        {
            var hocPhi = _context.HocPhis.Find(id);
            if (hocPhi == null)
            {
                return NotFound();
            }

            if (hocPhi.TrangThai == 1)
                return Error(nameof(HocPhi), "Không thể xóa khoản học phí đã thanh toán. Cần thực hiện đối soát trước.");
            _context.HocPhis.Remove(hocPhi);
            _context.SaveChanges();
            return Success(nameof(HocPhi), "Da xoa khoan hoc phi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(3, 4)]
        public IActionResult DongHocPhi(int id, string? phuongThuc)
        {
            var student = GetCurrentHocSinh();
            if (student == null) return NotFound();
            var fee = _context.HocPhis.AsNoTracking()
                .FirstOrDefault(x => x.IdHocPhi == id && x.IdHocSinh == student.IdHocSinh);
            if (fee == null) return NotFound();
            if (fee.TrangThai == 1)
                return Success(nameof(XemHocPhi), "Khoản học phí đã được thanh toán.");
            return Error(nameof(XemHocPhi), "Thanh toán trực tuyến chưa được cấu hình. Vui lòng liên hệ nhà trường để thanh toán và xác nhận học phí.");
        }

        [HttpGet]
        [RoleAuthorize(3, 4)]
        public IActionResult VnPayReturn(int id)
        {
            // No verified gateway transaction exists; URL parameters are not proof of payment.
            return Error(nameof(XemHocPhi), "Chưa thể xác thực giao dịch. Vui lòng liên hệ nhà trường để đối soát học phí.");
        }
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult PhieuDiem()
        {
            return View(new PhieuDiemPageViewModel
            {
                DanhSach = _context.PhieuDiems
                    .Include(x => x.HocSinh)
                    .Include(x => x.LopHoc)
                    .Include(x => x.NamHoc)
                    .Include(x => x.HocKy)
                    .OrderByDescending(x => x.NgayLap)
                    .ToList(),
                HocSinhs = GetHocSinhSelectList(),
                LopHocs = GetLopSelectList()
                    .Select(x => new SelectListItem(x.Text, x.Text))
                    .ToList(),
                NamHocs = GetNamHocSelectList(),
                HocKys = GetHocKySelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult TaoPhieuDiem(int idHocSinh, int idNamHoc, int idHocKy)
        {
            var hocSinh = _context.HocSinhs.Find(idHocSinh);
            var hocKy = _context.HocKys.FirstOrDefault(x => x.IdHocKy == idHocKy && x.IdNamHoc == idNamHoc);
            if (hocSinh == null || hocKy == null)
            {
                return Error(nameof(PhieuDiem), "Thong tin phieu diem khong hop le.");
            }

            var phieu = new PhieuDiem
            {
                IdHocSinh = idHocSinh,
                IdLop = hocSinh.IdLopHoc,
                IdNamHoc = idNamHoc,
                IdHocKy = idHocKy,
                NgayLap = DateTime.Now,
                NguoiLap = HttpContext.Session.GetInt32("UserId")
            };

            _context.PhieuDiems.Add(phieu);
            _context.SaveChanges();
            return RedirectToAction(nameof(PhieuDiem));
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult InPhieuDiem(int id)
        {
            var phieu = _context.PhieuDiems
                .Include(x => x.HocSinh)
                .Include(x => x.LopHoc)
                .Include(x => x.NamHoc)
                .Include(x => x.HocKy)
                .FirstOrDefault(x => x.IdPhieuDiem == id);
            if (phieu == null)
            {
                return NotFound();
            }

            var diems = _context.Diems.Include(x => x.MonHoc)
                .Where(x => x.IdHocSinh == phieu.IdHocSinh
                    && x.IdNamHoc == phieu.IdNamHoc
                    && x.IdHocKy == phieu.IdHocKy)
                .OrderBy(x => x.MonHoc!.TenMon)
                .ToList();

            diems = diems.GroupBy(d => d.IdMonHoc).Select(g => g.OrderByDescending(d => d.IdDiem).First()).ToList();
            var pdf = BuildScoreReportPdf(phieu, diems);
            return File(pdf, "application/pdf", $"PhieuDiem_{phieu.HocSinh?.MaHS}_{phieu.NamHoc?.TenNamHoc}.pdf");
        }

        private byte[] BuildScoreReportPdf(PhieuDiem phieu, List<Diem> diems)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("eSCHOOL - PHIEU DIEM").Bold().FontSize(20).FontColor(Colors.Blue.Darken3);
                        column.Item().AlignCenter().Text($"{phieu.HocKy?.TenHocKy} - Nam hoc {phieu.NamHoc?.TenNamHoc}");
                    });
                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(12);
                        column.Item().Text($"Hoc sinh: {phieu.HocSinh?.HoTen} ({phieu.HocSinh?.MaHS})").Bold();
                        column.Item().Text($"Lop: {phieu.LopHoc?.TenLop ?? "Chua xep lop"}");
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                foreach (var title in new[] { "Mon hoc", "15 phut", "1 tiet", "Giua ky", "Cuoi ky", "TB" })
                                {
                                    header.Cell().Background(Colors.Blue.Lighten4).Border(1).Padding(6).Text(title).Bold();
                                }
                            });

                            foreach (var diem in diems)
                            {
                                table.Cell().Border(1).Padding(6).Text(diem.MonHoc?.TenMon ?? string.Empty);
                                table.Cell().Border(1).Padding(6).Text(FormatScore(diem.Diem15Phut));
                                table.Cell().Border(1).Padding(6).Text(FormatScore(diem.Diem1Tiet));
                                table.Cell().Border(1).Padding(6).Text(FormatScore(diem.DiemGiuaKy));
                                table.Cell().Border(1).Padding(6).Text(FormatScore(diem.DiemCuoiKy));
                                table.Cell().Border(1).Padding(6).Text(FormatScore(diem.DiemTB)).Bold();
                            }
                        });

                        var average = diems.Where(x => x.DiemTB.HasValue).Select(x => x.DiemTB!.Value).DefaultIfEmpty().Average();
                        column.Item().AlignRight().Text($"TB cac mon da co diem: {(diems.Any(x => x.DiemTB.HasValue) ? average.ToString("0.00") : "-")}").Bold();
                    });
                    page.Footer().AlignCenter().Text($"Ngay lap: {phieu.NgayLap:dd/MM/yyyy HH:mm}");
                });
            }).GeneratePdf();
        }

        private static string FormatScore(string? score) => string.IsNullOrWhiteSpace(score) ? "-" : score;
        private static string FormatScore(decimal? score) => score?.ToString("0.00") ?? "-";

        private DiemPageViewModel BuildDiemPage() => new()
        {
            HocSinhs = GetHocSinhSelectList(),
            MonHocs = _context.MonHocs
                .OrderBy(x => x.TenMon)
                .Select(x => new SelectListItem(x.TenMon, x.IdMonHoc.ToString()))
                .ToList(),
            NamHocs = GetNamHocSelectList(),
            HocKys = GetHocKySelectList()
        };

        private List<SelectListItem> GetHocSinhSelectList() =>
            _context.HocSinhs
                .Include(x => x.LopHoc)
                .OrderBy(x => x.HoTen)
                .Select(x => new SelectListItem($"{x.MaHS} - {x.HoTen} - {x.LopHoc!.TenLop}", x.IdHocSinh.ToString()))
                .ToList();

        private List<SelectListItem> GetLopSelectList() =>
            _context.LopHocs
                .OrderBy(x => x.TenLop)
                .Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString()))
                .ToList();

        private List<SelectListItem> GetNamHocSelectList() =>
            _context.NamHocs
                .OrderByDescending(x => x.NgayBatDau)
                .Select(x => new SelectListItem(x.TenNamHoc, x.IdNamHoc.ToString()))
                .ToList();

        private List<SelectListItem> GetHocKySelectList() =>
            _context.HocKys.Include(x => x.NamHoc)
                .OrderByDescending(x => x.NamHoc!.NgayBatDau)
                .ThenBy(x => x.NgayBatDau)
                .Select(x => new SelectListItem($"{x.TenHocKy} - {x.NamHoc!.TenNamHoc}", x.IdHocKy.ToString()))
                .ToList();

        private async Task UpdateQuaHanHocPhi()
        {
            var overdue = _context.HocPhis
                .Where(x => x.TrangThai == 0 && x.HanDongTien.HasValue && x.HanDongTien.Value.Date < DateTime.Today)
                .ToList();

            if (overdue.Count == 0)
            {
                return;
            }

            var overdueHsIds = overdue.Select(x => x.IdHocSinh).ToList();
            var parentEmails = _context.HocSinhPhuHuynhs
                .Include(x => x.PhuHuynh)
                .Where(x => overdueHsIds.Contains(x.IdHocSinh) && x.PhuHuynh != null && x.PhuHuynh.Email != null && x.PhuHuynh.Email != "")
                .Select(x => new { x.IdHocSinh, x.PhuHuynh.Email })
                .ToList();
            var hsDict = _context.HocSinhs.Where(x => overdueHsIds.Contains(x.IdHocSinh)).ToDictionary(x => x.IdHocSinh, x => x.HoTen);

            foreach (var item in overdue)
            {
                item.TrangThai = 2;
                
                var emails = parentEmails.Where(x => x.IdHocSinh == item.IdHocSinh).Select(x => x.Email).ToList();
                foreach (var email in emails)
                {
                    if (!string.IsNullOrEmpty(email))
                    {
                        hsDict.TryGetValue(item.IdHocSinh, out string? hsName);
                        string subject = $"[Nhắc nhở] Học phí quá hạn của học sinh {hsName}";
                        string body = $"Kính gửi Phụ huynh,\n\nHọc phí trị giá {item.SoTien:N0} đ của học sinh {hsName} đã quá hạn đóng vào ngày {item.HanDongTien?.ToString("dd/MM/yyyy")}.\n\nVui lòng sắp xếp hoàn tất thanh toán trong thời gian sớm nhất.\n\nTrân trọng,\nNhà trường.";
                        try { await _emailSender.SendAsync(email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); /* Ignore email errors so it doesn't block the UI */ }
                    }
                }

                if (!emails.Any())
                {
                    var hsInfo = _context.HocSinhs.FirstOrDefault(x => x.IdHocSinh == item.IdHocSinh);
                    if (hsInfo != null && !string.IsNullOrWhiteSpace(hsInfo.Email))
                    {
                        hsDict.TryGetValue(item.IdHocSinh, out string? hsName);
                        string subject = $"[Nhắc nhở] Học phí quá hạn của học sinh {hsName}";
                        string body = $"Kính gửi Học sinh / Phụ huynh,\n\nHọc phí trị giá {item.SoTien:N0} đ của học sinh {hsName} đã quá hạn đóng vào ngày {item.HanDongTien?.ToString("dd/MM/yyyy")}.\n\nVui lòng sắp xếp hoàn tất thanh toán trong thời gian sớm nhất.\n\nTrân trọng,\nNhà trường.";
                        try { await _emailSender.SendAsync(hsInfo.Email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); }
                    }
                }
            }

            _context.SaveChanges();
        }

        private HocSinh? GetCurrentHocSinh()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (roleId == 4)
            {
                var ph = _context.PhuHuynhs.FirstOrDefault(x => x.IdTaiKhoan == userId);
                if (ph != null)
                {
                    var hsp = _context.HocSinhPhuHuynhs.FirstOrDefault(x => x.IdPhuHuynh == ph.IdPhuHuynh);
                    if (hsp != null)
                    {
                        return _context.HocSinhs
                            .Include(x => x.LopHoc)
                            .FirstOrDefault(x => x.IdHocSinh == hsp.IdHocSinh);
                    }
                }
                return null;
            }

            var hocSinh = _context.HocSinhs
                .Include(x => x.LopHoc)
                .FirstOrDefault(x => x.IdTaiKhoan == userId);

            if (hocSinh == null && !string.IsNullOrWhiteSpace(username))
            {
                hocSinh = _context.HocSinhs
                    .Include(x => x.LopHoc)
                    .FirstOrDefault(x => x.MaHS == username);
            }

            return hocSinh;
        }

        private GiaoVien? GetCurrentGiaoVien()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var username = HttpContext.Session.GetString("Username");

            var giaoVien = _context.GiaoViens.FirstOrDefault(x => x.IdTaiKhoan == userId);
            if (giaoVien == null && !string.IsNullOrWhiteSpace(username))
            {
                giaoVien = _context.GiaoViens.FirstOrDefault(x => x.MaGV == username);
            }

            return giaoVien;
        }

        private IActionResult Success(string action, string message)
        {
            TempData["Success"] = message;
            return RedirectToAction(action);
        }

        private IActionResult Error(string action, string message)
        {
            TempData["Error"] = message;
            return RedirectToAction(action);
        }
    }
}

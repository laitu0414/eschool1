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
    [RoleAuthorize(1, 2, 3, 4)]
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

        [RoleAuthorize(1)]
        public IActionResult NhatKySuaDiem()
        {
            var logs = _context.NhatKyHoatDongs
                .Where(x => x.HanhDong == "Sửa điểm" || x.HanhDong == "Nhập điểm")
                .OrderByDescending(x => x.ThoiGian)
                .ToList();

            return View(logs);
        }

        [RoleAuthorize(1, 2)]
        public IActionResult Diem(int? lopId, int? hocKyId)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var isGiaoVien = roleId == 2;
            var giaoVien = isGiaoVien ? GetCurrentGiaoVien() : null;

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
                var diemsQuery = _context.Diems.AsNoTracking().Where(x => x.IdHocKy == hocKyId.Value);
                var diems = diemsQuery.ToList();
                var diemGroup = diems.GroupBy(x => x.IdHocSinh).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var hs in hocSinhs)
                {
                    decimal? tbHocKy = null;
                    var diemMonHoc = new Dictionary<int, decimal?>();
                    if (diemGroup.TryGetValue(hs.IdHocSinh, out var hsDiems))
                    {
                        if (hsDiems.Any(x => x.DiemTB.HasValue))
                        {
                            tbHocKy = hsDiems.Where(x => x.DiemTB.HasValue).Average(x => x.DiemTB);
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

        [HttpGet]
        [RoleAuthorize(1, 2)]
        public IActionResult GetDiemHocSinh(int idHocSinh, int idHocKy)
        {
            var hocKy = _context.HocKys.Find(idHocKy);
            if (hocKy == null) return NotFound();

            var hocSinh = _context.HocSinhs.Find(idHocSinh);
            if (hocSinh == null) return NotFound();

            var roleId = HttpContext.Session.GetInt32("RoleId");
            var isGiaoVien = roleId == 2;
            var giaoVien = isGiaoVien ? GetCurrentGiaoVien() : null;

            var assignedMonHocs = new List<int>();
            if (isGiaoVien && giaoVien != null && hocSinh.IdLopHoc.HasValue)
            {
                assignedMonHocs = _context.PhanCongGiangDays
                    .Where(x => x.IdGiaoVien == giaoVien.IdGiaoVien && x.IdLop == hocSinh.IdLopHoc.Value)
                    .Select(x => x.IdMonHoc)
                    .ToList();
            }

            var monHocs = _context.MonHocs.OrderBy(x => x.TenMon).ToList();
            var diems = _context.Diems.Where(x => x.IdHocSinh == idHocSinh && x.IdHocKy == idHocKy).ToList();

            var list = monHocs.Select(m =>
            {
                var d = diems.FirstOrDefault(x => x.IdMonHoc == m.IdMonHoc);
                return new DiemMonHocViewModel
                {
                    IdMonHoc = m.IdMonHoc,
                    TenMon = m.TenMon,
                    Diem15Phut = d?.Diem15Phut,
                    Diem1Tiet = d?.Diem1Tiet,
                    DiemGiuaKy = d?.DiemGiuaKy,
                    DiemCuoiKy = d?.DiemCuoiKy,
                    IsEditable = !isGiaoVien || assignedMonHocs.Contains(m.IdMonHoc)
                };
            }).ToList();

            return Ok(new
            {
                idNamHoc = hocKy.IdNamHoc,
                diemMonHocs = list
            });
        }

        [HttpPost]
        [RoleAuthorize(1, 2)]
        public IActionResult LuuDiemHocSinh([FromBody] LuuDiemHocSinhRequest req)
        {
            if (req == null || req.IdHocSinh <= 0 || req.IdHocKy <= 0 || req.IdNamHoc <= 0)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var roleId = HttpContext.Session.GetInt32("RoleId");
            var isGiaoVien = roleId == 2;
            var giaoVien = isGiaoVien ? GetCurrentGiaoVien() : null;

            var hocSinh = _context.HocSinhs.Find(req.IdHocSinh);
            var hocKy = _context.HocKys.FirstOrDefault(x => x.IdHocKy == req.IdHocKy && x.IdNamHoc == req.IdNamHoc);

            if (hocSinh == null || hocKy == null) return BadRequest(new { success = false, message = "Học sinh hoặc học kỳ không tồn tại." });

            var assignedMonHocs = new List<int>();
            if (isGiaoVien && giaoVien != null && hocSinh.IdLopHoc.HasValue)
            {
                assignedMonHocs = _context.PhanCongGiangDays
                    .Where(x => x.IdGiaoVien == giaoVien.IdGiaoVien && x.IdLop == hocSinh.IdLopHoc.Value)
                    .Select(x => x.IdMonHoc)
                    .ToList();
            }

            var existingDiems = _context.Diems
                .Where(x => x.IdHocSinh == req.IdHocSinh && x.IdHocKy == req.IdHocKy && x.IdNamHoc == req.IdNamHoc)
                .ToList();

            foreach (var item in req.DiemMonHocs)
            {
                if (isGiaoVien && !assignedMonHocs.Contains(item.IdMonHoc)) continue;

                var hasGrade = !string.IsNullOrWhiteSpace(item.Diem15Phut) || !string.IsNullOrWhiteSpace(item.Diem1Tiet) || !string.IsNullOrWhiteSpace(item.DiemGiuaKy) || !string.IsNullOrWhiteSpace(item.DiemCuoiKy);
                var diem = existingDiems.FirstOrDefault(x => x.IdMonHoc == item.IdMonHoc);

                if (diem == null && hasGrade)
                {
                    diem = new Diem
                    {
                        IdHocSinh = req.IdHocSinh,
                        IdMonHoc = item.IdMonHoc,
                        IdHocKy = req.IdHocKy,
                        IdNamHoc = req.IdNamHoc,
                        HocKy = hocKy.TenHocKy
                    };
                    _context.Diems.Add(diem);
                }

                if (diem != null)
                {
                    diem.Diem15Phut = item.Diem15Phut;
                    diem.Diem1Tiet = item.Diem1Tiet;
                    diem.DiemGiuaKy = item.DiemGiuaKy;
                    diem.DiemCuoiKy = item.DiemCuoiKy;
                    diem.TinhDiemTrungBinh();
                }
            }

            if (roleId == 1) // If Admin
            {
                var username = HttpContext.Session.GetString("Username") ?? "Admin";
                _nhatKyService.GhiLog(username, "Sửa điểm", $"Sửa điểm cho học sinh {hocSinh.HoTen} ({hocSinh.MaHS}) ở học kỳ {hocKy.TenHocKy}");
            }

            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã lưu điểm thành công." });
        }

        [RoleAuthorize(1, 2)]
        public IActionResult DownloadDiemTemplate(int lopId)
        {
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

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_NhapDiem.xlsx");
        }

        [RoleAuthorize(1, 2)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportDiemExcel(IFormFile? file, int lopId, int hocKyId, int monHocId)
        {
            if (file == null || file.Length <= 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToAction(nameof(Diem), new { lopId, hocKyId });
            }

            var hocKy = _context.HocKys.Find(hocKyId);
            if (hocKy == null) return NotFound();

            int successCount = 0;
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed()?.RowsUsed()?.Skip(1);

                if (rows == null || !rows.Any())
                {
                    TempData["Error"] = "File Excel không có dữ liệu.";
                    return RedirectToAction(nameof(Diem), new { lopId, hocKyId });
                }

                foreach (var row in rows)
                {
                    var worksheetRow = row.WorksheetRow();
                    if (!int.TryParse(worksheetRow.Cell(1).GetString(), out int idHocSinh)) continue;

                    var d15 = worksheetRow.Cell(4).GetString()?.Trim();
                    var d1t = worksheetRow.Cell(5).GetString()?.Trim();
                    var dgk = worksheetRow.Cell(6).GetString()?.Trim();
                    var dck = worksheetRow.Cell(7).GetString()?.Trim();

                    var diem = _context.Diems.FirstOrDefault(x => x.IdHocSinh == idHocSinh && x.IdMonHoc == monHocId && x.IdHocKy == hocKyId);
                    if (diem == null)
                    {
                        if (string.IsNullOrWhiteSpace(d15) && string.IsNullOrWhiteSpace(d1t) && string.IsNullOrWhiteSpace(dgk) && string.IsNullOrWhiteSpace(dck)) continue;

                        diem = new Diem
                        {
                            IdHocSinh = idHocSinh,
                            IdMonHoc = monHocId,
                            IdHocKy = hocKyId,
                            IdNamHoc = hocKy.IdNamHoc,
                            HocKy = hocKy.TenHocKy
                        };
                        _context.Diems.Add(diem);
                    }

                    diem.Diem15Phut = string.IsNullOrWhiteSpace(d15) ? null : d15;
                    diem.Diem1Tiet = string.IsNullOrWhiteSpace(d1t) ? null : d1t;
                    diem.DiemGiuaKy = string.IsNullOrWhiteSpace(dgk) ? null : dgk;
                    diem.DiemCuoiKy = string.IsNullOrWhiteSpace(dck) ? null : dck;
                    diem.TinhDiemTrungBinh();
                    successCount++;
                }

                if (successCount > 0)
                {
                    var username = HttpContext.Session.GetString("Username") ?? "Admin";
                    _nhatKyService.GhiLog(username, "Nhập điểm", $"Nhập điểm từ Excel cho {successCount} học sinh lớp có ID {lopId} ở học kỳ {hocKy.TenHocKy}");

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã cập nhật điểm cho {successCount} học sinh thành công.";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy dữ liệu hợp lệ để cập nhật.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi nhập dữ liệu: {ex.Message}";
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

            ViewBag.HocSinh = hocSinh;
            ViewBag.FilterNamHocId = namHocId;
            ViewBag.FilterHocKyId = hocKyId;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(1)]
        public IActionResult LuuDiem(DiemFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Error(nameof(Diem), "Thong tin diem chua hop le.");
            }

            var hocKy = _context.HocKys.Include(x => x.NamHoc)
                .FirstOrDefault(x => x.IdHocKy == vm.IdHocKy && x.IdNamHoc == vm.IdNamHoc);
            if (hocKy == null)
            {
                return Error(nameof(Diem), "Hoc ky khong thuoc nam hoc da chon.");
            }

            var diem = vm.IdDiem > 0
                ? _context.Diems.Find(vm.IdDiem)
                : _context.Diems.FirstOrDefault(x =>
                    x.IdHocSinh == vm.IdHocSinh &&
                    x.IdMonHoc == vm.IdMonHoc &&
                    x.IdHocKy == vm.IdHocKy &&
                    x.IdNamHoc == vm.IdNamHoc);

            if (diem == null)
            {
                diem = new Diem();
                _context.Diems.Add(diem);
            }

            diem.IdHocSinh = vm.IdHocSinh;
            diem.IdMonHoc = vm.IdMonHoc;
            diem.IdHocKy = vm.IdHocKy;
            diem.IdNamHoc = vm.IdNamHoc;
            diem.HocKy = hocKy.TenHocKy;
            diem.Diem15Phut = vm.Diem15Phut;
            diem.Diem1Tiet = vm.Diem1Tiet;
            diem.DiemGiuaKy = vm.DiemGiuaKy;
            diem.DiemCuoiKy = vm.DiemCuoiKy;
            diem.TinhDiemTrungBinh();

            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            var hocSinhName = _context.HocSinhs.Find(vm.IdHocSinh)?.HoTen ?? "Không rõ";
            _nhatKyService.GhiLog(username, "Sửa điểm", $"Sửa điểm (form) cho học sinh {hocSinhName} ở học kỳ {hocKy.TenHocKy}");

            _context.SaveChanges();

            return Success(nameof(Diem), "Da luu diem.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(1)]
        public IActionResult XoaDiem(int id)
        {
            var diem = _context.Diems.Find(id);
            if (diem == null)
            {
                return NotFound();
            }

            _context.Diems.Remove(diem);
            _context.SaveChanges();
            return Success(nameof(Diem), "Da xoa diem.");
        }

        [RoleAuthorize(1)]
        public IActionResult DiemDanh(string? namHocId, int? lopId, DateTime? ngay)
        {
            var assignments = GetAllAttendanceAssignments();

            if (!string.IsNullOrWhiteSpace(namHocId) && int.TryParse(namHocId, out int nHocId))
            {
                var tenNamHoc = _context.NamHocs.Find(nHocId)?.TenNamHoc;
                if (!string.IsNullOrEmpty(tenNamHoc))
                {
                    assignments = assignments.Where(x => x.NamHoc == tenNamHoc).ToList();
                }
            }

            var sessions = BuildAttendanceSessionSummaries(assignments);

            if (ngay.HasValue)
            {
                sessions = sessions.Where(x => x.NgayHoc.Date == ngay.Value.Date).ToList();
            }

            if (lopId.HasValue)
            {
                sessions = sessions.Where(x => x.IdLop == lopId.Value).ToList();
            }

            var vm = new AdminDiemDanhPageViewModel
            {
                LopHocs = GetLopSelectList(),
                NamHocs = GetNamHocSelectList(),
                DanhSachBuoiHoc = sessions
                    .OrderByDescending(x => x.NgayHoc)
                    .ThenBy(x => x.TenLop)
                    .ThenBy(x => x.IdTietHoc)
                    .ToList()
            };

            ViewBag.Ngay = ngay?.ToString("yyyy-MM-dd");
            ViewBag.LopId = lopId;
            ViewBag.NamHocId = namHocId;
            return View(vm);
        }

        [RoleAuthorize(1)]
        public IActionResult ChiTietDiemDanh(int lopId, DateTime ngayHoc, int? idTietHoc)
        {
            var assignments = GetAllAttendanceAssignments();
            var session = BuildAttendanceSessionSummaries(assignments)
                .FirstOrDefault(x => x.IdLop == lopId && x.NgayHoc.Date == ngayHoc.Date && x.IdTietHoc == idTietHoc);

            if (session == null)
            {
                return NotFound();
            }

            var details = _context.DiemDanhs
                .Include(x => x.HocSinh)
                .Where(x => x.IdLop == lopId && x.NgayHoc.Date == ngayHoc.Date && x.IdTietHoc == idTietHoc)
                .OrderBy(x => x.HocSinh!.HoTen)
                .Select(x => new DiemDanhHocSinhChiTietViewModel
                {
                    MaHS = x.HocSinh!.MaHS,
                    HoTen = x.HocSinh!.HoTen,
                    TrangThai = x.TrangThai,
                    GhiChu = x.GhiChu
                })
                .ToList();

            return View(new ChiTietDiemDanhPageViewModel
            {
                BuoiHoc = session,
                ChiTietHocSinhs = details
            });
        }

        [RoleAuthorize(2)]
        public IActionResult GiaoVienDiemDanh(int? idPhanCong, DateTime? ngayHoc)
        {
            var giaoVien = GetCurrentGiaoVien();
            if (giaoVien == null)
            {
                return NotFound("Tai khoan nay chua duoc lien ket voi ho so giao vien.");
            }

            var assignments = GetTeacherAttendanceAssignments(giaoVien.IdGiaoVien);
            if (!idPhanCong.HasValue)
            {
                idPhanCong = assignments.FirstOrDefault()?.IdPhanCong;
            }

            var selectedAssignment = assignments.FirstOrDefault(x => x.IdPhanCong == idPhanCong);
            var ngay = ngayHoc?.Date ?? DateTime.Today;

            var vm = new GiaoVienDiemDanhPageViewModel
            {
                IdPhanCong = idPhanCong,
                NgayHoc = ngay,
                PhanCongs = assignments,
                PhanCongDangChon = selectedAssignment
            };

            if (selectedAssignment != null)
            {
                var hocSinhs = _context.HocSinhs
                    .Where(x => x.IdLopHoc == selectedAssignment.IdLop)
                    .OrderBy(x => x.HoTen)
                    .ToList();

                var attendanceMap = _context.DiemDanhs
                    .Where(x => x.IdLop == selectedAssignment.IdLop
                        && x.NgayHoc.Date == ngay
                        && x.IdTietHoc == selectedAssignment.TietBatDau)
                    .ToDictionary(x => x.IdHocSinh);

                vm.HocSinhs = hocSinhs.Select(x =>
                {
                    attendanceMap.TryGetValue(x.IdHocSinh, out var record);
                    return new GiaoVienDiemDanhHocSinhViewModel
                    {
                        IdHocSinh = x.IdHocSinh,
                        MaHS = x.MaHS,
                        HoTen = x.HoTen,
                        CoMat = record == null || string.Equals(record.TrangThai, "Co mat", StringComparison.OrdinalIgnoreCase) || string.Equals(record.TrangThai, "Có mặt", StringComparison.OrdinalIgnoreCase),
                        GhiChu = record?.GhiChu
                    };
                }).ToList();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(2)]
        public IActionResult LuuDiemDanhGiaoVien(GiaoVienLuuDiemDanhViewModel vm)
        {
            var giaoVien = GetCurrentGiaoVien();
            if (giaoVien == null)
            {
                return NotFound("Tai khoan nay chua duoc lien ket voi ho so giao vien.");
            }

            var assignment = _context.PhanCongGiangDays
                .FirstOrDefault(x => x.IdPhanCong == vm.IdPhanCong && x.IdGiaoVien == giaoVien.IdGiaoVien);
            if (assignment == null)
            {
                return NotFound();
            }

            var validStudentIds = _context.HocSinhs
                .Where(x => x.IdLopHoc == assignment.IdLop)
                .Select(x => x.IdHocSinh)
                .ToHashSet();

            var existing = _context.DiemDanhs
                .Where(x => x.IdLop == assignment.IdLop
                    && x.NgayHoc.Date == vm.NgayHoc.Date
                    && x.IdTietHoc == assignment.TietBatDau)
                .ToList()
                .ToDictionary(x => x.IdHocSinh);

            foreach (var item in vm.HocSinhs.Where(x => validStudentIds.Contains(x.IdHocSinh)))
            {
                if (!existing.TryGetValue(item.IdHocSinh, out var record))
                {
                    record = new DiemDanh();
                    _context.DiemDanhs.Add(record);
                }

                record.IdHocSinh = item.IdHocSinh;
                record.IdLop = assignment.IdLop;
                record.NgayHoc = vm.NgayHoc.Date;
                record.IdTietHoc = assignment.TietBatDau;
                record.TrangThai = item.CoMat ? "Co mat" : "Vang";
                record.GhiChu = string.IsNullOrWhiteSpace(item.GhiChu) ? null : item.GhiChu.Trim();
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu điểm danh cho lớp học.";
            return RedirectToAction(nameof(GiaoVienDiemDanh), new
            {
                idPhanCong = vm.IdPhanCong,
                ngayHoc = vm.NgayHoc.ToString("yyyy-MM-dd")
            });
        }

        [RoleAuthorize(3, 4)]
        public IActionResult XemDiemDanh(int? idMonHoc)
        {
            var hocSinh = GetCurrentHocSinh();
            if (hocSinh == null)
            {
                return NotFound("Tài khoản này chưa được liên kết với hồ sơ học sinh.");
            }

            var attendanceRecords = _context.DiemDanhs
                .Include(x => x.LopHoc)
                .Where(x => x.IdHocSinh == hocSinh.IdHocSinh)
                .OrderByDescending(x => x.NgayHoc)
                .ToList();

            var classIds = attendanceRecords.Select(x => x.IdLop).Distinct().ToList();
            if (hocSinh.IdLopHoc.HasValue)
            {
                classIds.Add(hocSinh.IdLopHoc.Value);
            }

            classIds = classIds.Distinct().ToList();

            var assignments = _context.PhanCongGiangDays
                .Include(x => x.MonHoc)
                .Include(x => x.GiaoVien)
                .Include(x => x.LopHoc)
                .Where(x => classIds.Contains(x.IdLop))
                .AsNoTracking()
                .ToList();

            var monHocs = assignments
                .Where(x => x.MonHoc != null)
                .GroupBy(x => x.IdMonHoc)
                .Select(x => x.First())
                .OrderBy(x => x.MonHoc!.TenMon)
                .Select(x => new SelectListItem(x.MonHoc!.TenMon, x.IdMonHoc.ToString()))
                .ToList();

            if (!idMonHoc.HasValue && monHocs.Count > 0)
            {
                idMonHoc = int.Parse(monHocs[0].Value);
            }

            var history = attendanceRecords
                .Select(record =>
                {
                    var assignment = MatchAssignment(record, assignments);
                    return new
                    {
                        Assignment = assignment,
                        Item = new HocSinhDiemDanhChiTietViewModel
                        {
                            NgayHoc = record.NgayHoc,
                            TenMonHoc = assignment?.MonHoc?.TenMon ?? "Chưa xác định",
                            TenGiaoVien = assignment?.GiaoVien?.HoTen ?? "Chưa cập nhật",
                            TenLop = record.LopHoc?.TenLop ?? string.Empty,
                            IdTietHoc = record.IdTietHoc,
                            TrangThai = record.TrangThai,
                            GhiChu = record.GhiChu
                        }
                    };
                })
                .Where(x => !idMonHoc.HasValue || x.Assignment?.IdMonHoc == idMonHoc.Value)
                .Select(x => x.Item)
                .OrderByDescending(x => x.NgayHoc)
                .ThenBy(x => x.IdTietHoc)
                .ToList();

            return View(new HocSinhDiemDanhPageViewModel
            {
                MonHocs = monHocs,
                IdMonHoc = idMonHoc,
                TenMonHocDangChon = monHocs.FirstOrDefault(x => x.Value == idMonHoc?.ToString())?.Text ?? "Tất cả môn học",
                LichSuDiemDanh = history
            });
        }

        [RoleAuthorize(1)]
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
        [RoleAuthorize(1)]
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
        [RoleAuthorize(1)]
        public async Task<IActionResult> LuuHocPhiHangLoat(int IdNamHoc, int IdHocKy, string? Khoi, int? IdLop, decimal SoTien, DateTime? HanDongTien, string? GhiChu)
        {
            var hocKy = _context.HocKys.FirstOrDefault(x => x.IdHocKy == IdHocKy && x.IdNamHoc == IdNamHoc);
            if (hocKy == null)
                return Error(nameof(HocPhi), "Học kỳ hoặc năm học không hợp lệ.");

            var hocSinhsQuery = _context.HocSinhs.Include(x => x.LopHoc).AsQueryable();
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
                                   .Where(x => hsIds.Contains(x.IdHocSinh))
                                   .ToList();

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
        [RoleAuthorize(1)]
        public IActionResult LuuMienGiam(ChinhSachMienGiam model)
        {
            _context.ChinhSachMienGiams.Add(model);
            _context.SaveChanges();
            return RedirectToAction(nameof(DanhSachMienGiam));
        }
        [RoleAuthorize(1)]
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
        [RoleAuthorize(1)]
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

        [RoleAuthorize(1)]
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
        [RoleAuthorize(1)]
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
        [RoleAuthorize(1)]
        public IActionResult XacNhanDongHocPhi(int id, string? phuongThuc)
        {
            var hocPhi = _context.HocPhis.Find(id);
            if (hocPhi == null)
            {
                return NotFound();
            }

            hocPhi.TrangThai = 1;
            hocPhi.NgayDong = DateTime.Today;
            hocPhi.PhuongThuc = phuongThuc;
            _context.SaveChanges();
            return Success(nameof(HocPhi), "Da xac nhan dong hoc phi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(1)]
        public IActionResult XoaHocPhi(int id)
        {
            var hocPhi = _context.HocPhis.Find(id);
            if (hocPhi == null)
            {
                return NotFound();
            }

            _context.HocPhis.Remove(hocPhi);
            _context.SaveChanges();
            return Success(nameof(HocPhi), "Da xoa khoan hoc phi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(3, 4)]
        public async Task<IActionResult> DongHocPhi(int id, string? phuongThuc)
        {
            var hocSinh = GetCurrentHocSinh();
            if (hocSinh == null)
            {
                return NotFound("Tai khoan nay chua duoc lien ket voi ho so hoc sinh.");
            }

            var hocPhi = _context.HocPhis.FirstOrDefault(x => x.IdHocPhi == id && x.IdHocSinh == hocSinh.IdHocSinh);
            if (hocPhi == null)
            {
                return NotFound();
            }

            if (hocPhi.TrangThai == 1)
            {
                return Success(nameof(XemHocPhi), "Khoản học phí này đã được thanh toán trước đó.");
            }
            
            if (phuongThuc == "VNPAY")
            {
                string vnp_Returnurl = Url.Action("VnPayReturn", "KetQuaHocTap", new { id = hocPhi.IdHocPhi }, Request.Scheme) ?? "";
                string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                string vnp_TmnCode = "8241VKPZ"; // Mã website tại VNPAY (dummy)
                string vnp_HashSecret = "BPKRATFNMZUPZTLNAVOAMUVDAALOMZKN"; // Chuỗi bí mật (dummy)

                var vnpayData = new SortedList<string, string>(new VnPayCompare())
                {
                    { "vnp_Version", "2.1.0" },
                    { "vnp_Command", "pay" },
                    { "vnp_TmnCode", vnp_TmnCode },
                    { "vnp_Amount", ((long)(hocPhi.SoTien * 100)).ToString() },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CurrCode", "VND" },
                    { "vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1" },
                    { "vnp_Locale", "vn" },
                    { "vnp_OrderInfo", $"Thanh toan hoc phi {hocPhi.HocKy}" },
                    { "vnp_OrderType", "other" },
                    { "vnp_ReturnUrl", vnp_Returnurl },
                    { "vnp_TxnRef", DateTime.Now.Ticks.ToString() }
                };

                var queryString = new System.Text.StringBuilder();
                foreach (var kv in vnpayData)
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        queryString.Append(System.Net.WebUtility.UrlEncode(kv.Key) + "=" + System.Net.WebUtility.UrlEncode(kv.Value) + "&");
                    }
                }
                
                string signDataStr = queryString.ToString().TrimEnd('&');
                string vnp_SecureHash = HmacSHA512(vnp_HashSecret, signDataStr);
                queryString.Append("vnp_SecureHash=" + vnp_SecureHash);
                
                string paymentUrl = vnp_Url + "?" + queryString.ToString();
                return Redirect(paymentUrl);
            }

            if (phuongThuc != "QR")
            {
                return Error(nameof(XemHocPhi), "Phương thức thanh toán không hợp lệ.");
            }

            var emails = _context.HocSinhPhuHuynhs
                .Include(x => x.PhuHuynh)
                .Where(x => x.IdHocSinh == hocSinh.IdHocSinh && x.PhuHuynh != null && x.PhuHuynh.Email != null && x.PhuHuynh.Email != "")
                .Select(x => x.PhuHuynh.Email)
                .ToList();

            string subject = $"[Thanh toán QR] Học phí của học sinh {hocSinh.HoTen}";
            string qrData = Uri.EscapeDataString($"TUITION_PAYMENT_{hocPhi.IdHocPhi}_{hocPhi.SoTien}");
            string qrUrl = $"https://quickchart.io/qr?text={qrData}&size=300";

            if (emails.Any())
            {
                string body = $"Kính gửi Phụ huynh,\n\nHọc sinh {hocSinh.HoTen} đã chọn thanh toán khoản học phí trị giá {hocPhi.SoTien:N0} đ.\n\nVui lòng quét mã QR tại đường dẫn sau để tiến hành thanh toán:\n{qrUrl}\n\nTrân trọng,\nNhà trường.";
                foreach (var email in emails)
                {
                    if (!string.IsNullOrEmpty(email))
                    {
                        try { await _emailSender.SendAsync(email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); }
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(hocSinh.Email))
            {
                string body = $"Kính gửi Học sinh / Phụ huynh,\n\nBạn đã chọn thanh toán khoản học phí trị giá {hocPhi.SoTien:N0} đ.\n\nVui lòng quét mã QR tại đường dẫn sau để tiến hành thanh toán:\n{qrUrl}\n\nTrân trọng,\nNhà trường.";
                try { await _emailSender.SendAsync(hocSinh.Email, subject, body); } catch (Exception ex) { Console.WriteLine($"Email send failed: {ex}"); }
            }

            return Success(nameof(XemHocPhi), "Đã gửi mã QR thanh toán vào email của phụ huynh.");
        }

        [RoleAuthorize(3, 4)]
        public IActionResult VnPayReturn(int id)
        {
            var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
            
            if (vnp_ResponseCode == "00")
            {
                // Payment success
                var hocPhi = _context.HocPhis.Find(id);
                if (hocPhi != null && hocPhi.TrangThai != 1)
                {
                    hocPhi.TrangThai = 1;
                    hocPhi.NgayDong = DateTime.Today;
                    hocPhi.PhuongThuc = "VNPAY";
                    _context.SaveChanges();
                    return Success(nameof(XemHocPhi), "Thanh toán qua VNPAY thành công.");
                }
            }
            
            return Error(nameof(XemHocPhi), "Thanh toán qua VNPAY thất bại hoặc bị hủy.");
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new System.Text.StringBuilder();
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new System.Security.Cryptography.HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        public class VnPayCompare : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                var Compare = System.Globalization.CompareInfo.GetCompareInfo("en-US");
                return Compare.Compare(x, y, System.Globalization.CompareOptions.Ordinal);
            }
        }
        [RoleAuthorize(1)]
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
                NamHocs = GetNamHocSelectList(),
                HocKys = GetHocKySelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(1)]
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
            return RedirectToAction(nameof(InPhieuDiem), new { id = phieu.IdPhieuDiem });
        }

        [RoleAuthorize(1)]
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
                        column.Item().AlignRight().Text($"Diem trung binh chung: {(diems.Any(x => x.DiemTB.HasValue) ? average.ToString("0.00") : "-")}").Bold();
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
                .OrderBy(x => x.HoTen)
                .Select(x => new SelectListItem($"{x.MaHS} - {x.HoTen}", x.IdHocSinh.ToString()))
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

        private List<GiaoVienDiemDanhPhanCongViewModel> GetTeacherAttendanceAssignments(int giaoVienId)
        {
            return _context.PhanCongGiangDays
                .Include(x => x.MonHoc)
                .Include(x => x.LopHoc)
                .Where(x => x.IdGiaoVien == giaoVienId)
                .OrderBy(x => x.LopHoc!.TenLop)
                .ThenBy(x => x.Thu)
                .ThenBy(x => x.TietBatDau)
                .Select(x => new GiaoVienDiemDanhPhanCongViewModel
                {
                    IdPhanCong = x.IdPhanCong,
                    IdLop = x.IdLop,
                    IdMonHoc = x.IdMonHoc,
                    TenLop = x.LopHoc!.TenLop,
                    TenMonHoc = x.MonHoc!.TenMon,
                    HocKy = x.HocKy,
                    NamHoc = x.NamHoc,
                    Thu = x.Thu,
                    TietBatDau = x.TietBatDau,
                    SoTiet = x.SoTiet
                })
                .ToList();
        }

        private List<PhanCongGiangDay> GetAllAttendanceAssignments()
        {
            return _context.PhanCongGiangDays
                .Include(x => x.MonHoc)
                .Include(x => x.GiaoVien)
                .Include(x => x.LopHoc)
                .AsNoTracking()
                .ToList();
        }

        private List<DiemDanhBuoiHocViewModel> BuildAttendanceSessionSummaries(List<PhanCongGiangDay> assignments)
        {
            var records = _context.DiemDanhs
                .Include(x => x.LopHoc)
                .AsNoTracking()
                .ToList();

            return records
                .GroupBy(x => new { x.IdLop, Ngay = x.NgayHoc.Date, x.IdTietHoc })
                .Select(group =>
                {
                    var first = group.First();
                    var assignment = MatchAssignment(first, assignments);
                    return new DiemDanhBuoiHocViewModel
                    {
                        IdLop = group.Key.IdLop,
                        TenLop = first.LopHoc?.TenLop ?? $"Lop {group.Key.IdLop}",
                        NgayHoc = group.Key.Ngay,
                        IdTietHoc = group.Key.IdTietHoc,
                        IdMonHoc = assignment?.IdMonHoc,
                        TenMonHoc = assignment?.MonHoc?.TenMon ?? "Chua xac dinh",
                        TenGiaoVien = assignment?.GiaoVien?.HoTen ?? "Chua cap nhat",
                        TongHocSinh = group.Count(),
                        SoHocSinhCoMat = group.Count(x => string.Equals(x.TrangThai, "Co mat", StringComparison.OrdinalIgnoreCase) || string.Equals(x.TrangThai, "Có mặt", StringComparison.OrdinalIgnoreCase)),
                        SoHocSinhVang = group.Count(x => !string.Equals(x.TrangThai, "Co mat", StringComparison.OrdinalIgnoreCase) && !string.Equals(x.TrangThai, "Có mặt", StringComparison.OrdinalIgnoreCase))
                    };
                })
                .ToList();
        }

        private PhanCongGiangDay? MatchAssignment(DiemDanh record, List<PhanCongGiangDay> assignments)
        {
            var thu = GetThuFromDate(record.NgayHoc);
            return assignments.FirstOrDefault(x =>
                x.IdLop == record.IdLop
                && x.Thu == thu
                && (!record.IdTietHoc.HasValue
                    || (!x.TietBatDau.HasValue)
                    || (record.IdTietHoc.Value >= x.TietBatDau.Value
                        && record.IdTietHoc.Value <= x.TietBatDau.Value + (x.SoTiet ?? 1) - 1)));
        }

        private static int GetThuFromDate(DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 3,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Friday => 6,
                DayOfWeek.Saturday => 7,
                _ => 8
            };
        }

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

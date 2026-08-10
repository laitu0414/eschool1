using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Controllers
{
    [RoleAuthorize(1, 3)]
    public class ChuyenLopController : Controller
    {
        private readonly AppDbContext _context;

        public ChuyenLopController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult LichSu()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var query = _context.ChuyenLops
                .Include(x => x.HocSinh)
                .AsQueryable();

            if (roleId == 3) // Học sinh
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var hocSinhId = _context.HocSinhs
                    .Where(x => x.IdTaiKhoan == userId)
                    .Select(x => x.IdHocSinh)
                    .FirstOrDefault();
                
                if (hocSinhId > 0)
                {
                    query = query.Where(x => x.IdHocSinh == hocSinhId);
                }
            }

            var data = query.OrderByDescending(x => x.NgayChuyen).ToList();

            ViewBag.LopNames = _context.LopHocs
                .ToDictionary(x => x.IdLop, x => x.TenLop);
            return View("Index", data); // Render the old Index view which shows history
        }

        [RoleAuthorize(1)]
        public IActionResult Index(int? lopId, string? namHoc)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == 3) return RedirectToAction(nameof(LichSu));

            ViewBag.NamHocs = _context.NamHocs.OrderByDescending(x => x.NgayBatDau).Select(x => new SelectListItem(x.TenNamHoc, x.TenNamHoc)).ToList();
            ViewBag.LopHocs = _context.LopHocs.OrderBy(x => x.TenLop).Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString())).ToList();
            
            ViewBag.SelectedNamHoc = namHoc;
            ViewBag.SelectedLopId = lopId;

            var hocSinhs = new List<HocSinh>();
            if (lopId.HasValue)
            {
                hocSinhs = _context.HocSinhs
                    .Include(x => x.LopHoc)
                    .Where(x => x.IdLopHoc == lopId && x.TrangThai == true)
                    .OrderBy(x => x.HoTen)
                    .ToList();
            }

            return View("StudentList", hocSinhs);
        }

        [RoleAuthorize(1)]
        public IActionResult Create(int id)
        {
            var hocSinh = _context.HocSinhs
                .Include(x => x.LopHoc)
                .FirstOrDefault(x => x.IdHocSinh == id);

            if (hocSinh == null)
                return NotFound();

            return View(CreateViewModel(hocSinh));
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ChuyenLopViewModel vm)
        {
            var hocSinh = _context.HocSinhs
                .Include(x => x.LopHoc)
                .FirstOrDefault(x => x.IdHocSinh == vm.IdHocSinh);

            if (hocSinh == null)
                return NotFound();

            vm.IdLopCu = hocSinh.IdLopHoc ?? 0;
            vm.MaHS = hocSinh.MaHS;
            vm.HoTen = hocSinh.HoTen;
            vm.LopCu = hocSinh.LopHoc?.TenLop ?? "Chưa có lớp";

            if (!_context.LopHocs.Any(x => x.IdLop == vm.IdLopMoi))
                ModelState.AddModelError(nameof(vm.IdLopMoi), "Lớp mới không tồn tại");

            if (vm.IdLopCu == vm.IdLopMoi)
                ModelState.AddModelError(nameof(vm.IdLopMoi), "Lớp mới không được trùng lớp hiện tại");

            if (!ModelState.IsValid)
            {
                vm.LopHocs = GetLopHocCungKhoiSelectList(hocSinh.LopHoc?.Khoi, vm.IdLopCu);
                return View(vm);
            }

            _context.ChuyenLops.Add(new ChuyenLop
            {
                IdHocSinh = vm.IdHocSinh,
                IdLopCu = vm.IdLopCu,
                IdLopMoi = vm.IdLopMoi,
                NgayChuyen = vm.NgayChuyen,
                LyDo = vm.LyDo?.Trim(),
                GhiChu = vm.GhiChu?.Trim()
            });

            if (vm.NgayChuyen.Date <= DateTime.Today)
            {
                hocSinh.IdLopHoc = vm.IdLopMoi;
            }
            
            _context.SaveChanges();

            TempData["Success"] = "Chuyển lớp thành công";
            return RedirectToAction("Index", new { lopId = vm.IdLopCu });
        }

        private ChuyenLopViewModel CreateViewModel(HocSinh hocSinh)
        {
            return new ChuyenLopViewModel
            {
                IdHocSinh = hocSinh.IdHocSinh,
                MaHS = hocSinh.MaHS,
                HoTen = hocSinh.HoTen,
                IdLopCu = hocSinh.IdLopHoc ?? 0,
                LopCu = hocSinh.LopHoc?.TenLop ?? "Chưa có lớp",
                NgayChuyen = DateTime.Today,
                LopHocs = GetLopHocCungKhoiSelectList(hocSinh.LopHoc?.Khoi, hocSinh.IdLopHoc ?? 0)
            };
        }

        private List<SelectListItem> GetLopHocCungKhoiSelectList(string? khoi, int currentLopId)
        {
            var query = _context.LopHocs.AsQueryable();
            if (!string.IsNullOrEmpty(khoi))
            {
                query = query.Where(x => x.Khoi == khoi);
            }
            return query
                .Where(x => x.IdLop != currentLopId)
                .OrderBy(x => x.TenLop)
                .Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString()))
                .ToList();
        }

        [RoleAuthorize(1)]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ChuyenLop");

            worksheet.Cell(1, 1).Value = "Mã HS (*)";
            worksheet.Cell(1, 2).Value = "Lớp chuyển đến (*)";
            worksheet.Cell(1, 3).Value = "Ngày chuyển (dd/MM/yyyy) (*)";
            worksheet.Cell(1, 4).Value = "Lý do";
            worksheet.Cell(1, 5).Value = "Ghi chú";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            worksheet.Cell(2, 1).Value = "HS001";
            worksheet.Cell(2, 2).Value = "6A2";
            worksheet.Cell(2, 3).Value = DateTime.Today.ToString("dd/MM/yyyy");
            worksheet.Cell(2, 4).Value = "Theo nguyện vọng";
            worksheet.Cell(2, 5).Value = "Chuyển theo đơn xin";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ChuyenLop_Template.xlsx");
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile? file)
        {
            if (file == null || file.Length <= 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToAction(nameof(Index));
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Chỉ hỗ trợ định dạng file .xlsx";
                return RedirectToAction(nameof(Index));
            }

            int successCount = 0;
            int skipCount = 0;

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
                    return RedirectToAction(nameof(Index));
                }

                var lopHocs = _context.LopHocs.ToList();

                foreach (var row in rows)
                {
                    var maHS = row.Cell(1).GetString().Trim();
                    var tenLopMoi = row.Cell(2).GetString().Trim();
                    
                    if (string.IsNullOrWhiteSpace(maHS) || string.IsNullOrWhiteSpace(tenLopMoi))
                    {
                        skipCount++;
                        continue;
                    }

                    var hocSinh = _context.HocSinhs.Include(x => x.LopHoc).FirstOrDefault(x => x.MaHS == maHS);
                    if (hocSinh == null || hocSinh.IdLopHoc == null)
                    {
                        skipCount++;
                        continue;
                    }

                    var lopMoi = lopHocs.FirstOrDefault(l => l.TenLop.Equals(tenLopMoi, StringComparison.OrdinalIgnoreCase));
                    if (lopMoi == null || lopMoi.IdLop == hocSinh.IdLopHoc)
                    {
                        skipCount++;
                        continue;
                    }

                    DateTime ngayChuyen = DateTime.Today;
                    var ngayChuyenStr = row.Cell(3).GetString().Trim();
                    if (DateTime.TryParseExact(ngayChuyenStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        ngayChuyen = parsedDate;
                    }
                    else if (row.Cell(3).TryGetValue<DateTime>(out var cellDate))
                    {
                        ngayChuyen = cellDate;
                    }

                    var chuyenLop = new ChuyenLop
                    {
                        IdHocSinh = hocSinh.IdHocSinh,
                        IdLopCu = hocSinh.IdLopHoc.Value,
                        IdLopMoi = lopMoi.IdLop,
                        NgayChuyen = ngayChuyen,
                        LyDo = row.Cell(4).GetString().Trim(),
                        GhiChu = row.Cell(5).GetString().Trim()
                    };

                    _context.ChuyenLops.Add(chuyenLop);
                    
                    if (ngayChuyen.Date <= DateTime.Today)
                    {
                        hocSinh.IdLopHoc = lopMoi.IdLop;
                    }
                    
                    successCount++;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã nhập thành công {successCount} học sinh chuyển lớp. Bỏ qua {skipCount} dòng (lỗi hoặc trùng lặp).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi đọc file Excel: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

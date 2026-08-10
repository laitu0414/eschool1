using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
namespace eSchool.Controllers
{
    [RoleAuthorize(1, 2)]
    public class GiaoVienController : Controller
    {
        private readonly AppDbContext _context;

        public GiaoVienController(AppDbContext context)
        {
            _context = context;
        }

        [RoleAuthorize(1)]
        public IActionResult Index(string? keyword)
        {
            var query = _context.GiaoViens
                .Include(x => x.MonHoc)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaGV.Contains(keyword) ||
                    x.HoTen.Contains(keyword) ||
                    (x.SDT != null && x.SDT.Contains(keyword)));
            }

            ViewBag.Keyword = keyword;
            ViewBag.MonHocs = GetMonHocSelectList();
            return View(query.OrderBy(x => x.HoTen).ToList());
        }

        public IActionResult HoSo(int id)
        {
            if (IsTeacher())
            {
                var currentGiaoVienId = GetCurrentGiaoVienId();
                if (!currentGiaoVienId.HasValue)
                    return NotFound("Tài khoản này chưa được liên kết với hồ sơ giáo viên.");

                if (currentGiaoVienId.Value != id)
                    return RedirectToAction(nameof(HoSoCaNhan));
            }

            var giaoVien = _context.GiaoViens
                .Include(x => x.TaiKhoan)
                .Include(x => x.MonHoc)
                .Include(x => x.LopChuNhiems)
                .FirstOrDefault(x => x.IdGiaoVien == id);

            if (giaoVien == null)
            {
                return NotFound();
            }

            return View(giaoVien);
        }

        [RoleAuthorize(2)]
        public IActionResult HoSoCaNhan()
        {
            var giaoVienId = GetCurrentGiaoVienId();
            var giaoVien = giaoVienId.HasValue
                ? _context.GiaoViens
                    .Include(x => x.TaiKhoan)
                    .FirstOrDefault(x => x.IdGiaoVien == giaoVienId.Value)
                : null;

            if (giaoVien == null)
            {
                return NotFound("Tài khoản này chưa được liên kết với hồ sơ giáo viên.");
            }

            return RedirectToAction(nameof(HoSo), new { id = giaoVien.IdGiaoVien });
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(GiaoVienFormViewModel vm)
        {
            NormalizeGiaoVien(vm);
            ValidateGiaoVien(vm);

            if (_context.GiaoViens.Any(x => x.MaGV == vm.MaGV))
            {
                TempData["Error"] = "Mã giáo viên đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin giáo viên chưa hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = _context.TaiKhoans.FirstOrDefault(t => t.Username == vm.MaGV.Trim());
            if (taiKhoan == null)
            {
                taiKhoan = new TaiKhoan
                {
                    Username = vm.MaGV.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    IdChucVu = 2,
                    TrangThai = true,
                    BatBuocDoiMatKhau = true
                };
                _context.TaiKhoans.Add(taiKhoan);
                _context.SaveChanges();
            }

            _context.GiaoViens.Add(new GiaoVien
            {
                MaGV = vm.MaGV.Trim(),
                HoTen = vm.HoTen.Trim(),
                NgaySinh = vm.NgaySinh,
                GioiTinh = vm.GioiTinh,
                SDT = vm.SDT,
                Email = vm.Email,
                DiaChi = vm.DiaChi,
                IdMonHoc = vm.IdMonHoc,
                IdTaiKhoan = taiKhoan.IdTaiKhoan
            });
            _context.SaveChanges();

            TempData["Success"] = "Đã thêm giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(GiaoVienFormViewModel vm)
        {
            var giaoVien = _context.GiaoViens.Find(vm.IdGiaoVien);
            if (giaoVien == null) return NotFound();

            NormalizeGiaoVien(vm);
            ValidateGiaoVien(vm);

            if (_context.GiaoViens.Any(x => x.MaGV == vm.MaGV && x.IdGiaoVien != vm.IdGiaoVien))
            {
                TempData["Error"] = "Mã giáo viên đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin giáo viên chưa hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            giaoVien.MaGV = vm.MaGV.Trim();
            giaoVien.HoTen = vm.HoTen.Trim();
            giaoVien.NgaySinh = vm.NgaySinh;
            giaoVien.GioiTinh = vm.GioiTinh;
            giaoVien.SDT = vm.SDT;
            giaoVien.Email = vm.Email;
            giaoVien.DiaChi = vm.DiaChi;
            giaoVien.IdMonHoc = vm.IdMonHoc;
            _context.SaveChanges();

            TempData["Success"] = "Đã cập nhật giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var giaoVien = _context.GiaoViens
                .Include(x => x.PhanCongGiangDays)
                .Include(x => x.LopChuNhiems)
                .FirstOrDefault(x => x.IdGiaoVien == id);

            if (giaoVien == null) return NotFound();

            if (giaoVien.PhanCongGiangDays?.Any() == true || giaoVien.LopChuNhiems?.Any() == true)
            {
                TempData["Error"] = "Không thể xóa giáo viên đang có phân công hoặc lớp chủ nhiệm.";
                return RedirectToAction(nameof(Index));
            }

            _context.GiaoViens.Remove(giaoVien);
            _context.SaveChanges();
            TempData["Success"] = "Đã xóa giáo viên.";
            return RedirectToAction(nameof(Index));
        }



        [RoleAuthorize(1)]
        public IActionResult ChuNhiem()
        {
            return View(new ChuNhiemViewModel
            {
                LopHocs = _context.LopHocs
                    .Include(x => x.GiaoVienChuNhiem)
                    .OrderBy(x => x.TenLop)
                    .ToList(),
                GiaoViens = GetGiaoVienSelectList()
            });
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GanChuNhiem(int idLop, int? idGiaoVien)
        {
            var lop = _context.LopHocs.Find(idLop);
            if (lop == null) return NotFound();

            if (idGiaoVien.HasValue && !_context.GiaoViens.Any(x => x.IdGiaoVien == idGiaoVien.Value))
            {
                return NotFound();
            }

            lop.IdGiaoVienCN = idGiaoVien;
            _context.SaveChanges();
            TempData["Success"] = idGiaoVien.HasValue
                ? "Đã cập nhật giáo viên chủ nhiệm."
                : "Đã bỏ phân công chủ nhiệm.";
            return RedirectToAction(nameof(ChuNhiem));
        }

        [RoleAuthorize(1, 2)]
        public IActionResult ThoiKhoaBieu(int? giaoVienId, string? hocKy, string? namHoc, DateTime? tuan)
        {
            if (IsTeacher())
            {
                giaoVienId = GetCurrentGiaoVienId();
                if (!giaoVienId.HasValue)
                    return NotFound("Tài khoản này chưa được liên kết với hồ sơ giáo viên.");
            }
            else
            {
                giaoVienId ??= _context.GiaoViens
                    .OrderBy(x => x.HoTen)
                    .Select(x => (int?)x.IdGiaoVien)
                    .FirstOrDefault();
            }

            var query = _context.PhanCongGiangDays
                .Include(x => x.GiaoVien)
                .Include(x => x.MonHoc)
                .Include(x => x.LopHoc)
                .Include(x => x.PhongHoc)
                .AsNoTracking()
                .AsQueryable();

            if (giaoVienId.HasValue)
                query = query.Where(x => x.IdGiaoVien == giaoVienId.Value);

            if (!string.IsNullOrWhiteSpace(hocKy))
                query = query.Where(x => x.HocKy == hocKy || x.HocKy == "Cả năm");

            if (!string.IsNullOrWhiteSpace(namHoc))
                query = query.Where(x => x.NamHoc == namHoc);

            ViewBag.GiaoVienId = giaoVienId;
            ViewBag.HocKy = hocKy;
            ViewBag.NamHoc = namHoc;
            
            var selectedDate = tuan ?? DateTime.Today;
            var diff = (7 + (selectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = selectedDate.AddDays(-diff);
            
            ViewBag.Tuan = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.StartOfWeek = startOfWeek.ToString("yyyy-MM-dd");
            
            var endDate = startOfWeek.AddDays(6);
            var thayDois = _context.LichHocThayDois
                .Where(x => x.Ngay >= startOfWeek && x.Ngay <= endDate)
                .ToList();
            ViewBag.ThayDois = thayDois;
            
            var phongHocMap = _context.PhongHocs.Where(x => x.IdLop != null).ToDictionary(x => x.IdLop.Value, x => x.MaPhong);
            ViewBag.PhongHocMap = phongHocMap;
            
            if (IsTeacher() && giaoVienId.HasValue)
            {
                var currentTeacher = _context.GiaoViens.Find(giaoVienId.Value);
                if (currentTeacher != null)
                {
                    ViewBag.GiaoViens = new List<SelectListItem>
                    {
                        new SelectListItem($"{currentTeacher.MaGV} - {currentTeacher.HoTen}", currentTeacher.IdGiaoVien.ToString())
                    };
                }
                else
                {
                    ViewBag.GiaoViens = new List<SelectListItem>();
                }
            }
            else
            {
                ViewBag.GiaoViens = GetGiaoVienSelectList();
            }

            return View(query
                .OrderBy(x => x.Thu)
                .ThenBy(x => x.TietBatDau)
                .ToList());
        }

        [RoleAuthorize(2)]
        public IActionResult QuanLyKyLuat()
        {
            var giaoVienId = GetCurrentGiaoVienId();
            if (!giaoVienId.HasValue) return NotFound("Tài khoản này chưa được liên kết với hồ sơ giáo viên.");

            var dsLopCN = _context.LopHocs.Where(x => x.IdGiaoVienCN == giaoVienId.Value).ToList();
            var lstKyLuat = _context.KyLuats
                .Include(x => x.HocSinh)
                .ThenInclude(x => x.LopHoc)
                .Where(x => x.HocSinh != null && x.HocSinh.IdLopHoc != null && dsLopCN.Select(l => l.IdLop).Contains(x.HocSinh.IdLopHoc.Value))
                .OrderByDescending(x => x.NgayViPham)
                .ToList();
            
            ViewBag.DanhSachLop = dsLopCN;
            ViewBag.DanhSachHocSinh = _context.HocSinhs
                .Where(x => x.IdLopHoc != null && dsLopCN.Select(l => l.IdLop).Contains(x.IdLopHoc.Value))
                .Select(x => new SelectListItem($"{x.MaHS} - {x.HoTen} (Lớp {x.LopHoc.TenLop})", x.IdHocSinh.ToString()))
                .ToList();

            return View(lstKyLuat);
        }

        [RoleAuthorize(2)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemKyLuat(KyLuat model)
        {
            var giaoVienId = GetCurrentGiaoVienId();
            if (!giaoVienId.HasValue) return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin kỷ luật không hợp lệ.";
                return RedirectToAction(nameof(QuanLyKyLuat));
            }

            model.IdGiaoVien = giaoVienId.Value;
            model.TrangThai = true;
            _context.KyLuats.Add(model);
            _context.SaveChanges();
            
            TempData["Success"] = "Đã thêm biên bản kỷ luật thành công.";
            return RedirectToAction(nameof(QuanLyKyLuat));
        }

        [RoleAuthorize(2)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaKyLuat(int id)
        {
            var kyLuat = _context.KyLuats.Find(id);
            if (kyLuat != null)
            {
                _context.KyLuats.Remove(kyLuat);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa kỷ luật.";
            }
            return RedirectToAction(nameof(QuanLyKyLuat));
        }

        [RoleAuthorize(2)]
        public IActionResult YeuCauBaoTri()
        {
            var giaoVienId = GetCurrentGiaoVienId();
            if (!giaoVienId.HasValue) return NotFound("Tài khoản này chưa được liên kết với hồ sơ giáo viên.");

            var dsLopCN = _context.LopHocs.Where(x => x.IdGiaoVienCN == giaoVienId.Value).Select(x => x.IdLop).ToList();
            if (!dsLopCN.Any())
            {
                TempData["Error"] = "Bạn chưa được phân công chủ nhiệm lớp nào.";
                return View(new List<BaoTri>());
            }

            var dsPhongHoc = _context.PhongHocs.Where(x => x.IdLop.HasValue && dsLopCN.Contains(x.IdLop.Value)).ToList();
            var dsPhongHocIds = dsPhongHoc.Select(x => x.IdPhongHoc).ToList();
            var dsThietBi = _context.ThietBis.Where(x => dsPhongHocIds.Contains(x.IdPhongHoc)).ToList();
            var dsThietBiIds = dsThietBi.Select(x => x.IdThietBi).ToList();

            var lstBaoTri = _context.BaoTris
                .Include(x => x.ThietBi)
                .ThenInclude(t => t.PhongHoc)
                .ThenInclude(p => p.LopHoc)
                .Where(x => dsThietBiIds.Contains(x.IdThietBi))
                .OrderByDescending(x => x.NgayBaoTri)
                .ToList();

            ViewBag.DanhSachPhong = dsPhongHoc.Select(x => new SelectListItem(x.TenPhong, x.IdPhongHoc.ToString())).ToList();
            ViewBag.DanhSachThietBi = dsThietBi.Select(x => new { Id = x.IdThietBi, Ten = $"{x.TenTB} ({x.MaTB})", PhongId = x.IdPhongHoc }).ToList();
            
            return View(lstBaoTri);
        }

        [RoleAuthorize(2)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoYeuCauBaoTri(BaoTri model)
        {
            var giaoVienId = GetCurrentGiaoVienId();
            if (!giaoVienId.HasValue) return NotFound();

            var thietBi = _context.ThietBis.Find(model.IdThietBi);
            if (thietBi == null)
            {
                TempData["Error"] = "Thiết bị không hợp lệ.";
                return RedirectToAction(nameof(YeuCauBaoTri));
            }

            model.MaBaoTri = "YC" + DateTime.Now.ToString("yyyyMMddHHmmss");
            model.NgayBaoTri = DateTime.Now;
            model.TrangThai = "Chờ xử lý";
            model.ChiPhi = 0;
            model.NguoiThucHien = "";
            model.KetQua = "";

            _context.BaoTris.Add(model);
            
            thietBi.TinhTrang = "Hỏng"; // Update device status to Broken/Needs repair
            
            _context.SaveChanges();

            TempData["Success"] = "Đã gửi yêu cầu bảo trì thành công.";
            return RedirectToAction(nameof(YeuCauBaoTri));
        }

        private PhanCongGiangDayViewModel BuildPhanCongViewModel(PhanCongGiangDayViewModel? vm = null)
        {
            vm ??= new PhanCongGiangDayViewModel();
            vm.DanhSach = _context.PhanCongGiangDays
                .Include(x => x.GiaoVien)
                .Include(x => x.MonHoc)
                .Include(x => x.LopHoc)
                .OrderByDescending(x => x.IdPhanCong)
                .ToList();
            vm.GiaoViens = GetGiaoVienSelectList();
            vm.MonHocs = _context.MonHocs
                .OrderBy(x => x.TenMon)
                .Select(x => new SelectListItem(x.TenMon, x.IdMonHoc.ToString()))
                .ToList();
            vm.LopHocs = _context.LopHocs
                .OrderBy(x => x.TenLop)
                .Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString()))
                .ToList();
            vm.GiaoVienMonHocIds = GetGiaoVienMonHocMap();
            return vm;
        }

        private List<SelectListItem> GetGiaoVienSelectList()
        {
            return _context.GiaoViens
                .OrderBy(x => x.HoTen)
                .Select(x => new SelectListItem($"{x.MaGV} - {x.HoTen}", x.IdGiaoVien.ToString()))
                .ToList();
        }

        private List<SelectListItem> GetMonHocSelectList()
        {
            return _context.MonHocs
                .OrderBy(x => x.TenMon)
                .Select(x => new SelectListItem(x.TenMon, x.IdMonHoc.ToString()))
                .ToList();
        }

        private Dictionary<int, int?> GetGiaoVienMonHocMap()
        {
            return _context.GiaoViens
                .ToDictionary(x => x.IdGiaoVien, x => x.IdMonHoc);
        }

        private bool ValidateGiaoVienMonDay(int idGiaoVien, int idMonHoc, out string errorMessage)
        {
            errorMessage = string.Empty;
            var giaoVien = _context.GiaoViens.Find(idGiaoVien);
            if (giaoVien != null && giaoVien.IdMonHoc != idMonHoc)
            {
                errorMessage = "Giáo viên không được phân công dạy môn học này.";
                return false;
            }
            return true;
        }

        private void NormalizeGiaoVien(GiaoVienFormViewModel vm)
        {
            vm.MaGV = vm.MaGV?.Trim() ?? string.Empty;
            vm.HoTen = vm.HoTen?.Trim() ?? string.Empty;
            vm.GioiTinh = vm.GioiTinh?.Trim();
            vm.SDT = vm.SDT?.Trim();
            vm.Email = vm.Email?.Trim();
            vm.DiaChi = vm.DiaChi?.Trim();
        }

        private void ValidateGiaoVien(GiaoVienFormViewModel vm)
        {
            if (vm.NgaySinh.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(vm.NgaySinh), "Ngày sinh không được lớn hơn ngày hiện tại.");
            }
        }

        private bool IsTeacher()
        {
            return HttpContext.Session.GetInt32("RoleId") == 2;
        }

        private int? GetCurrentGiaoVienId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var username = HttpContext.Session.GetString("Username");

            var giaoVienId = _context.GiaoViens
                .Where(x => x.IdTaiKhoan == userId)
                .Select(x => (int?)x.IdGiaoVien)
                .FirstOrDefault();

            if (giaoVienId.HasValue)
                return giaoVienId;

            if (string.IsNullOrWhiteSpace(username))
                return null;

            return _context.GiaoViens
                .Where(x => x.MaGV == username)
                .Select(x => (int?)x.IdGiaoVien)
                .FirstOrDefault();
        }

        [RoleAuthorize(1)]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("GiaoVien");

            // Headers
            worksheet.Cell(1, 1).Value = "Mã GV (*)";
            worksheet.Cell(1, 2).Value = "Họ Tên (*)";
            worksheet.Cell(1, 3).Value = "Ngày Sinh (dd/MM/yyyy) (*)";
            worksheet.Cell(1, 4).Value = "Giới Tính";
            worksheet.Cell(1, 5).Value = "SĐT";
            worksheet.Cell(1, 6).Value = "Email";
            worksheet.Cell(1, 7).Value = "Địa Chỉ";
            worksheet.Cell(1, 8).Value = "Mã Môn";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Cell(2, 1).Value = "GV001";
            worksheet.Cell(2, 2).Value = "Trần Văn B";
            worksheet.Cell(2, 3).Value = "01/01/1980";
            worksheet.Cell(2, 4).Value = "Nam";
            worksheet.Cell(2, 5).Value = "0912345678";
            worksheet.Cell(2, 6).Value = "tranvanb@example.com";
            worksheet.Cell(2, 7).Value = "Hà Nội";
            worksheet.Cell(2, 8).Value = "TOAN";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "GiaoVien_Template.xlsx");
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
                var dictMonHoc = _context.MonHocs.ToDictionary(m => m.MaMon, m => m.IdMonHoc);
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed()?.RowsUsed()?.Skip(1);

                if (rows == null || !rows.Any())
                {
                    TempData["Error"] = "File Excel không có dữ liệu.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var row in rows)
                {
                    var worksheetRow = row.WorksheetRow();
                    var maGV = worksheetRow.Cell(1).GetString().Trim();
                    var hoTen = worksheetRow.Cell(2).GetString().Trim();
                    
                    if (string.IsNullOrWhiteSpace(maGV) || string.IsNullOrWhiteSpace(hoTen))
                    {
                        skipCount++;
                        continue;
                    }

                    if (_context.GiaoViens.Any(x => x.MaGV == maGV))
                    {
                        skipCount++;
                        continue;
                    }

                    DateTime ngaySinh = DateTime.Today;
                    var ngaySinhStr = worksheetRow.Cell(3).GetString().Trim();
                    if (DateTime.TryParseExact(ngaySinhStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        ngaySinh = parsedDate;
                    }
                    else if (worksheetRow.Cell(3).TryGetValue<DateTime>(out var cellDate))
                    {
                        ngaySinh = cellDate;
                    }

                    var maMon = worksheetRow.Cell(8).GetString().Trim();
                    int? idMonHoc = null;
                    if (!string.IsNullOrWhiteSpace(maMon) && dictMonHoc.TryGetValue(maMon, out var foundId))
                    {
                        idMonHoc = foundId;
                    }

                    var taiKhoan = _context.TaiKhoans.FirstOrDefault(t => t.Username == maGV);
                    if (taiKhoan == null)
                    {
                        taiKhoan = new TaiKhoan
                        {
                            Username = maGV,
                            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                            IdChucVu = 2,
                            TrangThai = true,
                            BatBuocDoiMatKhau = true
                        };
                        _context.TaiKhoans.Add(taiKhoan);
                        await _context.SaveChangesAsync();
                    }

                    var gv = new GiaoVien
                    {
                        MaGV = maGV,
                        HoTen = hoTen,
                        NgaySinh = ngaySinh,
                        GioiTinh = worksheetRow.Cell(4).GetString().Trim(),
                        SDT = worksheetRow.Cell(5).GetString().Trim(),
                        Email = worksheetRow.Cell(6).GetString().Trim(),
                        DiaChi = worksheetRow.Cell(7).GetString().Trim(),
                        IdTaiKhoan = taiKhoan.IdTaiKhoan,
                        IdMonHoc = idMonHoc
                    };

                    _context.GiaoViens.Add(gv);
                    successCount++;
                }

                if (successCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Đã nhập thành công {successCount} giáo viên. Bỏ qua {skipCount} dòng (lỗi hoặc trùng lặp).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi đọc file Excel: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

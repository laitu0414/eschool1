using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.Services;
using eSchool.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
namespace eSchool.Controllers
{
    [RoleAuthorize(1, 3, 4)]
    public class HocSinhController : Controller
    {
        private static readonly HashSet<string> AllowedImageExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly IHocSinhService _service;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HocSinhController(
            IHocSinhService service,
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _service = service;
            _context = context;
            _environment = environment;
        }

        [RoleAuthorize(1)]
        public IActionResult Index(string? keyword, int? lopId, bool? trangThai)
        {
            SetFilterData(keyword, lopId, trangThai);
            var createForm = new HocSinhViewModel();
            SetFormLists(createForm);
            ViewBag.CreateHocSinhForm = createForm;
            return View(_service.GetAll(keyword, lopId, trangThai));
        }

        [RoleAuthorize(1)]
        public IActionResult Details(int id)
        {
            var data = _service.GetById(id);
            return data == null ? NotFound() : View("HoSo", data);
        }

        [RoleAuthorize(1)]
        public IActionResult Create()
        {
            var vm = new HocSinhViewModel();
            SetFormLists(vm);
            return View(vm);
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HocSinhViewModel vm)
        {
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(t => t.Username == vm.MaHS.Trim());
            if (taiKhoan == null)
            {
                taiKhoan = new TaiKhoan
                {
                    Username = vm.MaHS.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    IdChucVu = 3,
                    TrangThai = true,
                    BatBuocDoiMatKhau = true
                };
                _context.TaiKhoans.Add(taiKhoan);
                _context.SaveChanges();
            }
            vm.IdTaiKhoan = taiKhoan.IdTaiKhoan;
            ValidateStudent(vm);
            ValidateImage(vm.AnhTaiLen);

            if (!ModelState.IsValid)
            {
                SetFormLists(vm);
                return View(vm);
            }

            vm.AnhDaiDien = await SaveImageAsync(vm.AnhTaiLen);

            try
            {
                _service.Add(vm);
            }
            catch
            {
                DeleteImage(vm.AnhDaiDien);
                throw;
            }

            TempData["Success"] = "Thêm học sinh thành công";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize(1)]
        public IActionResult Edit(int id)
        {
            var vm = _service.GetById(id);
            if (vm == null)
                return NotFound();

            SetFormLists(vm, id);
            return View(vm);
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HocSinhViewModel vm)
        {
            var existing = _service.GetById(vm.IdHocSinh);
            if (existing == null)
                return NotFound();

            var linkedAccountId = existing.IdTaiKhoan;
            vm.IdTaiKhoan = null;
            ValidateStudent(vm);
            ValidateImage(vm.AnhTaiLen);

            if (!ModelState.IsValid)
            {
                vm.AnhDaiDien = existing.AnhDaiDien;
                SetFormLists(vm, vm.IdHocSinh);
                return View(vm);
            }

            var oldImage = existing.AnhDaiDien;
            vm.AnhDaiDien = oldImage;
            vm.IdTaiKhoan = linkedAccountId;

            if (vm.AnhTaiLen is { Length: > 0 })
            {
                vm.AnhDaiDien = await SaveImageAsync(vm.AnhTaiLen);
            }

            try
            {
                _service.Update(vm);
            }
            catch
            {
                if (vm.AnhDaiDien != oldImage)
                    DeleteImage(vm.AnhDaiDien);
                throw;
            }

            if (vm.AnhDaiDien != oldImage)
                DeleteImage(oldImage);

            TempData["Success"] = "Cập nhật học sinh thành công";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize(1)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            TempData["Success"] = "Đã chuyển học sinh sang trạng thái nghỉ học";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult HoSo(int id)
        {
            if (HttpContext.Session.GetInt32("RoleId") == 3)
            {
                var currentHocSinhId = GetCurrentHocSinhId();
                if (!currentHocSinhId.HasValue)
                    return NotFound("Tài khoản này chưa được liên kết với hồ sơ học sinh.");

                if (currentHocSinhId.Value != id)
                    return RedirectToAction(nameof(HoSoCaNhan));
            }

            var data = _service.GetById(id);
            return data == null ? NotFound() : View(data);
        }

        [RoleAuthorize(1)]
        public IActionResult TraCuu(string? keyword, int? lopId, bool? trangThai)
        {
            SetFilterData(keyword, lopId, trangThai);
            return View(_service.GetAll(keyword, lopId, trangThai));
        }

        [RoleAuthorize(1)]
        public IActionResult KyLuat(string? namHoc, string? hocKy, int? lopId, string? keyword)
        {
            var query = _context.KyLuats
                .Include(x => x.HocSinh)
                .ThenInclude(x => x.LopHoc)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(namHoc) && !string.IsNullOrWhiteSpace(hocKy) && lopId.HasValue)
            {
                // In a full implementation, you might filter KyLuats by the semester dates or if the model had HocKy/NamHoc fields.
                // For now, it filters by LopHoc and Keyword. We enforce the step-by-step filter visually.
                query = query.Where(x => x.HocSinh != null && x.HocSinh.IdLopHoc == lopId);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(x => x.HocSinh != null && (x.HocSinh.MaHS.Contains(keyword) || x.HocSinh.HoTen.Contains(keyword) || x.HinhThuc.Contains(keyword) || x.LyDo.Contains(keyword)));
                }
            }
            else
            {
                // Return an empty list if the required filters aren't selected, similar to ThoiKhoaBieu and Diem
                query = query.Where(x => false);
            }

            ViewBag.NamHocs = _context.NamHocs.OrderByDescending(x => x.NgayBatDau).Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.TenNamHoc, x.TenNamHoc)).ToList();
            ViewBag.HocKys = _context.HocKys.OrderBy(x => x.TenHocKy).Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.TenHocKy, x.TenHocKy)).ToList();
            ViewBag.LopHocs = _context.LopHocs.OrderBy(x => x.TenLop).Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.TenLop, x.IdLop.ToString())).ToList();
            
            ViewBag.FilterNamHoc = namHoc;
            ViewBag.FilterHocKy = hocKy;
            ViewBag.FilterLopId = lopId;
            ViewBag.Keyword = keyword;

            return View(query.OrderByDescending(x => x.NgayViPham).ToList());
        }

        [RoleAuthorize(1)]
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
            return RedirectToAction(nameof(KyLuat));
        }

        [RoleAuthorize(3, 4)]
        public IActionResult HoSoCaNhan()
        {
            var hocSinhId = GetCurrentHocSinhId();
            var hocSinh = hocSinhId.HasValue
                ? _context.HocSinhs.FirstOrDefault(x => x.IdHocSinh == hocSinhId.Value)
                : null;

            if (hocSinh == null)
                return NotFound("Tài khoản này chưa được liên kết với hồ sơ học sinh.");

            return RedirectToAction(nameof(HoSo), new { id = hocSinh.IdHocSinh });
        }

        [RoleAuthorize(3, 4)]
        public IActionResult ThoiKhoaBieu(string? hocKy, string? namHoc, DateTime? tuan)
        {
            var selectedDate = tuan;
            if (!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }

            var hocSinhId = GetCurrentHocSinhId();
            var hocSinh = hocSinhId.HasValue
                ? _context.HocSinhs
                    .Include(x => x.LopHoc)
                    .FirstOrDefault(x => x.IdHocSinh == hocSinhId.Value)
                : null;

            if (hocSinh == null)
            {
                return NotFound("Tài khoản này chưa được liên kết với hồ sơ học sinh.");
            }

            var diff = (7 + (selectedDate.Value.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = selectedDate.Value.AddDays(-diff);
            var endDate = startOfWeek.AddDays(6);
            
            var currentNamHoc = _context.NamHocs.FirstOrDefault(n => selectedDate.Value.Date >= n.NgayBatDau.Date && selectedDate.Value.Date <= n.NgayKetThuc.Date);
            if (currentNamHoc == null)
            {
                ViewBag.HocSinh = hocSinh;
                ViewBag.HocKy = hocKy;
                ViewBag.NamHoc = namHoc;
                ViewBag.Tuan = selectedDate.Value.ToString("yyyy-MM-dd");
                ViewBag.StartOfWeek = startOfWeek.ToString("yyyy-MM-dd");
                ViewBag.ThayDois = new List<LichHocThayDoi>();
                ViewBag.TenLop = "Ngoài thời gian năm học";
                return View(new List<PhanCongGiangDay>());
            }

            namHoc = currentNamHoc.TenNamHoc;

            var thayDois = _context.LichHocThayDois
                .Where(x => x.Ngay >= startOfWeek && x.Ngay <= endDate)
                .ToList();

            ViewBag.HocSinh = hocSinh;
            ViewBag.HocKy = hocKy;
            ViewBag.NamHoc = namHoc;
            ViewBag.Tuan = selectedDate.Value.ToString("yyyy-MM-dd");
            ViewBag.StartOfWeek = startOfWeek.ToString("yyyy-MM-dd");
            ViewBag.ThayDois = thayDois;

            var chuyenLops = _context.ChuyenLops
                .Where(x => x.IdHocSinh == hocSinh.IdHocSinh)
                .OrderByDescending(x => x.NgayChuyen)
                .ToList();

            var lopHocTheoThu = new Dictionary<int, int>();
            for (int i = 0; i < 7; i++)
            {
                var currentDate = startOfWeek.AddDays(i);
                int currentLopId = hocSinh.IdLopHoc ?? 0;
                
                var latestTransferBeforeOrOnDate = chuyenLops.FirstOrDefault(cl => cl.NgayChuyen.Date <= currentDate.Date);
                if (latestTransferBeforeOrOnDate != null)
                {
                    currentLopId = latestTransferBeforeOrOnDate.IdLopMoi;
                }
                else
                {
                    var earliestTransferAfterDate = chuyenLops.LastOrDefault(cl => cl.NgayChuyen.Date > currentDate.Date);
                    if (earliestTransferAfterDate != null)
                    {
                        currentLopId = earliestTransferAfterDate.IdLopCu;
                    }
                }
                
                if (currentLopId > 0)
                {
                    int thu = i == 6 ? 8 : i + 2; // Sunday is 8, Mon is 2
                    lopHocTheoThu[thu] = currentLopId;
                }
            }

            var lopIds = lopHocTheoThu.Values.Distinct().ToList();

            if (!lopIds.Any())
            {
                ViewBag.TenLop = hocSinh.LopHoc?.TenLop ?? "Chưa xếp lớp";
                return View(new List<PhanCongGiangDay>());
            }

            var lopNames = _context.LopHocs.Where(x => lopIds.Contains(x.IdLop)).Select(x => x.TenLop).ToList();
            ViewBag.TenLop = string.Join(" / ", lopNames);

            var phongHocMap = _context.PhongHocs.Where(x => lopIds.Contains(x.IdLop.Value)).ToDictionary(x => x.IdLop.Value, x => x.MaPhong);
            ViewBag.PhongHocMap = phongHocMap;

            var query = _context.PhanCongGiangDays
                .Include(x => x.GiaoVien)
                .Include(x => x.MonHoc)
                .Include(x => x.LopHoc)
                .Include(x => x.PhongHoc)
                .AsNoTracking()
                .Where(x => lopIds.Contains(x.IdLop));

            if (!string.IsNullOrWhiteSpace(hocKy))
            {
                query = query.Where(x => x.HocKy == hocKy || x.HocKy == "Cả năm");
            }

            if (!string.IsNullOrWhiteSpace(namHoc))
            {
                query = query.Where(x => x.NamHoc == namHoc);
            }

            var rawResult = query.ToList();
            var finalResult = rawResult
                .Where(x => x.Thu.HasValue && lopHocTheoThu.ContainsKey(x.Thu.Value) && lopHocTheoThu[x.Thu.Value] == x.IdLop)
                .OrderBy(x => x.Thu)
                .ThenBy(x => x.TietBatDau)
                .ToList();

            return View(finalResult);
        }

        private void ValidateStudent(HocSinhViewModel vm)
        {
            vm.MaHS = vm.MaHS?.Trim() ?? string.Empty;
            vm.HoTen = vm.HoTen?.Trim() ?? string.Empty;
            vm.GioiTinh = vm.GioiTinh?.Trim();
            vm.SDT = vm.SDT?.Trim();
            vm.Email = vm.Email?.Trim();
            vm.DiaChi = vm.DiaChi?.Trim();

            if (_context.HocSinhs.Any(x => x.MaHS == vm.MaHS && x.IdHocSinh != vm.IdHocSinh))
                ModelState.AddModelError(nameof(vm.MaHS), "Mã học sinh đã tồn tại");

            if (vm.IdLopHoc.HasValue && !_context.LopHocs.Any(x => x.IdLop == vm.IdLopHoc.Value))
                ModelState.AddModelError(nameof(vm.IdLopHoc), "Lớp học không tồn tại");

            if (vm.IdTaiKhoan.HasValue)
            {
                var isStudentAccount = _context.TaiKhoans.Any(x =>
                    x.IdTaiKhoan == vm.IdTaiKhoan.Value &&
                    x.IdChucVu == 3 &&
                    x.TrangThai);

                if (!isStudentAccount)
                {
                    ModelState.AddModelError(nameof(vm.IdTaiKhoan),
                        "Tài khoản không tồn tại, đã khóa hoặc không thuộc quyền học sinh");
                }
                else if (_context.HocSinhs.Any(x =>
                    x.IdTaiKhoan == vm.IdTaiKhoan.Value &&
                    x.IdHocSinh != vm.IdHocSinh))
                {
                    ModelState.AddModelError(nameof(vm.IdTaiKhoan),
                        "Tài khoản này đã được gắn với một học sinh khác");
                }
            }

            if (vm.NgaySinh.Date > DateTime.Today)
                ModelState.AddModelError(nameof(vm.NgaySinh), "Ngày sinh không được lớn hơn ngày hiện tại");
        }

        private void ValidateImage(IFormFile? image)
        {
            if (image == null || image.Length == 0)
                return;

            var extension = Path.GetExtension(image.FileName);
            if (!AllowedImageExtensions.Contains(extension) ||
                !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("AnhTaiLen", "Chỉ chấp nhận ảnh JPG, PNG hoặc WebP");
            }

            if (image.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("AnhTaiLen", "Dung lượng ảnh tối đa là 5 MB");
        }

        private async Task<string?> SaveImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
                return null;

            var imageDirectory = Path.Combine(_environment.WebRootPath, "image");
            Directory.CreateDirectory(imageDirectory);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
            var fullPath = Path.Combine(imageDirectory, fileName);

            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await image.CopyToAsync(stream);
            return $"/image/{fileName}";
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) ||
                !imagePath.StartsWith("/image/", StringComparison.OrdinalIgnoreCase))
                return;

            var fileName = Path.GetFileName(imagePath);
            var fullPath = Path.Combine(_environment.WebRootPath, "image", fileName);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        private void SetFilterData(string? keyword, int? lopId, bool? trangThai)
        {
            ViewBag.Keyword = keyword;
            ViewBag.LopId = lopId;
            ViewBag.TrangThai = trangThai;
            ViewBag.LopHocs = _context.LopHocs.OrderBy(x => x.TenLop).ToList();
        }

        private List<SelectListItem> GetLopHocSelectList()
        {
            return _context.LopHocs
                .OrderBy(x => x.TenLop)
                .Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString()))
                .ToList();
        }

        private void SetFormLists(HocSinhViewModel vm, int? currentStudentId = null)
        {
            vm.LopHocs = GetLopHocSelectList();

            var availableAccounts = _context.TaiKhoans
                .Where(x => x.IdChucVu == 3 && x.TrangThai)
                .Where(x => !_context.HocSinhs.Any(h =>
                    h.IdTaiKhoan == x.IdTaiKhoan &&
                    (!currentStudentId.HasValue || h.IdHocSinh != currentStudentId.Value)))
                .OrderBy(x => x.Username)
                .Select(x => new SelectListItem(x.Username, x.IdTaiKhoan.ToString()))
                .ToList();

            vm.TaiKhoans = availableAccounts;
        }

        private int? GetCurrentHocSinhId()
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
                    if (hsp != null) return hsp.IdHocSinh;
                }
                return null;
            }

            var hocSinhId = _context.HocSinhs
                .Where(x => x.IdTaiKhoan == userId)
                .Select(x => (int?)x.IdHocSinh)
                .FirstOrDefault();

            if (hocSinhId.HasValue)
                return hocSinhId;

            if (string.IsNullOrWhiteSpace(username))
                return null;

            return _context.HocSinhs
                .Where(x => x.MaHS == username)
                .Select(x => (int?)x.IdHocSinh)
                .FirstOrDefault();
        }

        [RoleAuthorize(1)]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("HocSinh");

            // Headers
            worksheet.Cell(1, 1).Value = "Mã HS (*)";
            worksheet.Cell(1, 2).Value = "Họ Tên (*)";
            worksheet.Cell(1, 3).Value = "Ngày Sinh (dd/MM/yyyy) (*)";
            worksheet.Cell(1, 4).Value = "Giới Tính";
            worksheet.Cell(1, 5).Value = "SĐT";
            worksheet.Cell(1, 6).Value = "Email";
            worksheet.Cell(1, 7).Value = "Địa Chỉ";
            worksheet.Cell(1, 8).Value = "Tên Lớp";

            // Make headers bold
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Sample data
            worksheet.Cell(2, 1).Value = "HS001";
            worksheet.Cell(2, 2).Value = "Nguyễn Văn A";
            worksheet.Cell(2, 3).Value = "01/01/2010";
            worksheet.Cell(2, 4).Value = "Nam";
            worksheet.Cell(2, 5).Value = "0987654321";
            worksheet.Cell(2, 6).Value = "nguyenvana@example.com";
            worksheet.Cell(2, 7).Value = "Hà Nội";
            worksheet.Cell(2, 8).Value = "10A1";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "HocSinh_Template.xlsx");
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
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed()?.RowsUsed()?.Skip(1); // Skip header

                if (rows == null || !rows.Any())
                {
                    TempData["Error"] = "File Excel không có dữ liệu.";
                    return RedirectToAction(nameof(Index));
                }

                var lopHocs = _context.LopHocs.ToList();

                foreach (var row in rows)
                {
                    var maHS = row.Cell(1).GetString().Trim();
                    var hoTen = row.Cell(2).GetString().Trim();
                    
                    if (string.IsNullOrWhiteSpace(maHS) || string.IsNullOrWhiteSpace(hoTen))
                    {
                        skipCount++;
                        continue; // Bắt buộc phải có mã và họ tên
                    }

                    // Kiểm tra trùng lặp
                    if (_context.HocSinhs.Any(x => x.MaHS == maHS))
                    {
                        skipCount++;
                        continue;
                    }

                    DateTime ngaySinh = DateTime.Today;
                    var ngaySinhStr = row.Cell(3).GetString().Trim();
                    if (DateTime.TryParseExact(ngaySinhStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        ngaySinh = parsedDate;
                    }
                    else if (row.Cell(3).TryGetValue<DateTime>(out var cellDate))
                    {
                        ngaySinh = cellDate;
                    }

                    var tenLop = row.Cell(8).GetString().Trim();
                    int? idLopHoc = null;
                    if (!string.IsNullOrWhiteSpace(tenLop))
                    {
                        var lop = lopHocs.FirstOrDefault(l => l.TenLop.Equals(tenLop, StringComparison.OrdinalIgnoreCase));
                        if (lop != null)
                        {
                            idLopHoc = lop.IdLop;
                        }
                    }

                    var taiKhoan = _context.TaiKhoans.FirstOrDefault(t => t.Username == maHS);
                    if (taiKhoan == null)
                    {
                        taiKhoan = new TaiKhoan
                        {
                            Username = maHS,
                            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                            IdChucVu = 3,
                            TrangThai = true,
                            BatBuocDoiMatKhau = true
                        };
                        _context.TaiKhoans.Add(taiKhoan);
                        await _context.SaveChangesAsync();
                    }

                    var hs = new HocSinhViewModel
                    {
                        MaHS = maHS,
                        HoTen = hoTen,
                        NgaySinh = ngaySinh,
                        GioiTinh = row.Cell(4).GetString().Trim(),
                        SDT = row.Cell(5).GetString().Trim(),
                        Email = row.Cell(6).GetString().Trim(),
                        DiaChi = row.Cell(7).GetString().Trim(),
                        IdLopHoc = idLopHoc,
                        IdTaiKhoan = taiKhoan.IdTaiKhoan,
                        TrangThai = true
                    };

                    _service.Add(hs);
                    successCount++;
                }

                TempData["Success"] = $"Đã nhập thành công {successCount} học sinh. Bỏ qua {skipCount} dòng (lỗi hoặc trùng lặp).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi đọc file Excel: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

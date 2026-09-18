using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
namespace eSchool.Controllers
{
    [RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
    public class GiaoVienController : Controller
    {
        private static readonly HashSet<string> AllowedImageExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public GiaoVienController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult Index(string? keyword)
        {
            var query = _context.GiaoViens
                .Include(x => x.MonHoc)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.HoTen.Contains(keyword) ||
                    (x.Email != null && x.Email.Contains(keyword)) ||
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

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GiaoVienFormViewModel vm)
        {
            NormalizeGiaoVien(vm);
            ModelState.Remove(nameof(vm.MaGV));
            vm.MaGV = GenerateTeacherCode();
            ValidateGiaoVien(vm);
            ValidateImage(vm.AnhTaiLen);

            if (!string.IsNullOrWhiteSpace(vm.SDT) && _context.TaiKhoans.Any(x =>
                    x.Username == vm.SDT && x.IdChucVu == 2))
            {
                ModelState.AddModelError(nameof(vm.SDT), "Số điện thoại này đã được dùng cho một tài khoản giáo viên khác.");
            }

            if (!string.IsNullOrWhiteSpace(vm.Email) && _context.TaiKhoans.Any(x =>
                    x.Email == vm.Email && x.IdChucVu == 2))
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được dùng cho một tài khoản giáo viên khác.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin giáo viên chưa hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = new TaiKhoan
            {
                Username = vm.SDT!,
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                Email = vm.Email,
                IdChucVu = 2,
                TrangThai = true,
                BatBuocDoiMatKhau = true
            };

            string? imagePath = null;
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                imagePath = await SaveImageAsync(vm.AnhTaiLen);
                _context.GiaoViens.Add(new GiaoVien
                {
                    MaGV = vm.MaGV,
                    HoTen = vm.HoTen,
                    NgaySinh = vm.NgaySinh,
                    GioiTinh = vm.GioiTinh,
                    SDT = vm.SDT,
                    Email = vm.Email,
                    DiaChi = vm.DiaChi,
                    AnhDaiDien = imagePath,
                    IdMonHoc = vm.IdMonHoc,
                    TaiKhoan = taiKhoan
                });
                _context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                DeleteImage(imagePath);
                TempData["Error"] = "Không thể thêm giáo viên. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã thêm giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GiaoVienFormViewModel vm)
        {
            var giaoVien = _context.GiaoViens.Find(vm.IdGiaoVien);
            if (giaoVien == null) return NotFound();

            NormalizeGiaoVien(vm);
            ModelState.Remove(nameof(vm.MaGV));
            vm.MaGV = giaoVien.MaGV;
            ValidateGiaoVien(vm);
            ValidateImage(vm.AnhTaiLen);

            var taiKhoan = giaoVien.IdTaiKhoan.HasValue
                ? _context.TaiKhoans.Find(giaoVien.IdTaiKhoan.Value)
                : null;

            if (!string.IsNullOrWhiteSpace(vm.SDT) && _context.TaiKhoans.Any(x =>
                    x.Username == vm.SDT && x.IdChucVu == 2 &&
                    (taiKhoan == null || x.IdTaiKhoan != taiKhoan.IdTaiKhoan)))
            {
                ModelState.AddModelError(nameof(vm.SDT), "Số điện thoại này đã được dùng cho một tài khoản giáo viên khác.");
            }

            if (!string.IsNullOrWhiteSpace(vm.Email) && _context.TaiKhoans.Any(x =>
                    x.Email == vm.Email && x.IdChucVu == 2 &&
                    (taiKhoan == null || x.IdTaiKhoan != taiKhoan.IdTaiKhoan)))
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được dùng cho một tài khoản giáo viên khác.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin giáo viên chưa hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var oldImage = giaoVien.AnhDaiDien;
            string? newImage = null;
            try
            {
                if (vm.AnhTaiLen is { Length: > 0 })
                {
                    newImage = await SaveImageAsync(vm.AnhTaiLen);
                    giaoVien.AnhDaiDien = newImage;
                }

                giaoVien.HoTen = vm.HoTen;
                giaoVien.NgaySinh = vm.NgaySinh;
                giaoVien.GioiTinh = vm.GioiTinh;
                giaoVien.SDT = vm.SDT;
                giaoVien.Email = vm.Email;
                giaoVien.DiaChi = vm.DiaChi;
                giaoVien.IdMonHoc = vm.IdMonHoc;

                if (taiKhoan?.IdChucVu == 2)
                {
                    taiKhoan.Username = vm.SDT!;
                    taiKhoan.Email = vm.Email;
                }
                _context.SaveChanges();
            }
            catch
            {
                DeleteImage(newImage);
                TempData["Error"] = "Không thể cập nhật giáo viên. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }

            if (newImage != null && newImage != oldImage)
                DeleteImage(oldImage);

            TempData["Success"] = "Đã cập nhật giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
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

            var imagePath = giaoVien.AnhDaiDien;
            _context.GiaoViens.Remove(giaoVien);
            _context.SaveChanges();
            DeleteImage(imagePath);
            TempData["Success"] = "Đã xóa giáo viên.";
            return RedirectToAction(nameof(Index));
        }



        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult ChuNhiem()
        {
            ViewBag.EditableYears = _context.NamHocs.AsNoTracking().ToList()
                .Where(x => AcademicYearPolicy.CanModify(x, DateTime.Today))
                .Select(x => x.TenNamHoc).ToHashSet();
            return View(new ChuNhiemViewModel
            {
                LopHocs = _context.LopHocs
                    .Include(x => x.GiaoVienChuNhiem)
                    .OrderBy(x => x.TenLop)
                    .ToList(),
                GiaoViens = GetGiaoVienSelectList(),
                NamHocs = _context.NamHocs
                    .OrderByDescending(x => x.NgayBatDau)
                    .Select(x => new SelectListItem(x.TenNamHoc, x.TenNamHoc))
                    .ToList()
            });
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GanChuNhiem(int idLop, int? idGiaoVien)
        {
            var lop = _context.LopHocs.Find(idLop);
            if (lop == null) return NotFound();
            if (!AcademicYearPolicy.CanModify(_context.NamHocs.FirstOrDefault(x => x.TenNamHoc == lop.NamHoc), DateTime.Today))
            {
                TempData["Error"] = AcademicYearPolicy.ReadOnlyMessage;
                return RedirectToAction(nameof(ChuNhiem));
            }

            if (idGiaoVien.HasValue && !_context.GiaoViens.Any(x => x.IdGiaoVien == idGiaoVien.Value))
            {
                return NotFound();
            }

            if (idGiaoVien.HasValue && _context.LopHocs.Any(x =>
                    x.IdGiaoVienCN == idGiaoVien.Value &&
                    x.IdLop != lop.IdLop &&
                    x.NamHoc == lop.NamHoc))
            {
                TempData["Error"] = "Giáo viên này đã chủ nhiệm một lớp khác trong năm học đã chọn.";
                return RedirectToAction(nameof(ChuNhiem));
            }

            lop.IdGiaoVienCN = idGiaoVien;
            _context.SaveChanges();
            TempData["Success"] = idGiaoVien.HasValue
                ? "Đã cập nhật giáo viên chủ nhiệm."
                : "Đã bỏ phân công chủ nhiệm.";
            return RedirectToAction(nameof(ChuNhiem));
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TuDongGanChuNhiem(string? namHoc)
        {
            namHoc = namHoc?.Trim();
            if (string.IsNullOrWhiteSpace(namHoc))
            {
                TempData["Error"] = "Vui lòng chọn năm học cần phân công.";
                return RedirectToAction(nameof(ChuNhiem));
            }

            if (!AcademicYearPolicy.CanModify(_context.NamHocs.FirstOrDefault(x => x.TenNamHoc == namHoc), DateTime.Today))
            {
                TempData["Error"] = AcademicYearPolicy.ReadOnlyMessage;
                return RedirectToAction(nameof(ChuNhiem));
            }

            var lopChuaPhanCong = _context.LopHocs
                .Where(x => x.NamHoc == namHoc && !x.IdGiaoVienCN.HasValue)
                .OrderBy(x => x.Khoi)
                .ThenBy(x => x.TenLop)
                .ToList();

            if (!lopChuaPhanCong.Any())
            {
                TempData["Success"] = "Tất cả lớp trong năm học này đã có giáo viên chủ nhiệm.";
                return RedirectToAction(nameof(ChuNhiem));
            }

            var teacherIds = _context.GiaoViens
                .AsNoTracking()
                .OrderBy(x => x.HoTen)
                .Select(x => x.IdGiaoVien)
                .ToList();

            if (!teacherIds.Any())
            {
                TempData["Error"] = "Chưa có giáo viên để thực hiện phân công tự động.";
                return RedirectToAction(nameof(ChuNhiem));
            }

            var homeroomLoads = _context.LopHocs
                .Where(x => x.NamHoc == namHoc && x.IdGiaoVienCN.HasValue)
                .GroupBy(x => x.IdGiaoVienCN!.Value)
                .ToDictionary(x => x.Key, x => x.Count());
            var previousNamHoc = GetPreviousAcademicYearName(namHoc);

            var autoAssignedCount = 0;
            foreach (var lop in lopChuaPhanCong)
            {
                lop.IdGiaoVienCN = SelectAutomaticHomeroomTeacher(
                    lop,
                    previousNamHoc,
                    teacherIds,
                    homeroomLoads);
                if (lop.IdGiaoVienCN.HasValue)
                    autoAssignedCount++;
            }

            _context.SaveChanges();
            TempData["Success"] = autoAssignedCount == lopChuaPhanCong.Count
                ? $"Đã tự động phân công giáo viên chủ nhiệm cho {autoAssignedCount} lớp trong năm học {namHoc}."
                : $"Đã tự động phân công giáo viên chủ nhiệm cho {autoAssignedCount}/{lopChuaPhanCong.Count} lớp trong năm học {namHoc}. Các lớp còn lại chưa được phân công vì không còn giáo viên trống trong năm học này.";
            return RedirectToAction(nameof(ChuNhiem));
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin, 2)]
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

            var selectedDate = tuan ?? DateTime.Today;
            var diff = (7 + (selectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = selectedDate.AddDays(-diff);
            var endDate = startOfWeek.AddDays(6);

            var currentNamHoc = _context.NamHocs.FirstOrDefault(n => selectedDate.Date >= n.NgayBatDau.Date && selectedDate.Date <= n.NgayKetThuc.Date);
            if (currentNamHoc == null)
            {
                ViewBag.GiaoVienId = giaoVienId;
                ViewBag.HocKy = hocKy;
                ViewBag.NamHoc = namHoc;
                ViewBag.Tuan = selectedDate.ToString("yyyy-MM-dd");
                ViewBag.StartOfWeek = startOfWeek.ToString("yyyy-MM-dd");
                ViewBag.ThayDois = new List<LichHocThayDoi>();
                ViewBag.PhongHocMap = new Dictionary<int, string>();
                if (IsTeacher() && giaoVienId.HasValue)
                {
                    var currentTeacher = _context.GiaoViens.Find(giaoVienId.Value);
                    ViewBag.GiaoViens = currentTeacher != null 
                        ? new List<SelectListItem> { new SelectListItem($"{currentTeacher.MaGV} - {currentTeacher.HoTen}", currentTeacher.IdGiaoVien.ToString()) }
                        : new List<SelectListItem>();
                }
                else
                {
                    ViewBag.GiaoViens = GetGiaoVienSelectList();
                }
                return View(new List<PhanCongGiangDay>());
            }

            namHoc = currentNamHoc.TenNamHoc;

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
            
            ViewBag.Tuan = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.StartOfWeek = startOfWeek.ToString("yyyy-MM-dd");
            
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

            var student = _context.HocSinhs.Include(x => x.LopHoc).FirstOrDefault(x => x.IdHocSinh == model.IdHocSinh);
            if (student?.LopHoc?.IdGiaoVienCN != giaoVienId.Value)
                return NotFound();
            var year = _context.NamHocs.FirstOrDefault(x => x.TenNamHoc == student.LopHoc.NamHoc);
            if (!AcademicYearPolicy.CanModify(year, DateTime.Today) || model.NgayViPham.Date > DateTime.Today ||
                model.NgayViPham.Date < year!.NgayBatDau.Date || model.NgayViPham.Date > year.NgayKetThuc.Date)
            {
                TempData["Error"] = "Ngày vi phạm phải thuộc năm học còn mở và không nằm trong tương lai.";
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
            var teacherId = GetCurrentGiaoVienId();
            var kyLuat = _context.KyLuats.Include(x => x.HocSinh).ThenInclude(x => x!.LopHoc)
                .FirstOrDefault(x => x.IdKyLuat == id && x.IdGiaoVien == teacherId);
            if (!teacherId.HasValue || kyLuat?.HocSinh?.LopHoc?.IdGiaoVienCN != teacherId)
                return NotFound();
            var year = _context.NamHocs.FirstOrDefault(x => x.TenNamHoc == kyLuat.HocSinh.LopHoc.NamHoc);
            if (!AcademicYearPolicy.CanModify(year, DateTime.Today))
            {
                TempData["Error"] = AcademicYearPolicy.ReadOnlyMessage;
                return RedirectToAction(nameof(QuanLyKyLuat));
            }
            if (kyLuat != null)
            {
                _context.KyLuats.Remove(kyLuat);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa kỷ luật.";
            }
            return RedirectToAction(nameof(QuanLyKyLuat));
        }

        [RoleAuthorize(2)]
        public IActionResult QuanLyHocPhi(int? namHocId, int? hocKyId, int? lopId)
        {
            var giaoVienId = GetCurrentGiaoVienId();
            if (!giaoVienId.HasValue) return NotFound("Tài khoản này chưa được liên kết với hồ sơ giáo viên.");

            var dsLopCN = _context.LopHocs.Where(x => x.IdGiaoVienCN == giaoVienId.Value).ToList();
            if (!dsLopCN.Any())
            {
                TempData["Error"] = "Bạn chưa được phân công chủ nhiệm lớp nào.";
                return View(new HocPhiPageViewModel()); // Return empty view model
            }

            var lopHocIds = dsLopCN.Select(x => x.IdLop).ToList();

            var query = _context.HocPhis
                .Include(x => x.HocSinh).ThenInclude(x => x!.LopHoc)
                .Include(x => x.NamHoc)
                .Include(x => x.HocKyInfo)
                .Where(x => x.HocSinh != null && x.HocSinh.IdLopHoc.HasValue && lopHocIds.Contains(x.HocSinh.IdLopHoc.Value))
                .AsQueryable();

            if (namHocId.HasValue)
            {
                query = query.Where(x => x.IdNamHoc == namHocId.Value);
            }
            if (hocKyId.HasValue)
            {
                query = query.Where(x => x.IdHocKy == hocKyId.Value);
            }
            if (lopId.HasValue)
            {
                query = query.Where(x => x.HocSinh != null && x.HocSinh.IdLopHoc == lopId.Value);
            }

            var vm = new HocPhiPageViewModel
            {
                DanhSach = query.OrderByDescending(x => x.HanDongTien).ToList(),
                NamHocs = _context.NamHocs.OrderByDescending(x => x.TenNamHoc)
                    .Select(x => new SelectListItem { Text = x.TenNamHoc, Value = x.IdNamHoc.ToString(), Selected = x.IdNamHoc == namHocId })
                    .ToList(),
                HocKys = _context.HocKys.OrderBy(x => x.TenHocKy)
                    .Select(x => new SelectListItem { Text = x.TenHocKy, Value = x.IdHocKy.ToString(), Selected = x.IdHocKy == hocKyId })
                    .ToList()
            };

            ViewBag.LopHocs = dsLopCN.OrderBy(x => x.TenLop).Select(x => new SelectListItem { Text = x.TenLop, Value = x.IdLop.ToString(), Selected = x.IdLop == lopId }).ToList();
            ViewBag.NamHocId = namHocId;
            ViewBag.HocKyId = hocKyId;
            ViewBag.LopId = lopId;
            return View(vm);
        }

        [RoleAuthorize(2)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatTrangThaiHocPhi(int IdHocPhi, int TrangThai, int? namHocId, int? hocKyId, int? lopId)
        {
            var giaoVienId = GetCurrentGiaoVienId();
            if (!giaoVienId.HasValue) return NotFound();

            var hocPhi = _context.HocPhis.Include(x => x.HocSinh).FirstOrDefault(x => x.IdHocPhi == IdHocPhi);
            if (hocPhi == null || hocPhi.HocSinh == null || !hocPhi.HocSinh.IdLopHoc.HasValue)
            {
                TempData["Error"] = "Khoản học phí không tồn tại hoặc dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(QuanLyHocPhi), new { namHocId, hocKyId, lopId });
            }

            // Check if teacher is homeroom teacher of this class
            var isHomeroomTeacher = _context.LopHocs.Any(x => x.IdLop == hocPhi.HocSinh.IdLopHoc.Value && x.IdGiaoVienCN == giaoVienId.Value);
            if (!isHomeroomTeacher)
            {
                TempData["Error"] = "Bạn không có quyền cập nhật học phí của học sinh này.";
                return RedirectToAction(nameof(QuanLyHocPhi), new { namHocId, hocKyId, lopId });
            }

            if (TrangThai is not (0 or 1) || (hocPhi.TrangThai == 1 && TrangThai != 1))
            {
                TempData["Error"] = "Không thể hủy khoản đã xác nhận thanh toán. Vui lòng liên hệ quản trị để đối soát.";
                return RedirectToAction(nameof(QuanLyHocPhi), new { namHocId, hocKyId, lopId });
            }
            if (hocPhi.TrangThai == 1)
                return RedirectToAction(nameof(QuanLyHocPhi), new { namHocId, hocKyId, lopId });
            // Update to selected status (0 = Chưa đóng, 1 = Đã đóng)
            hocPhi.TrangThai = TrangThai;
            if (TrangThai == 1) // Đã đóng
            {
                hocPhi.NgayDong = DateTime.Now;
            }
            else
            {
                hocPhi.NgayDong = null;
            }

            _context.SaveChanges();
            TempData["Success"] = "Cập nhật trạng thái học phí thành công.";
            return RedirectToAction(nameof(QuanLyHocPhi), new { namHocId, hocKyId, lopId });
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

        private string GenerateTeacherCode(ISet<string>? reservedCodes = null)
        {
            var existingCodes = reservedCodes ?? _context.GiaoViens
                .AsNoTracking()
                .Select(x => x.MaGV)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sequence = 1;
            string code;
            do
            {
                code = $"GV{sequence:D5}";
                sequence++;
            } while (existingCodes.Contains(code));

            return code;
        }

        private void ValidateImage(IFormFile? image)
        {
            if (image == null || image.Length == 0)
                return;

            var extension = Path.GetExtension(image.FileName);
            if (!AllowedImageExtensions.Contains(extension) ||
                !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(GiaoVienFormViewModel.AnhTaiLen),
                    "Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.");
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(GiaoVienFormViewModel.AnhTaiLen),
                    "Dung lượng ảnh tối đa là 5 MB.");
            }
        }

        private async Task<string?> SaveImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
                return null;

            var imageDirectory = Path.Combine(_environment.WebRootPath, "image", "teachers");
            Directory.CreateDirectory(imageDirectory);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
            var fullPath = Path.Combine(imageDirectory, fileName);
            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await image.CopyToAsync(stream);
            return $"/image/teachers/{fileName}";
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) ||
                !imagePath.StartsWith("/image/teachers/", StringComparison.OrdinalIgnoreCase))
                return;

            var fileName = Path.GetFileName(imagePath);
            var fullPath = Path.Combine(_environment.WebRootPath, "image", "teachers", fileName);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
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

        private string? GetPreviousAcademicYearName(string targetNamHoc)
        {
            var targetYear = _context.NamHocs
                .AsNoTracking()
                .FirstOrDefault(x => x.TenNamHoc == targetNamHoc);

            if (targetYear == null)
                return null;

            return _context.NamHocs
                .AsNoTracking()
                .Where(x => x.NgayKetThuc < targetYear.NgayBatDau)
                .OrderByDescending(x => x.NgayKetThuc)
                .Select(x => x.TenNamHoc)
                .FirstOrDefault();
        }

        private int? SelectAutomaticHomeroomTeacher(
            LopHoc lop,
            string? previousNamHoc,
            IReadOnlyList<int> teacherIds,
            IDictionary<int, int> homeroomLoads)
        {
            int? preferredTeacherId = null;
            var predecessorClassName = GetPredecessorClassName(lop.TenLop);

            if (!string.IsNullOrWhiteSpace(previousNamHoc) && predecessorClassName != null)
            {
                preferredTeacherId = _context.LopHocs
                    .AsNoTracking()
                    .Where(x => x.NamHoc == previousNamHoc &&
                                x.TenLop == predecessorClassName &&
                                x.IdGiaoVienCN.HasValue)
                    .Select(x => x.IdGiaoVienCN)
                    .FirstOrDefault();
            }

            var availableTeacherIds = teacherIds
                .Where(id => !homeroomLoads.ContainsKey(id))
                .ToList();
            if (!availableTeacherIds.Any())
                return null;

            var selectedTeacherId = preferredTeacherId.HasValue &&
                                    availableTeacherIds.Contains(preferredTeacherId.Value)
                ? preferredTeacherId.Value
                : availableTeacherIds.First();

            if (selectedTeacherId <= 0)
                return null;

            homeroomLoads[selectedTeacherId] = homeroomLoads.TryGetValue(selectedTeacherId, out var currentLoad)
                ? currentLoad + 1
                : 1;

            return selectedTeacherId;
        }

        private static string? GetPredecessorClassName(string? className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(className.Trim(), @"^(?<grade>[6-9])(?<suffix>.+)$");
            if (!match.Success || !int.TryParse(match.Groups["grade"].Value, out var grade) || grade <= 6)
                return null;

            return $"{grade - 1}{match.Groups["suffix"].Value}";
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("GiaoVien");

            // Headers
            worksheet.Cell(1, 1).Value = "Họ Tên (*)";
            worksheet.Cell(1, 2).Value = "Ngày Sinh (dd/MM/yyyy) (*)";
            worksheet.Cell(1, 3).Value = "Giới Tính";
            worksheet.Cell(1, 4).Value = "SĐT - 10 số (*)";
            worksheet.Cell(1, 5).Value = "Email (*)";
            worksheet.Cell(1, 6).Value = "Địa Chỉ";
            worksheet.Cell(1, 7).Value = "Mã Môn";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Cell(2, 1).Value = "Trần Văn B";
            worksheet.Cell(2, 2).Value = "01/01/1980";
            worksheet.Cell(2, 3).Value = "Nam";
            worksheet.Cell(2, 4).Value = "0912345678";
            worksheet.Cell(2, 5).Value = "tranvanb@example.com";
            worksheet.Cell(2, 6).Value = "Hà Nội";
            worksheet.Cell(2, 7).Value = "TOAN";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "GiaoVien_Template.xlsx");
        }

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
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
                var usedTeacherCodes = _context.GiaoViens
                    .AsNoTracking()
                    .Select(x => x.MaGV)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var usedPhones = _context.TaiKhoans
                    .Where(x => x.IdChucVu == 2)
                    .Select(x => x.Username)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var usedEmails = _context.TaiKhoans
                    .Where(x => x.IdChucVu == 2 && x.Email != null)
                    .Select(x => x.Email!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

                var firstHeader = worksheet.Cell(1, 1).GetString();
                var isLegacyTemplate = firstHeader.Contains("Mã", StringComparison.OrdinalIgnoreCase) ||
                                       firstHeader.Contains("Ma", StringComparison.OrdinalIgnoreCase);
                var columnOffset = isLegacyTemplate ? 1 : 0;

                foreach (var row in rows)
                {
                    var worksheetRow = row.WorksheetRow();
                    var maGV = GenerateTeacherCode(usedTeacherCodes);
                    var hoTen = worksheetRow.Cell(1 + columnOffset).GetString().Trim();
                    
                    if (string.IsNullOrWhiteSpace(hoTen))
                    {
                        skipCount++;
                        continue;
                    }

                    var sdt = worksheetRow.Cell(4 + columnOffset).GetString().Trim();
                    var email = worksheetRow.Cell(5 + columnOffset).GetString().Trim();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^0[0-9]{9}$") ||
                        string.IsNullOrWhiteSpace(email) ||
                        !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) ||
                        usedPhones.Contains(sdt) ||
                        usedEmails.Contains(email))
                    {
                        skipCount++;
                        continue;
                    }

                    DateTime ngaySinh = DateTime.Today;
                    var ngaySinhStr = worksheetRow.Cell(2 + columnOffset).GetString().Trim();
                    if (DateTime.TryParseExact(ngaySinhStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        ngaySinh = parsedDate;
                    }
                    else if (worksheetRow.Cell(2 + columnOffset).TryGetValue<DateTime>(out var cellDate))
                    {
                        ngaySinh = cellDate;
                    }

                    if (ngaySinh.Date > DateTime.Today)
                    {
                        skipCount++;
                        continue;
                    }

                    var maMon = worksheetRow.Cell(7 + columnOffset).GetString().Trim();
                    int? idMonHoc = null;
                    if (!string.IsNullOrWhiteSpace(maMon) && dictMonHoc.TryGetValue(maMon, out var foundId))
                    {
                        idMonHoc = foundId;
                    }

                    var taiKhoan = new TaiKhoan
                    {
                        Username = sdt,
                        Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                        Email = email,
                        IdChucVu = 2,
                        TrangThai = true,
                        BatBuocDoiMatKhau = true
                    };
                    _context.TaiKhoans.Add(taiKhoan);
                    await _context.SaveChangesAsync();

                    var gv = new GiaoVien
                    {
                        MaGV = maGV,
                        HoTen = hoTen,
                        NgaySinh = ngaySinh,
                        GioiTinh = worksheetRow.Cell(3 + columnOffset).GetString().Trim(),
                        SDT = sdt,
                        Email = email,
                        DiaChi = worksheetRow.Cell(6 + columnOffset).GetString().Trim(),
                        IdTaiKhoan = taiKhoan.IdTaiKhoan,
                        IdMonHoc = idMonHoc
                    };

                    _context.GiaoViens.Add(gv);
                    usedTeacherCodes.Add(maGV);
                    usedPhones.Add(sdt);
                    usedEmails.Add(email);
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

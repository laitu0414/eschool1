using eSchool.Infrastructure;
using eSchool.Services;
using eSchool.ViewModels;
using eSchool.Models;
using Microsoft.AspNetCore.Mvc;

namespace eSchool.Controllers
{
    [RoleAuthorize(3, 4)]
    public class PhuHuynhController : Controller
    {
        private readonly IPhuHuynhService _service;
        private readonly AppDbContext _context;

        public PhuHuynhController(IPhuHuynhService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        private HocSinh? GetCurrentHocSinh()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (userId == null) return null;

            if (roleId == 4)
            {
                var ph = _context.PhuHuynhs.FirstOrDefault(x => x.IdTaiKhoan == userId);
                if (ph != null)
                {
                    var hsp = _context.HocSinhPhuHuynhs.FirstOrDefault(x => x.IdPhuHuynh == ph.IdPhuHuynh);
                    if (hsp != null)
                        return _context.HocSinhs.FirstOrDefault(x => x.IdHocSinh == hsp.IdHocSinh);
                }
                return null;
            }

            return _context.HocSinhs.FirstOrDefault(x => x.IdTaiKhoan == userId);
        }

        public IActionResult Index(string? keyword)
        {
            ViewBag.Keyword = keyword;
            var hs = GetCurrentHocSinh();
            if (hs == null) return View(_service.GetAll(keyword));

            var phIds = _context.HocSinhPhuHuynhs.Where(x => x.IdHocSinh == hs.IdHocSinh).Select(x => x.IdPhuHuynh).ToList();
            var phuHuynhs = _service.GetAll(keyword).Where(x => phIds.Contains(x.IdPhuHuynh)).ToList();
            
            ViewBag.HasParent = phuHuynhs.Any();
            return View(phuHuynhs);
        }

        public IActionResult Create()
        {
            var hs = GetCurrentHocSinh();
            if (hs != null)
            {
                var hasParent = _context.HocSinhPhuHuynhs.Any(x => x.IdHocSinh == hs.IdHocSinh);
                if (hasParent)
                {
                    TempData["Error"] = "Mỗi học sinh chỉ được thêm 1 phụ huynh làm người giám hộ chính.";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(new PhuHuynhViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PhuHuynhViewModel vm)
        {
            Normalize(vm);

            var hs = GetCurrentHocSinh();
            if (hs != null)
            {
                var hasParent = _context.HocSinhPhuHuynhs.Any(x => x.IdHocSinh == hs.IdHocSinh);
                if (hasParent)
                {
                    TempData["Error"] = "Mỗi học sinh chỉ được thêm 1 phụ huynh làm người giám hộ chính.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (!ModelState.IsValid)
                return View(vm);

            var ph = new PhuHuynh
            {
                HoTen = vm.HoTen,
                SDT = vm.SDT,
                Email = vm.Email,
                DiaChi = vm.DiaChi,
                NgheNghiep = vm.NgheNghiep,
                TrangThai = vm.TrangThai
            };
            if (!string.IsNullOrWhiteSpace(vm.SDT) && !_context.TaiKhoans.Any(x => x.Username == vm.SDT))
            {
                var taiKhoan = new TaiKhoan
                {
                    Username = vm.SDT,
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    IdChucVu = 4,
                    TrangThai = true,
                    BatBuocDoiMatKhau = true
                };
                _context.TaiKhoans.Add(taiKhoan);
                _context.SaveChanges();
                ph.IdTaiKhoan = taiKhoan.IdTaiKhoan;
            }

            _context.PhuHuynhs.Add(ph);
            _context.SaveChanges();

            if (hs != null)
            {
                var hsp = new HocSinhPhuHuynh
                {
                    IdHocSinh = hs.IdHocSinh,
                    IdPhuHuynh = ph.IdPhuHuynh,
                    QuanHe = "Giám hộ chính",
                    LaLienHeChinh = true
                };
                _context.HocSinhPhuHuynhs.Add(hsp);
                _context.SaveChanges();
            }

            TempData["Success"] = "Thêm phụ huynh thành công";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var hs = GetCurrentHocSinh();
            if (hs != null)
            {
                var isMyParent = _context.HocSinhPhuHuynhs.Any(x => x.IdHocSinh == hs.IdHocSinh && x.IdPhuHuynh == id);
                if (!isMyParent) return NotFound();
            }

            var data = _service.GetById(id);
            return data == null ? NotFound() : View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(PhuHuynhViewModel vm)
        {
            var hs = GetCurrentHocSinh();
            if (hs != null)
            {
                var isMyParent = _context.HocSinhPhuHuynhs.Any(x => x.IdHocSinh == hs.IdHocSinh && x.IdPhuHuynh == vm.IdPhuHuynh);
                if (!isMyParent) return NotFound();
            }

            Normalize(vm);

            if (!ModelState.IsValid)
                return View(vm);

            _service.Update(vm);
            TempData["Success"] = "Cập nhật phụ huynh thành công";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var hs = GetCurrentHocSinh();
            if (hs != null)
            {
                var isMyParent = _context.HocSinhPhuHuynhs.Any(x => x.IdHocSinh == hs.IdHocSinh && x.IdPhuHuynh == id);
                if (!isMyParent) return NotFound();
            }

            _service.Delete(id);
            TempData["Success"] = "Đã chuyển phụ huynh sang trạng thái ngừng hoạt động";
            return RedirectToAction(nameof(Index));
        }

        private static void Normalize(PhuHuynhViewModel vm)
        {
            vm.HoTen = vm.HoTen?.Trim() ?? string.Empty;
            vm.SDT = vm.SDT?.Trim();
            vm.Email = vm.Email?.Trim();
            vm.DiaChi = vm.DiaChi?.Trim();
            vm.NgheNghiep = vm.NgheNghiep?.Trim();
        }
    }
}

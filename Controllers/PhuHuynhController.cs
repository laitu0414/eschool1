using eSchool.Infrastructure;
using eSchool.Services;
using eSchool.ViewModels;
using eSchool.Models;
using Microsoft.AspNetCore.Mvc;

namespace eSchool.Controllers
{
    [RoleAuthorize(SystemRoleIds.SystemAdmin, 3, 4)]
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
            if (IsAdministrator())
            {
                var vm = new PhuHuynhViewModel();
                SetHocSinhOptions(vm);
                return View(vm);
            }

            var hs = GetCurrentHocSinh();
            if (hs == null)
                return NotFound("Tài khoản này chưa được liên kết với học sinh.");

            var hasParent = _context.HocSinhPhuHuynhs.Any(x => x.IdHocSinh == hs.IdHocSinh);
            if (hasParent)
            {
                TempData["Error"] = "Mỗi học sinh chỉ được thêm 1 phụ huynh làm người giám hộ chính.";
                return RedirectToAction(nameof(Index));
            }

            return View(new PhuHuynhViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PhuHuynhViewModel vm)
        {
            Normalize(vm);

            HocSinh? hs;
            if (IsAdministrator())
            {
                if (!vm.IdHocSinh.HasValue)
                {
                    hs = null;
                    ModelState.AddModelError(nameof(vm.IdHocSinh), "Vui lòng chọn học sinh để liên kết.");
                }
                else
                {
                    hs = _context.HocSinhs.FirstOrDefault(x => x.IdHocSinh == vm.IdHocSinh.Value && x.TrangThai);
                    if (hs == null)
                        ModelState.AddModelError(nameof(vm.IdHocSinh), "Học sinh được chọn không tồn tại hoặc đã nghỉ học.");
                }
            }
            else
            {
                hs = GetCurrentHocSinh();
                if (hs == null)
                    return NotFound("Tài khoản này chưa được liên kết với học sinh.");

                vm.IdHocSinh = hs.IdHocSinh;
                ModelState.Remove(nameof(vm.IdHocSinh));
            }

            if (hs != null && _context.HocSinhPhuHuynhs.Any(x => x.IdHocSinh == hs.IdHocSinh))
            {
                ModelState.AddModelError(nameof(vm.IdHocSinh), "Học sinh này đã có phụ huynh là người giám hộ chính.");
            }

            if (!ModelState.IsValid)
            {
                if (IsAdministrator())
                    SetHocSinhOptions(vm);
                return View(vm);
            }

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
                ph.TaiKhoan = new TaiKhoan
                {
                    Username = vm.SDT,
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    IdChucVu = 4,
                    TrangThai = true,
                    BatBuocDoiMatKhau = true
                };
            }

            _context.PhuHuynhs.Add(ph);
            _context.HocSinhPhuHuynhs.Add(new HocSinhPhuHuynh
            {
                IdHocSinh = hs!.IdHocSinh,
                PhuHuynh = ph,
                QuanHe = "Giám hộ chính",
                LaLienHeChinh = true
            });
            _context.SaveChanges();

            TempData["Success"] = "Thêm phụ huynh và liên kết với học sinh thành công.";
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

        [RoleAuthorize(SystemRoleIds.SystemAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!_context.PhuHuynhs.Any(x => x.IdPhuHuynh == id))
                return NotFound();

            _service.Delete(id);
            TempData["Success"] = "Đã xóa phụ huynh, tài khoản liên quan và liên kết với học sinh.";
            return RedirectToAction(nameof(Index));
        }

        private bool IsAdministrator()
        {
            return HttpContext.Session.GetInt32("RoleId") == SystemRoleIds.SystemAdmin;
        }

        private void SetHocSinhOptions(PhuHuynhViewModel vm)
        {
            vm.HocSinhs = _context.HocSinhs
                .Where(hs => hs.TrangThai && !_context.HocSinhPhuHuynhs
                    .Any(link => link.IdHocSinh == hs.IdHocSinh))
                .OrderByDescending(hs => hs.IdHocSinh)
                .Select(hs => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = hs.IdHocSinh.ToString(),
                    Text = $"{hs.MaHS} - {hs.HoTen}",
                    Selected = vm.IdHocSinh == hs.IdHocSinh
                })
                .ToList();
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

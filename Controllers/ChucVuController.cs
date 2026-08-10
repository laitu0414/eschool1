using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.Services;
using Microsoft.AspNetCore.Mvc;

namespace eSchool.Controllers
{
    [AdminOnly]
    public class ChucVuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INhatKyService _nhatKyService;

        public ChucVuController(AppDbContext context, INhatKyService nhatKyService)
        {
            _context = context;
            _nhatKyService = nhatKyService;
        }

        public IActionResult Index()
        {
            return View(_context.ChucVus.OrderBy(x => x.IdChucVu).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string tenChucVu)
        {
            tenChucVu = tenChucVu?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tenChucVu) || tenChucVu.Length > 50)
            {
                TempData["Error"] = "Tên chức vụ phải từ 1 đến 50 ký tự";
                return RedirectToAction("Index");
            }

            if (_context.ChucVus.Any(x => x.TenChucVu == tenChucVu))
            {
                TempData["Error"] = "Chức vụ đã tồn tại";
                return RedirectToAction("Index");
            }

            _context.ChucVus.Add(new ChucVu { TenChucVu = tenChucVu });
            _context.SaveChanges();
            WriteLog("Thêm chức vụ", $"Đã thêm chức vụ {tenChucVu}");
            TempData["Success"] = "Thêm chức vụ thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string tenChucVu)
        {
            tenChucVu = tenChucVu?.Trim() ?? string.Empty;
            var chucVu = _context.ChucVus.Find(id);

            if (chucVu == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(tenChucVu) ||
                tenChucVu.Length > 50 ||
                _context.ChucVus.Any(x => x.IdChucVu != id && x.TenChucVu == tenChucVu))
            {
                TempData["Error"] = "Tên chức vụ không hợp lệ hoặc đã tồn tại";
                return RedirectToAction("Index");
            }

            chucVu.TenChucVu = tenChucVu;
            _context.SaveChanges();
            WriteLog("Sửa chức vụ", $"Đã sửa chức vụ ID {id} thành {tenChucVu}");
            TempData["Success"] = "Cập nhật chức vụ thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var chucVu = _context.ChucVus.Find(id);
            if (chucVu == null)
                return NotFound();

            if (id is >= 1 and <= 4)
            {
                TempData["Error"] = "Không thể xóa các chức vụ hệ thống mặc định";
                return RedirectToAction("Index");
            }

            if (_context.TaiKhoans.Any(x => x.IdChucVu == id))
            {
                TempData["Error"] = "Không thể xóa chức vụ đang được gán cho tài khoản";
                return RedirectToAction("Index");
            }

            _context.ChucVus.Remove(chucVu);
            _context.SaveChanges();
            WriteLog("Xóa chức vụ", $"Đã xóa chức vụ {chucVu.TenChucVu}");
            TempData["Success"] = "Xóa chức vụ thành công";
            return RedirectToAction("Index");
        }

        private void WriteLog(string action, string content)
        {
            _nhatKyService.GhiLog(
                HttpContext.Session.GetString("Username") ?? "Admin",
                action,
                content);
        }
    }
}

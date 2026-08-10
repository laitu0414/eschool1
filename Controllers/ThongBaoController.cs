using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Controllers
{
    [AdminOnly]
    public class ThongBaoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INhatKyService _nhatKyService;

        public ThongBaoController(AppDbContext context, INhatKyService nhatKyService)
        {
            _context = context;
            _nhatKyService = nhatKyService;
        }

        public IActionResult Index()
        {
            var data = _context.ThongBaos
                .Include(x => x.TaiKhoan)
                .OrderByDescending(x => x.NgayTao)
                .ToList();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string tieuDe, string noiDung, int doiTuongNhan)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            if (!IsValid(tieuDe, noiDung) || !IsValidDoiTuongNhan(doiTuongNhan))
            {
                TempData["Error"] = "Tiêu đề phải từ 1 đến 200 ký tự, nội dung không được để trống và đối tượng nhận phải hợp lệ";
                return RedirectToAction("Index");
            }

            var thongBao = new ThongBao
            {
                TieuDe = tieuDe.Trim(),
                NoiDung = noiDung.Trim(),
                DoiTuongNhan = doiTuongNhan,
                NgayTao = DateTime.Now,
                IdTaiKhoan = userId
            };

            _context.ThongBaos.Add(thongBao);
            _context.SaveChanges();
            WriteLog("Thêm thông báo", $"Đã thêm thông báo {thongBao.TieuDe}");
            TempData["Success"] = "Thêm thông báo thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string tieuDe, string noiDung, int doiTuongNhan)
        {
            var thongBao = _context.ThongBaos.Find(id);
            if (thongBao == null)
                return NotFound();

            if (!IsValid(tieuDe, noiDung) || !IsValidDoiTuongNhan(doiTuongNhan))
            {
                TempData["Error"] = "Tiêu đề phải từ 1 đến 200 ký tự, nội dung không được để trống và đối tượng nhận phải hợp lệ";
                return RedirectToAction("Index");
            }

            thongBao.TieuDe = tieuDe.Trim();
            thongBao.NoiDung = noiDung.Trim();
            thongBao.DoiTuongNhan = doiTuongNhan;
            _context.SaveChanges();
            WriteLog("Sửa thông báo", $"Đã sửa thông báo ID {id}");
            TempData["Success"] = "Cập nhật thông báo thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var thongBao = _context.ThongBaos.Find(id);
            if (thongBao == null)
                return NotFound();

            _context.ThongBaos.Remove(thongBao);
            _context.SaveChanges();
            WriteLog("Xóa thông báo", $"Đã xóa thông báo {thongBao.TieuDe}");
            TempData["Success"] = "Xóa thông báo thành công";
            return RedirectToAction("Index");
        }

        private static bool IsValid(string tieuDe, string noiDung)
        {
            return !string.IsNullOrWhiteSpace(tieuDe) &&
                   tieuDe.Trim().Length <= 200 &&
                   !string.IsNullOrWhiteSpace(noiDung);
        }

        private static bool IsValidDoiTuongNhan(int doiTuongNhan)
        {
            return doiTuongNhan is >= 0 and <= 2;
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

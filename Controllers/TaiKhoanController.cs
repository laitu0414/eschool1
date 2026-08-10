using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Controllers
{
    [AdminOnly]
    public class TaiKhoanController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IChucVuService _chucVuService;
        private readonly INhatKyService _nhatKyService;
        private readonly AppDbContext _context;

        public TaiKhoanController(
            IAccountService accountService,
            IChucVuService chucVuService,
            INhatKyService nhatKyService,
            AppDbContext context)
        {
            _accountService = accountService;
            _chucVuService = chucVuService;
            _nhatKyService = nhatKyService;
            _context = context;
        }

        public IActionResult Index(string? keyword, int? idChucVu, bool? trangThai)
        {
            SetPageData(keyword, idChucVu, trangThai);
            return View(_accountService.Search(keyword, idChucVu, trangThai));
        }

        private void SetPageData(string? keyword, int? idChucVu, bool? trangThai)
        {
            ViewBag.ChucVus = _chucVuService.GetAll();
            ViewBag.Keyword = keyword;
            ViewBag.IdChucVu = idChucVu;
            ViewBag.TrangThai = trangThai;
            ViewBag.HocSinhsChuaGan = _context.HocSinhs
                .Where(x => x.IdTaiKhoan == null)
                .OrderBy(x => x.HoTen)
                .ToList();
            ViewBag.GiaoViensChuaGan = _context.GiaoViens
                .Where(x => x.IdTaiKhoan == null)
                .OrderBy(x => x.HoTen)
                .ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string username, string password, int idChucVu, string? email, int? idHocSinhLienKet, int? idGiaoVienLienKet)
        {
            username = username?.Trim() ?? string.Empty;
            if (!ValidateProfileLink(idChucVu, idHocSinhLienKet, idGiaoVienLienKet, out var linkError))
            {
                TempData["Error"] = linkError;
                return RedirectToAction("Index");
            }

            using var transaction = _context.Database.BeginTransaction();

            if (!_accountService.Create(username, password, idChucVu, email))
            {
                TempData["Error"] = "Không thể thêm tài khoản. Username có thể đã tồn tại hoặc mật khẩu dưới 6 ký tự.";
                return RedirectToAction("Index");
            }

            var account = _accountService.GetByUsername(username);
            if (account == null || !LinkProfile(account.IdTaiKhoan, idChucVu, idHocSinhLienKet, idGiaoVienLienKet, out linkError))
            {
                transaction.Rollback();
                TempData["Error"] = linkError;
                return RedirectToAction("Index");
            }

            transaction.Commit();
            WriteLog("Thêm tài khoản", $"Đã thêm tài khoản {username}");
            TempData["Success"] = "Thêm tài khoản thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string username, int idChucVu, bool trangThai, string? email)
        {
            if (!HasPermissionToManage(id))
            {
                TempData["Error"] = "Bạn không có quyền thao tác trên tài khoản Quản trị viên này.";
                return RedirectToAction("Index");
            }

            if (HttpContext.Session.GetInt32("UserId") == id && (idChucVu != 1 || !trangThai))
            {
                TempData["Error"] = "Không thể tự khóa hoặc hạ quyền tài khoản đang đăng nhập";
                return RedirectToAction("Index");
            }

            if (!_accountService.Update(id, username, idChucVu, trangThai, email))
            {
                TempData["Error"] = "Cập nhật thất bại. Hãy kiểm tra username và chức vụ.";
                return RedirectToAction("Index");
            }

            WriteLog("Sửa tài khoản", $"Đã sửa tài khoản {username}");
            TempData["Success"] = "Cập nhật tài khoản thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!HasPermissionToManage(id))
            {
                TempData["Error"] = "Bạn không có quyền thao tác trên tài khoản Quản trị viên này.";
                return RedirectToAction("Index");
            }

            if (HttpContext.Session.GetInt32("UserId") == id)
            {
                TempData["Error"] = "Không thể tự xóa tài khoản đang đăng nhập";
                return RedirectToAction("Index");
            }

            if (!_accountService.Delete(id))
            {
                TempData["Error"] = "Không thể xóa tài khoản quản trị hoặc tài khoản đang được sử dụng";
                return RedirectToAction("Index");
            }

            WriteLog("Xóa tài khoản", $"Đã xóa tài khoản ID {id}");
            TempData["Success"] = "Xóa tài khoản thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            if (!HasPermissionToManage(id))
            {
                TempData["Error"] = "Bạn không có quyền thao tác trên tài khoản Quản trị viên này.";
                return RedirectToAction("Index");
            }

            if (HttpContext.Session.GetInt32("UserId") == id)
            {
                TempData["Error"] = "Không thể tự khóa tài khoản đang đăng nhập";
                return RedirectToAction("Index");
            }

            if (!_accountService.ToggleStatus(id))
            {
                TempData["Error"] = "Không thể đổi trạng thái tài khoản quản trị";
                return RedirectToAction("Index");
            }

            WriteLog("Khóa/Mở khóa tài khoản", $"Đã đổi trạng thái tài khoản ID {id}");
            TempData["Success"] = "Cập nhật trạng thái thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(int id, string newPassword)
        {
            if (!HasPermissionToManage(id))
            {
                TempData["Error"] = "Bạn không có quyền thao tác trên tài khoản Quản trị viên này.";
                return RedirectToAction("Index");
            }

            if (!_accountService.ResetPassword(id, newPassword))
            {
                TempData["Error"] = "Đặt lại mật khẩu thất bại. Mật khẩu phải có ít nhất 6 ký tự.";
                return RedirectToAction("Index");
            }

            WriteLog("Đặt lại mật khẩu", $"Đã đặt lại mật khẩu tài khoản ID {id}");
            TempData["Success"] = "Đặt lại mật khẩu thành công";
            return RedirectToAction("Index");
        }

        private void WriteLog(string action, string content)
        {
            var admin = HttpContext.Session.GetString("Username") ?? "Admin";
            _nhatKyService.GhiLog(admin, action, content);
        }

        private bool HasPermissionToManage(int targetAccountId)
        {
            var currentRoleId = HttpContext.Session.GetInt32("RoleId");
            if (currentRoleId == 5) return true; // System Admin can do anything

            var targetAccount = _context.TaiKhoans.AsNoTracking().FirstOrDefault(x => x.IdTaiKhoan == targetAccountId);
            if (targetAccount == null) return false;

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == targetAccountId) return true; // Self edit is fine

            if (currentRoleId == 1 && (targetAccount.IdChucVu == 1 || targetAccount.IdChucVu == 5))
            {
                return false;
            }

            return true;
        }

        private bool ValidateProfileLink(int idChucVu, int? idHocSinhLienKet, int? idGiaoVienLienKet, out string error)
        {
            error = string.Empty;

            if (idChucVu == 3 && idHocSinhLienKet.HasValue)
            {
                var hocSinh = _context.HocSinhs.AsNoTracking().FirstOrDefault(x => x.IdHocSinh == idHocSinhLienKet.Value);
                if (hocSinh == null)
                {
                    error = "Học sinh được chọn không tồn tại.";
                    return false;
                }

                if (hocSinh.IdTaiKhoan.HasValue)
                {
                    error = "Học sinh này đã được gắn tài khoản.";
                    return false;
                }
            }

            if (idChucVu == 2 && idGiaoVienLienKet.HasValue)
            {
                var giaoVien = _context.GiaoViens.AsNoTracking().FirstOrDefault(x => x.IdGiaoVien == idGiaoVienLienKet.Value);
                if (giaoVien == null)
                {
                    error = "Giáo viên được chọn không tồn tại.";
                    return false;
                }

                if (giaoVien.IdTaiKhoan.HasValue)
                {
                    error = "Giáo viên này đã được gắn tài khoản.";
                    return false;
                }
            }

            return true;
        }

        private bool LinkProfile(int idTaiKhoan, int idChucVu, int? idHocSinhLienKet, int? idGiaoVienLienKet, out string error)
        {
            error = string.Empty;

            if (idChucVu == 3 && idHocSinhLienKet.HasValue)
            {
                var hocSinh = _context.HocSinhs.FirstOrDefault(x => x.IdHocSinh == idHocSinhLienKet.Value && x.IdTaiKhoan == null);
                if (hocSinh == null)
                {
                    error = "Không thể gắn tài khoản cho học sinh đã chọn.";
                    return false;
                }

                hocSinh.IdTaiKhoan = idTaiKhoan;
                _context.SaveChanges();
            }

            if (idChucVu == 2 && idGiaoVienLienKet.HasValue)
            {
                var giaoVien = _context.GiaoViens.FirstOrDefault(x => x.IdGiaoVien == idGiaoVienLienKet.Value && x.IdTaiKhoan == null);
                if (giaoVien == null)
                {
                    error = "Không thể gắn tài khoản cho giáo viên đã chọn.";
                    return false;
                }

                giaoVien.IdTaiKhoan = idTaiKhoan;
                _context.SaveChanges();
            }

            return true;
        }
    }
}

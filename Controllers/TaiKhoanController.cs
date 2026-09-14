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
        public IActionResult Create(string sdt, string password, int idChucVu, string? email, int? idHocSinhLienKet, int? idGiaoVienLienKet)
        {
            sdt = sdt?.Trim() ?? string.Empty;
            if (idChucVu == SystemRoleIds.SystemAdmin)
            {
                TempData["Error"] = "Hệ thống chỉ có một tài khoản System Admin và không thể tạo thêm.";
                return RedirectToAction("Index");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^0\d{9}$"))
            {
                TempData["Error"] = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.";
                return RedirectToAction("Index");
            }

            if (!ValidateProfileLink(idChucVu, idHocSinhLienKet, idGiaoVienLienKet, out var linkError))
            {
                TempData["Error"] = linkError;
                return RedirectToAction("Index");
            }

            using var transaction = _context.Database.BeginTransaction();

            if (!_accountService.Create(sdt, password, idChucVu, email))
            {
                TempData["Error"] = "Không thể thêm tài khoản. Số điện thoại có thể đã tồn tại hoặc mật khẩu dưới 6 ký tự.";
                return RedirectToAction("Index");
            }

            var account = _accountService.GetByUsername(sdt, idChucVu);
            if (account == null || !LinkProfile(account.IdTaiKhoan, idChucVu, idHocSinhLienKet, idGiaoVienLienKet, out linkError))
            {
                transaction.Rollback();
                TempData["Error"] = linkError;
                return RedirectToAction("Index");
            }

            transaction.Commit();
            WriteLog("Thêm tài khoản", $"Đã thêm tài khoản có số điện thoại {sdt}");
            TempData["Success"] = "Thêm tài khoản thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, int idChucVu, string? email)
        {
            var account = _context.TaiKhoans.Find(id);
            if (account == null)
                return NotFound();

            if (!HasPermissionToManage(id))
            {
                TempData["Error"] = "Bạn không có quyền thao tác trên tài khoản System Admin này.";
                return RedirectToAction("Index");
            }

            if (idChucVu == SystemRoleIds.SystemAdmin && HttpContext.Session.GetInt32("UserId") != id)
            {
                TempData["Error"] = "Không thể gán vai trò System Admin cho tài khoản khác.";
                return RedirectToAction("Index");
            }

            if (HttpContext.Session.GetInt32("UserId") == id && idChucVu != SystemRoleIds.SystemAdmin)
            {
                TempData["Error"] = "Không thể tự khóa hoặc hạ quyền tài khoản đang đăng nhập";
                return RedirectToAction("Index");
            }

            if (!_accountService.Update(id, idChucVu, email))
            {
                TempData["Error"] = "Cập nhật thất bại. Hãy kiểm tra chức vụ tài khoản.";
                return RedirectToAction("Index");
            }

            WriteLog("Sửa tài khoản", $"Đã sửa tài khoản {account.Username}");
            TempData["Success"] = "Cập nhật tài khoản thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
             var taiKhoan = _context.TaiKhoans
        .FirstOrDefault(x => x.IdTaiKhoan == id);

        // if (taiKhoan == null)
        // {
        //     TempData["Error"] = "Không tìm thấy tài khoản.";
        //     return RedirectToAction("Index");
        // }

        // Không cho xóa System Admin
        if (taiKhoan.IdChucVu == SystemRoleIds.SystemAdmin)
        {
            TempData["Error"] = "Không thể xóa tài khoản System Admin.";
            return RedirectToAction("Index");
        }

        try
        {
            // 1. Gỡ tài khoản khỏi học sinh
            var hocSinhs = _context.HocSinhs
                .Where(x => x.IdTaiKhoan == id)
                .ToList();

            foreach (var x in hocSinhs)
            {
                x.IdTaiKhoan = null;
            }

            // 2. Gỡ tài khoản khỏi giáo viên
            var giaoViens = _context.GiaoViens
                .Where(x => x.IdTaiKhoan == id)
                .ToList();

            foreach (var x in giaoViens)
            {
                x.IdTaiKhoan = null;
            }

            // 3. Gỡ tài khoản khỏi phụ huynh
            var phuHuynhs = _context.PhuHuynhs
                .Where(x => x.IdTaiKhoan == id)
                .ToList();

            foreach (var x in phuHuynhs)
            {
                x.IdTaiKhoan = null;
            }

            // 4. Nếu tài khoản từng lập phiếu điểm
            var phieuDiems = _context.PhieuDiems
                .Where(x => x.NguoiLap == id)
                .ToList();

            foreach (var x in phieuDiems)
            {
                x.NguoiLap = null;
            }

            // Lưu việc gỡ liên kết
            _context.SaveChanges();

            // 5. Xóa tài khoản
            _context.TaiKhoans.Remove(taiKhoan);
            _context.SaveChanges();

            WriteLog("Xóa tài khoản", $"Đã xóa tài khoản ID {id}");

            TempData["Success"] = "Xóa tài khoản thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Xóa thất bại: " + ex.Message;
        }

        return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            if (!HasPermissionToManage(id))
            {
                TempData["Error"] = "Bạn không có quyền thao tác trên tài khoản System Admin này.";
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
                TempData["Error"] = "Bạn không có quyền thao tác trên tài khoản System Admin này.";
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
            var targetAccount = _context.TaiKhoans.AsNoTracking().FirstOrDefault(x => x.IdTaiKhoan == targetAccountId);
            if (targetAccount == null) return false;

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == targetAccountId) return true; // Self edit is fine

            if (currentRoleId == SystemRoleIds.SystemAdmin && targetAccount.IdChucVu == SystemRoleIds.SystemAdmin)
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

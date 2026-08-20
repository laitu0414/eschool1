using eSchool.Services;
using Microsoft.AspNetCore.Mvc;

namespace eSchool.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IEmailSender _emailSender;
        private readonly INhatKyService _nhatKyService;

        public AccountController(
            IAccountService accountService,
            IEmailSender emailSender,
            INhatKyService nhatKyService)
        {
            _accountService = accountService;
            _emailSender = emailSender;
            _nhatKyService = nhatKyService;
        }

        public IActionResult Login()
        {
            return RedirectToAction("Index", "Home", new { openLogin = true });
        }

        public IActionResult ForgotPassword()
        {
            return RedirectToAction("Index", "Home", new { openForgotPassword = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string username)
        {
            username = username?.Trim() ?? string.Empty;
            var account = _accountService.GetByUsername(username);

            if (account == null || !account.TrangThai)
            {
                TempData["ForgotPasswordError"] = "Tài khon không tồn tại hoặc ảđang bị khoá";
                return RedirectToAction("Index", "Home", new { openForgotPassword = true });
            }

            if (string.IsNullOrWhiteSpace(account.Email))
            {
                TempData["ForgotPasswordError"] = "Tài khoản này chưa có Email để nhận mật khẩu mới";
                return RedirectToAction("Index", "Home", new { openForgotPassword = true });
            }

            var newPassword = _accountService.GeneratePassword();
            try
            {
                await _emailSender.SendAsync(
                    account.Email,
                    "eSchool - Mật khẩu mới",
                    $"Xin chào {account.Username},\n\nMật khẩu tạm thời của bạn là: {newPassword}\n\nSau khi đăng nhập, hệ thống sẽ yêu cầu bạn đổi mật khẩu mới.");
            }
            catch (Exception ex)
            {
                TempData["ForgotPasswordError"] = $"Không gửi được email: {ex.Message}";
                return RedirectToAction("Index", "Home", new { openForgotPassword = true });
            }

            _accountService.ResetPasswordAndRequireChange(account.IdTaiKhoan, newPassword);
            _nhatKyService.GhiLog(account.Username, "Quên mật khẩu", "Đã đặt lại mật khẩu tạm và yêu cầu đổi mật khẩu");
            TempData["AuthSuccess"] = "Mật khẩu tạm đã được gửi về email. Sau khi đăng nhập, bạn cần đổi mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            var account = _accountService.Login(username, password);

            if (account == null)
            {
                TempData["LoginError"] = "Sai tai khoan hoac mat khau";
                return RedirectToAction("Index", "Home", new { openLogin = true });
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", account.IdTaiKhoan);
            HttpContext.Session.SetString("Username", account.Username);
            HttpContext.Session.SetInt32("RoleId", account.IdChucVu);
            HttpContext.Session.SetInt32("MustChangePassword", account.BatBuocDoiMatKhau ? 1 : 0);
            _nhatKyService.GhiLog(account.Username, "Dang nhap", "Dang nhap he thong thanh cong");

            if (account.BatBuocDoiMatKhau)
            {
                return RedirectToAction(nameof(ForceChangePassword));
            }

            return RedirectByRole(account.IdChucVu);
        }

        public IActionResult ForceChangePassword()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForceChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var username = HttpContext.Session.GetString("Username") ?? "User";

            if (userId == null || roleId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                ViewBag.Error = "Vui long nhap mat khau cu";
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ViewBag.Error = "Mat khau moi phai co it nhat 6 ky tu";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Nhap lai mat khau moi khong khop";
                return View();
            }

            if (!_accountService.ChangePassword(userId.Value, oldPassword, newPassword))
            {
                ViewBag.Error = "Mat khau cu khong dung";
                return View();
            }

            _nhatKyService.GhiLog(username, "Doi mat khau", "Da doi mat khau bat buoc sau khi quen mat khau");
            HttpContext.Session.SetInt32("MustChangePassword", 0);
            return RedirectByRole(roleId.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrWhiteSpace(username))
            {
                _nhatKyService.GhiLog(username, "Dang xuat", "Dang xuat khoi he thong");
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectByRole(int roleId)
        {
            return roleId switch
            {
                1 => RedirectToAction("Index", "Admin"),
                2 => RedirectToAction("HoSoCaNhan", "GiaoVien"),
                3 => RedirectToAction("HoSoCaNhan", "HocSinh"),
                4 => RedirectToAction("HoSoCaNhan", "HocSinh"),
                5 => RedirectToAction("Index", "Admin"),
                _ => InvalidRole()
            };
        }

        private IActionResult InvalidRole()
        {
            HttpContext.Session.Clear();
            TempData["LoginError"] = "Tai khoan chua duoc gan quyen hop le";
            return RedirectToAction("Index", "Home", new { openLogin = true });
        }
    }
}

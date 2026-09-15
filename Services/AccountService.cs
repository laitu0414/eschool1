using eSchool.Models;
using eSchool.Repositories;
using eSchool.Infrastructure;
using System.Security.Cryptography;

namespace eSchool.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepo;

        public AccountService(IAccountRepository accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public TaiKhoan? Login(string username, string password, int idChucVu)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                !_accountRepo.RoleExists(idChucVu))
                return null;

            return _accountRepo.Login(username.Trim(), password, idChucVu);
        }

        public List<TaiKhoan> GetAll()
        {
            return _accountRepo.GetAll();
        }

        public List<TaiKhoan> Search(string? keyword, int? idChucVu, bool? trangThai)
        {
            var data = _accountRepo.GetAll();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                data = data.Where(x =>
                    x.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    new[] { x.GiaoVien?.HoTen, x.HocSinh?.HoTen, x.PhuHuynh?.HoTen }
                        .Any(name => !string.IsNullOrWhiteSpace(name) &&
                                     name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    new[] { x.Email, x.GiaoVien?.Email, x.HocSinh?.Email, x.PhuHuynh?.Email }
                        .Any(email => !string.IsNullOrWhiteSpace(email) &&
                                      email.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (idChucVu != null && idChucVu > 0)
            {
                data = data.Where(x => x.IdChucVu == idChucVu).ToList();
            }

            if (trangThai != null)
            {
                data = data.Where(x => x.TrangThai == trangThai).ToList();
            }

            return data;
        }

        public bool Create(string sdt, string password, int idChucVu, string? email)
        {
            sdt = sdt?.Trim() ?? string.Empty;
            email = email?.Trim();

            if (!System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^0\d{9}$") ||
                password?.Length < 6 ||
                idChucVu == SystemRoleIds.SystemAdmin ||
                !_accountRepo.RoleExists(idChucVu))
                return false;

            if (_accountRepo.ExistsUsername(sdt, idChucVu))
                return false;

            var account = new TaiKhoan
            {
                // Số điện thoại vẫn được lưu ở cột Username để tương thích dữ liệu và luồng đăng nhập hiện có.
                Username = sdt,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                IdChucVu = idChucVu,
                TrangThai = true
            };

            _accountRepo.Add(account);
            _accountRepo.Save();

            return true;
        }

        public bool Update(int id, int idChucVu, string? email)
        {
            var account = _accountRepo.GetById(id);

            if (account == null)
                return false;

            email = email?.Trim();
            if (!_accountRepo.RoleExists(idChucVu) ||
                (idChucVu == SystemRoleIds.SystemAdmin && account.IdChucVu != SystemRoleIds.SystemAdmin) ||
                _accountRepo.ExistsUsername(account.Username, idChucVu, id))
                return false;

            account.Email = string.IsNullOrWhiteSpace(email) ? null : email;
            account.IdChucVu = idChucVu;
            _accountRepo.Update(account);
            _accountRepo.Save();

            return true;
        }

        public TaiKhoan? GetByUsername(string username, int? idChucVu = null)
        {
            username = username?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(username) ? null : _accountRepo.GetByUsername(username, idChucVu);
        }

        public string GeneratePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$";
            return RandomNumberGenerator.GetString(chars, 10);
        }

        public bool Delete(int id)
        {
            var account = _accountRepo.GetById(id);

            if (account == null)
                return false;

            if (account.IdChucVu == SystemRoleIds.SystemAdmin)
                return false;

            try
            {
                _accountRepo.Delete(account);
                _accountRepo.Save();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return false;
            }

            return true;
        }

        public bool ChangePassword(int id, string oldPassword, string newPassword)
        {
            var account = _accountRepo.GetById(id);

            if (account == null)
                return false;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return false;

            var isHashed = account.Password.StartsWith("$2", StringComparison.Ordinal);
            var isValid = isHashed
                ? BCrypt.Net.BCrypt.Verify(oldPassword, account.Password)
                : account.Password == oldPassword;

            if (!isValid)
                return false;

            account.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            account.BatBuocDoiMatKhau = false;

            _accountRepo.Update(account);
            _accountRepo.Save();

            return true;
        }

        public bool ResetPassword(int id, string newPassword)
        {
            var account = _accountRepo.GetById(id);

            if (account == null || string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return false;

            account.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            account.BatBuocDoiMatKhau = false;
            _accountRepo.Update(account);
            _accountRepo.Save();

            return true;
        }

        public bool ResetPasswordAndRequireChange(int id, string newPassword)
        {
            var account = _accountRepo.GetById(id);

            if (account == null || string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return false;

            account.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            account.BatBuocDoiMatKhau = true;
            _accountRepo.Update(account);
            _accountRepo.Save();

            return true;
        }

        public bool ToggleStatus(int id)
        {
            var account = _accountRepo.GetById(id);

            if (account == null)
                return false;

            if (account.IdChucVu == SystemRoleIds.SystemAdmin)
                return false;

            account.TrangThai = !account.TrangThai;

            _accountRepo.Update(account);
            _accountRepo.Save();

            return true;
        }
    }
}

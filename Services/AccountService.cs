using eSchool.Models;
using eSchool.Repositories;
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

        public TaiKhoan? Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            return _accountRepo.Login(username, password);
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
                    (!string.IsNullOrWhiteSpace(x.Email) && x.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase))
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

        public bool Create(string username, string password, int idChucVu, string? email)
        {
            username = username?.Trim() ?? string.Empty;
            email = email?.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                password?.Length < 6 ||
                !_accountRepo.RoleExists(idChucVu))
                return false;

            if (_accountRepo.ExistsUsername(username))
                return false;

            var account = new TaiKhoan
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                IdChucVu = idChucVu,
                TrangThai = true
            };

            _accountRepo.Add(account);
            _accountRepo.Save();

            return true;
        }

        public bool Update(int id, string username, int idChucVu, bool trangThai, string? email)
        {
            var account = _accountRepo.GetById(id);

            if (account == null)
                return false;

            username = username?.Trim() ?? string.Empty;
            email = email?.Trim();
            if (string.IsNullOrWhiteSpace(username) ||
                !_accountRepo.RoleExists(idChucVu) ||
                _accountRepo.ExistsUsername(username, id))
                return false;

            account.Username = username;
            account.Email = string.IsNullOrWhiteSpace(email) ? null : email;
            account.IdChucVu = idChucVu;
            account.TrangThai = trangThai;

            _accountRepo.Update(account);
            _accountRepo.Save();

            return true;
        }

        public TaiKhoan? GetByUsername(string username)
        {
            username = username?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(username) ? null : _accountRepo.GetByUsername(username);
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

            if (account.IdChucVu == 1)
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

            if (account.IdChucVu == 1)
                return false;

            account.TrangThai = !account.TrangThai;

            _accountRepo.Update(account);
            _accountRepo.Save();

            return true;
        }
    }
}

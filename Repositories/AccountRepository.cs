using eSchool.Models;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public TaiKhoan? Login(string username, string password, int idChucVu)
        {
            var account = _context.TaiKhoans.FirstOrDefault(x =>
                (x.Username == username || x.Email == username) &&
                x.IdChucVu == idChucVu && x.TrangThai);

            if (account == null)
                return null;

            var isHashed = account.Password.StartsWith("$2", StringComparison.Ordinal);
            var isValid = isHashed
                ? BCrypt.Net.BCrypt.Verify(password, account.Password)
                : account.Password == password;

            if (!isValid)
                return null;

            // Nâng cấp tài khoản cũ sang BCrypt sau lần đăng nhập hợp lệ.
            if (!isHashed)
            {
                account.Password = BCrypt.Net.BCrypt.HashPassword(password);
                _context.SaveChanges();
            }

            return account;
        }

        public List<TaiKhoan> GetAll()
        {
            return _context.TaiKhoans
                .Include(x => x.ChucVu)
                .Include(x => x.GiaoVien)
                .Include(x => x.HocSinh)
                .Include(x => x.PhuHuynh)
                .ToList();
        }

        public TaiKhoan? GetById(int id)
        {
            return _context.TaiKhoans.Find(id);
        }

        public TaiKhoan? GetByUsername(string username, int? idChucVu = null)
        {
            return _context.TaiKhoans.FirstOrDefault(x =>
                (x.Username == username || x.Email == username) &&
                (!idChucVu.HasValue || x.IdChucVu == idChucVu.Value));
        }

        public bool ExistsUsername(string username, int idChucVu, int? excludeId = null)
        {
            return _context.TaiKhoans.Any(x =>
                x.Username == username &&
                x.IdChucVu == idChucVu &&
                (!excludeId.HasValue || x.IdTaiKhoan != excludeId.Value));
        }

        public bool RoleExists(int idChucVu)
        {
            return _context.ChucVus.Any(x => x.IdChucVu == idChucVu);
        }

        public void Add(TaiKhoan taiKhoan)
        {
            _context.TaiKhoans.Add(taiKhoan);
        }

        public void Update(TaiKhoan taiKhoan)
        {
            _context.TaiKhoans.Update(taiKhoan);
        }

        public void Delete(TaiKhoan taiKhoan)
        {
            _context.TaiKhoans.Remove(taiKhoan);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

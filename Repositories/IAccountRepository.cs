using eSchool.Models;

namespace eSchool.Repositories
{
    public interface IAccountRepository
    {
        TaiKhoan? Login(string username, string password, int idChucVu);
        List<TaiKhoan> GetAll();
        TaiKhoan? GetById(int id);
        TaiKhoan? GetByUsername(string username, int? idChucVu = null);
        bool ExistsUsername(string username, int idChucVu, int? excludeId = null);
        bool RoleExists(int idChucVu);
        void Add(TaiKhoan taiKhoan);
        void Update(TaiKhoan taiKhoan);
        void Delete(TaiKhoan taiKhoan);
        void Save();
    }
}

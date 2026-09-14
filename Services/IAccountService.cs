using eSchool.Models;

namespace eSchool.Services
{
    public interface IAccountService
    {
        TaiKhoan? Login(string username, string password, int idChucVu);

        List<TaiKhoan> GetAll();

        List<TaiKhoan> Search(string? keyword, int? idChucVu, bool? trangThai);

        bool Create(string sdt, string password, int idChucVu, string? email);

        bool Update(int id, int idChucVu, string? email);

        bool Delete(int id);

        bool ChangePassword(int id, string oldPassword, string newPassword);

        bool ResetPassword(int id, string newPassword);

        bool ResetPasswordAndRequireChange(int id, string newPassword);

        TaiKhoan? GetByUsername(string username, int? idChucVu = null);

        string GeneratePassword();

        bool ToggleStatus(int id);
    }
}

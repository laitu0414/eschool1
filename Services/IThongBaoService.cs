using eSchool.Models;

namespace eSchool.Services
{
    public interface IThongBaoService
    {
        List<ThongBao> GetAll();
        bool Create(string tieuDe, string noiDung, int idTaiKhoan);
        bool Update(int id, string tieuDe, string noiDung);
        bool Delete(int id);
    }
}
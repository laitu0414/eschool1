using eSchool.Models;

namespace eSchool.Services
{
    public interface IChucVuService
    {
        List<ChucVu> GetAll();
        bool Create(string tenChucVu);
        bool Update(int id, string tenChucVu);
        bool Delete(int id);
    }
}
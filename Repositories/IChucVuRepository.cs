using eSchool.Models;

namespace eSchool.Repositories
{
    public interface IChucVuRepository
    {
        List<ChucVu> GetAll();
        ChucVu? GetById(int id);
        bool ExistsName(string tenChucVu);
        void Add(ChucVu chucVu);
        void Update(ChucVu chucVu);
        void Delete(ChucVu chucVu);
        void Save();
    }
}
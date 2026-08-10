using eSchool.Models;

namespace eSchool.Repositories
{
    public interface IChuyenLopRepository
    {
        List<ChuyenLop> GetAll();
        void Add(ChuyenLop chuyenLop);
        void Save();
    }
}
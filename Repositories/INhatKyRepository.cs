using eSchool.Models;

namespace eSchool.Repositories
{
    public interface INhatKyRepository
    {
        List<NhatKyHoatDong> GetAll();
        void Add(NhatKyHoatDong log);
        void Save();
    }
}
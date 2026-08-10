using eSchool.Models;

namespace eSchool.Services
{
    public interface INhatKyService
    {
        List<NhatKyHoatDong> GetAll();
        void GhiLog(string username, string hanhDong, string noiDung);
    }
}
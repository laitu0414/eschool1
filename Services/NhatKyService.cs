using eSchool.Models;
using eSchool.Repositories;

namespace eSchool.Services
{
    public class NhatKyService : INhatKyService
    {
        private readonly INhatKyRepository _repo;

        public NhatKyService(INhatKyRepository repo)
        {
            _repo = repo;
        }

        public List<NhatKyHoatDong> GetAll()
        {
            return _repo.GetAll();
        }

        public void GhiLog(string username, string hanhDong, string noiDung)
        {
            _repo.Add(new NhatKyHoatDong
            {
                TenDangNhap = username,
                HanhDong = hanhDong,
                NoiDung = noiDung,
                ThoiGian = DateTime.Now
            });

            _repo.Save();
        }
    }
}
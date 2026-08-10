using eSchool.Models;
using eSchool.Repositories;

namespace eSchool.Services
{
    public class ThongBaoService : IThongBaoService
    {
        private readonly IThongBaoRepository _thongBaoRepo;

        public ThongBaoService(IThongBaoRepository thongBaoRepo)
        {
            _thongBaoRepo = thongBaoRepo;
        }

        public List<ThongBao> GetAll()
        {
            return _thongBaoRepo.GetAll();
        }

        public bool Create(string tieuDe, string noiDung, int idTaiKhoan)
        {
            if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(noiDung))
                return false;

            var thongBao = new ThongBao
            {
                TieuDe = tieuDe,
                NoiDung = noiDung,
                NgayTao = DateTime.Now,
                IdTaiKhoan = idTaiKhoan
            };

            _thongBaoRepo.Add(thongBao);
            _thongBaoRepo.Save();

            return true;
        }

        public bool Update(int id, string tieuDe, string noiDung)
        {
            var thongBao = _thongBaoRepo.GetById(id);

            if (thongBao == null)
                return false;

            if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(noiDung))
                return false;

            thongBao.TieuDe = tieuDe;
            thongBao.NoiDung = noiDung;

            _thongBaoRepo.Update(thongBao);
            _thongBaoRepo.Save();

            return true;
        }

        public bool Delete(int id)
        {
            var thongBao = _thongBaoRepo.GetById(id);

            if (thongBao == null)
                return false;

            _thongBaoRepo.Delete(thongBao);
            _thongBaoRepo.Save();

            return true;
        }
    }
}
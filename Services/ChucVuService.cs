using eSchool.Models;
using eSchool.Repositories;

namespace eSchool.Services
{
    public class ChucVuService : IChucVuService
    {
        private readonly IChucVuRepository _chucVuRepo;

        public ChucVuService(IChucVuRepository chucVuRepo)
        {
            _chucVuRepo = chucVuRepo;
        }

        public List<ChucVu> GetAll()
        {
            return _chucVuRepo.GetAll();
        }

        public bool Create(string tenChucVu)
        {
            if (string.IsNullOrWhiteSpace(tenChucVu))
                return false;

            if (_chucVuRepo.ExistsName(tenChucVu))
                return false;

            var chucVu = new ChucVu
            {
                TenChucVu = tenChucVu
            };

            _chucVuRepo.Add(chucVu);
            _chucVuRepo.Save();

            return true;
        }

        public bool Update(int id, string tenChucVu)
        {
            var chucVu = _chucVuRepo.GetById(id);

            if (chucVu == null)
                return false;

            if (string.IsNullOrWhiteSpace(tenChucVu))
                return false;

            chucVu.TenChucVu = tenChucVu;

            _chucVuRepo.Update(chucVu);
            _chucVuRepo.Save();

            return true;
        }

        public bool Delete(int id)
        {
            var chucVu = _chucVuRepo.GetById(id);

            if (chucVu == null)
                return false;

            _chucVuRepo.Delete(chucVu);
            _chucVuRepo.Save();

            return true;
        }
    }
}
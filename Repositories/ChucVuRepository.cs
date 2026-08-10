using eSchool.Models;

namespace eSchool.Repositories
{
    public class ChucVuRepository : IChucVuRepository
    {
        private readonly AppDbContext _context;

        public ChucVuRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<ChucVu> GetAll()
        {
            return _context.ChucVus.ToList();
        }

        public ChucVu? GetById(int id)
        {
            return _context.ChucVus.Find(id);
        }

        public bool ExistsName(string tenChucVu)
        {
            return _context.ChucVus.Any(x => x.TenChucVu == tenChucVu);
        }

        public void Add(ChucVu chucVu)
        {
            _context.ChucVus.Add(chucVu);
        }

        public void Update(ChucVu chucVu)
        {
            _context.ChucVus.Update(chucVu);
        }

        public void Delete(ChucVu chucVu)
        {
            _context.ChucVus.Remove(chucVu);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
using eSchool.Models;

namespace eSchool.Repositories
{
    public class PhuHuynhRepository : IPhuHuynhRepository
    {
        private readonly AppDbContext _context;

        public PhuHuynhRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<PhuHuynh> GetAll(string? keyword)
        {
            var query = _context.PhuHuynhs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.HoTen.Contains(keyword) ||
                    (x.SDT != null && x.SDT.Contains(keyword)));
            }

            return query.OrderByDescending(x => x.IdPhuHuynh).ToList();
        }

        public PhuHuynh? GetById(int id)
        {
            return _context.PhuHuynhs.Find(id);
        }

        public void Add(PhuHuynh phuHuynh)
        {
            _context.PhuHuynhs.Add(phuHuynh);
        }

        public void Update(PhuHuynh phuHuynh)
        {
            _context.PhuHuynhs.Update(phuHuynh);
        }

        public void Delete(int id)
        {
            var ph = _context.PhuHuynhs.Find(id);
            if (ph != null)
            {
                ph.TrangThai = false;
                _context.PhuHuynhs.Update(ph);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

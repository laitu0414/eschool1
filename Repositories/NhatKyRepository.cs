using eSchool.Models;

namespace eSchool.Repositories
{
    public class NhatKyRepository : INhatKyRepository
    {
        private readonly AppDbContext _context;

        public NhatKyRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<NhatKyHoatDong> GetAll()
        {
            return _context.NhatKyHoatDongs
                .OrderByDescending(x => x.ThoiGian)
                .ToList();
        }

        public void Add(NhatKyHoatDong log)
        {
            _context.NhatKyHoatDongs.Add(log);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
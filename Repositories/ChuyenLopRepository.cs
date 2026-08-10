using eSchool.Models;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Repositories
{
    public class ChuyenLopRepository : IChuyenLopRepository
    {
        private readonly AppDbContext _context;

        public ChuyenLopRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<ChuyenLop> GetAll()
        {
            return _context.ChuyenLops
                .Include(x => x.HocSinh)
                .OrderByDescending(x => x.IdChuyenLop)
                .ToList();
        }

        public void Add(ChuyenLop chuyenLop)
        {
            _context.ChuyenLops.Add(chuyenLop);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
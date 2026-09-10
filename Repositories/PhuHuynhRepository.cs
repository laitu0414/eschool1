using eSchool.Models;

using Microsoft.EntityFrameworkCore;

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
            var query = _context.PhuHuynhs
                .Include(x => x.HocSinhPhuHuynhs!)
                    .ThenInclude(x => x.HocSinh)
                .AsQueryable();

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
            return _context.PhuHuynhs
                .Include(x => x.HocSinhPhuHuynhs!)
                    .ThenInclude(x => x.HocSinh)
                .FirstOrDefault(x => x.IdPhuHuynh == id);
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
            var ph = _context.PhuHuynhs
                .Include(x => x.TaiKhoan)
                .FirstOrDefault(x => x.IdPhuHuynh == id);

            if (ph == null)
                return;

            // The HocSinhPhuHuynh records are cascade-deleted with the parent.
            // Remove the dedicated parent account to prevent an orphaned login.
            if (ph.TaiKhoan?.IdChucVu == 4)
                _context.TaiKhoans.Remove(ph.TaiKhoan);

            _context.PhuHuynhs.Remove(ph);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

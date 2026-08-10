using eSchool.Models;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Repositories
{
    public class HocSinhRepository : IHocSinhRepository
    {
        private readonly AppDbContext _context;

        public HocSinhRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<HocSinh> GetAll(string? keyword, int? lopId, bool? trangThai)
        {
            var query = _context.HocSinhs
                .Include(x => x.LopHoc)
                .Include(x => x.TaiKhoan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaHS.Contains(keyword) ||
                    x.HoTen.Contains(keyword) ||
                    (x.SDT != null && x.SDT.Contains(keyword)));
            }

            if (lopId.HasValue)
            {
                query = query.Where(x => x.IdLopHoc == lopId.Value);
            }

            if (trangThai.HasValue)
            {
                query = query.Where(x => x.TrangThai == trangThai.Value);
            }

            return query.OrderByDescending(x => x.IdHocSinh).ToList();
        }

        public HocSinh? GetById(int id)
        {
            return _context.HocSinhs
                .Include(x => x.LopHoc)
                .Include(x => x.TaiKhoan)
                .Include(x => x.HocSinhPhuHuynhs)
                    .ThenInclude(x => x.PhuHuynh)
                .FirstOrDefault(x => x.IdHocSinh == id);
        }

        public void Add(HocSinh hocSinh)
        {
            _context.HocSinhs.Add(hocSinh);
        }

        public void Update(HocSinh hocSinh)
        {
            _context.HocSinhs.Update(hocSinh);
        }

        public void Delete(int id)
        {
            var hocSinh = _context.HocSinhs.Find(id);
            if (hocSinh != null)
            {
                hocSinh.TrangThai = false;
                _context.HocSinhs.Update(hocSinh);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

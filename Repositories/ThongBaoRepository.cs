using eSchool.Models;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Repositories
{
    public class ThongBaoRepository : IThongBaoRepository
    {
        private readonly AppDbContext _context;

        public ThongBaoRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<ThongBao> GetAll()
        {
            return _context.ThongBaos
                .Include(x => x.TaiKhoan)
                .OrderByDescending(x => x.NgayTao)
                .ToList();
        }

        public ThongBao? GetById(int id)
        {
            return _context.ThongBaos.Find(id);
        }

        public void Add(ThongBao thongBao)
        {
            _context.ThongBaos.Add(thongBao);
        }

        public void Update(ThongBao thongBao)
        {
            _context.ThongBaos.Update(thongBao);
        }

        public void Delete(ThongBao thongBao)
        {
            _context.ThongBaos.Remove(thongBao);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
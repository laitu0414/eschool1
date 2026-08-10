using eSchool.Models;

namespace eSchool.Repositories
{
    public interface IThongBaoRepository
    {
        List<ThongBao> GetAll();
        ThongBao? GetById(int id);
        void Add(ThongBao thongBao);
        void Update(ThongBao thongBao);
        void Delete(ThongBao thongBao);
        void Save();
    }
}
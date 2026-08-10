using eSchool.Models;

namespace eSchool.Repositories
{
    public interface IHocSinhRepository
    {
        List<HocSinh> GetAll(string? keyword, int? lopId, bool? trangThai);
        HocSinh? GetById(int id);
        void Add(HocSinh hocSinh);
        void Update(HocSinh hocSinh);
        void Delete(int id);
        void Save();
    }
}
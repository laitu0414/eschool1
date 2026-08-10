using eSchool.ViewModels;

namespace eSchool.Services
{
    public interface IHocSinhService
    {
        List<HocSinhViewModel> GetAll(string? keyword, int? lopId, bool? trangThai);
        HocSinhViewModel? GetById(int id);
        void Add(HocSinhViewModel vm);
        void Update(HocSinhViewModel vm);
        void Delete(int id);
    }
}
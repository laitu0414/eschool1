using eSchool.ViewModels;

namespace eSchool.Services
{
    public interface IPhuHuynhService
    {
        List<PhuHuynhViewModel> GetAll(string? keyword);
        PhuHuynhViewModel? GetById(int id);
        void Add(PhuHuynhViewModel vm);
        void Update(PhuHuynhViewModel vm);
        void Delete(int id);
    }
}
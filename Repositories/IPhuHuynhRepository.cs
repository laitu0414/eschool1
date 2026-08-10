using eSchool.Models;

namespace eSchool.Repositories
{
    public interface IPhuHuynhRepository
    {
        List<PhuHuynh> GetAll(string? keyword);
        PhuHuynh? GetById(int id);
        void Add(PhuHuynh phuHuynh);
        void Update(PhuHuynh phuHuynh);
        void Delete(int id);
        void Save();
    }
}
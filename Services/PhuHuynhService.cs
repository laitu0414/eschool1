using eSchool.Models;
using eSchool.Repositories;
using eSchool.ViewModels;

namespace eSchool.Services
{
    public class PhuHuynhService : IPhuHuynhService
    {
        private readonly IPhuHuynhRepository _repo;

        public PhuHuynhService(IPhuHuynhRepository repo)
        {
            _repo = repo;
        }

        public List<PhuHuynhViewModel> GetAll(string? keyword)
        {
            return _repo.GetAll(keyword).Select(x => new PhuHuynhViewModel
            {
                IdPhuHuynh = x.IdPhuHuynh,
                HoTen = x.HoTen,
                SDT = x.SDT,
                Email = x.Email,
                DiaChi = x.DiaChi,
                NgheNghiep = x.NgheNghiep,
                TrangThai = x.TrangThai
            }).ToList();
        }

        public PhuHuynhViewModel? GetById(int id)
        {
            var x = _repo.GetById(id);
            if (x == null) return null;

            return new PhuHuynhViewModel
            {
                IdPhuHuynh = x.IdPhuHuynh,
                HoTen = x.HoTen,
                SDT = x.SDT,
                Email = x.Email,
                DiaChi = x.DiaChi,
                NgheNghiep = x.NgheNghiep,
                TrangThai = x.TrangThai
            };
        }

        public void Add(PhuHuynhViewModel vm)
        {
            var ph = new PhuHuynh
            {
                HoTen = vm.HoTen.Trim(),
                SDT = vm.SDT?.Trim(),
                Email = vm.Email?.Trim(),
                DiaChi = vm.DiaChi?.Trim(),
                NgheNghiep = vm.NgheNghiep?.Trim(),
                TrangThai = vm.TrangThai
            };

            _repo.Add(ph);
            _repo.Save();
        }

        public void Update(PhuHuynhViewModel vm)
        {
            var ph = _repo.GetById(vm.IdPhuHuynh);
            if (ph == null) return;

            ph.HoTen = vm.HoTen.Trim();
            ph.SDT = vm.SDT?.Trim();
            ph.Email = vm.Email?.Trim();
            ph.DiaChi = vm.DiaChi?.Trim();
            ph.NgheNghiep = vm.NgheNghiep?.Trim();
            ph.TrangThai = vm.TrangThai;

            _repo.Update(ph);
            _repo.Save();
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
            _repo.Save();
        }
    }
}

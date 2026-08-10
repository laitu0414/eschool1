using eSchool.Models;
using eSchool.Repositories;
using eSchool.ViewModels;

namespace eSchool.Services
{
    public class HocSinhService : IHocSinhService
    {
        private readonly IHocSinhRepository _repo;

        public HocSinhService(IHocSinhRepository repo)
        {
            _repo = repo;
        }

        public List<HocSinhViewModel> GetAll(string? keyword, int? lopId, bool? trangThai)
        {
            return _repo.GetAll(keyword, lopId, trangThai)
                .Select(x => new HocSinhViewModel
                {
                    IdHocSinh = x.IdHocSinh,
                    MaHS = x.MaHS,
                    HoTen = x.HoTen,
                    NgaySinh = x.NgaySinh,
                    GioiTinh = x.GioiTinh,
                    SDT = x.SDT,
                    Email = x.Email,
                    DiaChi = x.DiaChi,
                    AnhDaiDien = x.AnhDaiDien,
                    TrangThai = x.TrangThai,
                    IdTaiKhoan = x.IdTaiKhoan,
                    TenTaiKhoan = x.TaiKhoan != null ? x.TaiKhoan.Username : null,
                    IdLopHoc = x.IdLopHoc,
                    TenLop = x.LopHoc != null ? x.LopHoc.TenLop : ""
                }).ToList();
        }

        public HocSinhViewModel? GetById(int id)
        {
            var x = _repo.GetById(id);
            if (x == null) return null;

            return new HocSinhViewModel
            {
                IdHocSinh = x.IdHocSinh,
                MaHS = x.MaHS,
                HoTen = x.HoTen,
                NgaySinh = x.NgaySinh,
                GioiTinh = x.GioiTinh,
                SDT = x.SDT,
                Email = x.Email,
                DiaChi = x.DiaChi,
                AnhDaiDien = x.AnhDaiDien,
                TrangThai = x.TrangThai,
                IdTaiKhoan = x.IdTaiKhoan,
                TenTaiKhoan = x.TaiKhoan != null ? x.TaiKhoan.Username : null,
                IdLopHoc = x.IdLopHoc,
                TenLop = x.LopHoc != null ? x.LopHoc.TenLop : ""
            };
        }

        public void Add(HocSinhViewModel vm)
        {
            var hs = new HocSinh
            {
                MaHS = vm.MaHS.Trim(),
                HoTen = vm.HoTen.Trim(),
                NgaySinh = vm.NgaySinh,
                GioiTinh = vm.GioiTinh?.Trim(),
                SDT = vm.SDT?.Trim(),
                Email = vm.Email?.Trim(),
                DiaChi = vm.DiaChi?.Trim(),
                AnhDaiDien = vm.AnhDaiDien,
                TrangThai = vm.TrangThai,
                IdTaiKhoan = vm.IdTaiKhoan,
                IdLopHoc = vm.IdLopHoc
            };

            _repo.Add(hs);
            _repo.Save();
        }

        public void Update(HocSinhViewModel vm)
        {
            var hs = _repo.GetById(vm.IdHocSinh);
            if (hs == null) return;

            hs.MaHS = vm.MaHS.Trim();
            hs.HoTen = vm.HoTen.Trim();
            hs.NgaySinh = vm.NgaySinh;
            hs.GioiTinh = vm.GioiTinh?.Trim();
            hs.SDT = vm.SDT?.Trim();
            hs.Email = vm.Email?.Trim();
            hs.DiaChi = vm.DiaChi?.Trim();
            hs.AnhDaiDien = vm.AnhDaiDien;
            hs.TrangThai = vm.TrangThai;
            hs.IdTaiKhoan = vm.IdTaiKhoan;
            hs.IdLopHoc = vm.IdLopHoc;

            _repo.Update(hs);
            _repo.Save();
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
            _repo.Save();
        }
    }
}

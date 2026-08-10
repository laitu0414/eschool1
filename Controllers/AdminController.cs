using eSchool.Infrastructure;
using eSchool.Services;
using eSchool.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace eSchool.Controllers
{
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IChucVuService _chucVuService;
        private readonly IThongBaoService _thongBaoService;
        private readonly AppDbContext _context;

        public AdminController(
            IAccountService accountService,
            IChucVuService chucVuService,
            IThongBaoService thongBaoService,
            AppDbContext context)
        {
            _accountService = accountService;
            _chucVuService = chucVuService;
            _thongBaoService = thongBaoService;
            _context = context;
        }

        public IActionResult Index()
        {
            var thongBaos = _thongBaoService.GetAll();

            ViewBag.SoTaiKhoan = _accountService.GetAll().Count;
            ViewBag.SoChucVu = _chucVuService.GetAll().Count;
            ViewBag.SoThongBao = thongBaos.Count;
            ViewBag.ThongBaoMoi = thongBaos.Take(3).ToList();

            // Tính thống kê tổng số tiền học phí đã thu (TrangThai == 1)
            var hocPhis = _context.HocPhis
                .Where(x => x.TrangThai == 1 && x.NamHoc != null && x.HocKyInfo != null)
                .Select(x => new { NamHoc = x.NamHoc.TenNamHoc, HocKy = x.HocKyInfo.TenHocKy, SoTien = x.SoTien })
                .ToList();

            var chartData = hocPhis
                .GroupBy(x => x.NamHoc)
                .Select(g => new {
                    NamHoc = g.Key,
                    HK1 = g.Where(x => x.HocKy.Contains("1")).Sum(x => x.SoTien),
                    HK2 = g.Where(x => x.HocKy.Contains("2")).Sum(x => x.SoTien)
                })
                .OrderByDescending(x => x.NamHoc)
                .ToList();

            ViewBag.ChartData = chartData;
            ViewBag.NamHocs = _context.NamHocs.OrderByDescending(x => x.TenNamHoc).ToList();

            return View();
        }
    }
}

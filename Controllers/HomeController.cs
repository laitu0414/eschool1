using System.Diagnostics;
using eSchool.Models;
using eSchool.ViewModels;
using eschool.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eschool.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(bool openLogin = false, bool openForgotPassword = false)
        {
            var vm = new TrangChuViewModel
            {
                OpenLogin = openLogin || TempData["LoginError"] != null || TempData["AuthSuccess"] != null,
                OpenForgotPassword = openForgotPassword || TempData["ForgotPasswordError"] != null,
                TinTucSuKiens = _context.TinTucSuKiens
                    .Where(x => x.TrangThai)
                    .OrderByDescending(x => x.NgayTao)
                    .Take(20)
                    .AsNoTracking()
                    .ToList(),
                TopTinTucHomNay = _context.TinTucSuKiens
                    .Where(x => x.TrangThai)
                    .OrderByDescending(x => x.LuotXem)
                    .Take(5)
                    .AsNoTracking()
                    .ToList(),
                ThongBaos = _context.ThongBaos
                    .Where(x => x.DoiTuongNhan == 0)
                    .OrderByDescending(x => x.NgayTao)
                    .Take(8)
                    .AsNoTracking()
                    .ToList()
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ThongDiepHieuTruong()
        {
            return View();
        }

        public IActionResult TamNhin()
        {
            return View();
        }

        public IActionResult DoiNguGiaoVien()
        {
            return View();
        }

        public IActionResult CoSoVatChat()
        {
            return View();
        }

        public IActionResult MoHinhDaoTao()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

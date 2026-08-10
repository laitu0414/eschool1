using eSchool.Infrastructure;
using eSchool.Services;
using Microsoft.AspNetCore.Mvc;

namespace eSchool.Controllers
{
    [AdminOnly]
    public class NhatKyController : Controller
    {
        private readonly INhatKyService _nhatKyService;

        public NhatKyController(INhatKyService nhatKyService)
        {
            _nhatKyService = nhatKyService;
        }

        public IActionResult Index()
        {
            return View(_nhatKyService.GetAll());
        }
    }
}

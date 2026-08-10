using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.Services;
using eSchool.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Controllers
{
    [AdminOnly]
    public class TinTucSuKienController : Controller
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly INhatKyService _nhatKyService;

        public TinTucSuKienController(
            AppDbContext context,
            IWebHostEnvironment environment,
            INhatKyService nhatKyService)
        {
            _context = context;
            _environment = environment;
            _nhatKyService = nhatKyService;
        }

        public IActionResult Index()
        {
            var data = _context.TinTucSuKiens
                .OrderByDescending(x => x.NgayTao)
                .AsNoTracking()
                .ToList();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TinTucSuKienFormViewModel vm)
        {
            Normalize(vm);
            ValidateForm(vm, true);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = FirstError() ?? "Thong tin tin tuc su kien chua hop le.";
                return RedirectToAction(nameof(Index));
            }

            var item = new TinTucSuKien
            {
                TieuDe = vm.TieuDe,
                MoTa = vm.MoTa,
                DuongDan = vm.DuongDan,
                AnhMinhHoa = await SaveImageAsync(vm.AnhTaiLen),
                TrangThai = vm.TrangThai,
                LoaiTin = vm.LoaiTin,
                NgayTao = DateTime.Now
            };

            _context.TinTucSuKiens.Add(item);
            _context.SaveChanges();

            WriteLog("Them tin tuc su kien", $"Da them tin tuc su kien {item.TieuDe}");
            TempData["Success"] = "Them tin tuc su kien thanh cong.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TinTucSuKienFormViewModel vm)
        {
            var existing = _context.TinTucSuKiens.Find(vm.IdTinTucSuKien);
            if (existing == null)
            {
                return NotFound();
            }

            Normalize(vm);
            ValidateForm(vm, false);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = FirstError() ?? "Thong tin tin tuc su kien chua hop le.";
                return RedirectToAction(nameof(Index));
            }

            var oldImage = existing.AnhMinhHoa;
            if (vm.AnhTaiLen is { Length: > 0 })
            {
                existing.AnhMinhHoa = await SaveImageAsync(vm.AnhTaiLen);
            }

            existing.TieuDe = vm.TieuDe;
            existing.MoTa = vm.MoTa;
            existing.DuongDan = vm.DuongDan;
            existing.TrangThai = vm.TrangThai;
            existing.LoaiTin = vm.LoaiTin;
            _context.SaveChanges();

            if (vm.AnhTaiLen is { Length: > 0 } && oldImage != existing.AnhMinhHoa)
            {
                DeleteImage(oldImage);
            }

            WriteLog("Sua tin tuc su kien", $"Da sua tin tuc su kien ID {existing.IdTinTucSuKien}");
            TempData["Success"] = "Cap nhat tin tuc su kien thanh cong.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var item = _context.TinTucSuKiens.Find(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.TinTucSuKiens.Remove(item);
            _context.SaveChanges();
            DeleteImage(item.AnhMinhHoa);

            WriteLog("Xoa tin tuc su kien", $"Da xoa tin tuc su kien {item.TieuDe}");
            TempData["Success"] = "Xoa tin tuc su kien thanh cong.";
            return RedirectToAction(nameof(Index));
        }

        private void Normalize(TinTucSuKienFormViewModel vm)
        {
            vm.TieuDe = vm.TieuDe?.Trim() ?? string.Empty;
            vm.MoTa = vm.MoTa?.Trim() ?? string.Empty;
            vm.DuongDan = vm.DuongDan?.Trim() ?? string.Empty;
        }

        private void ValidateForm(TinTucSuKienFormViewModel vm, bool requireImage)
        {
            if (requireImage && (vm.AnhTaiLen == null || vm.AnhTaiLen.Length == 0))
            {
                ModelState.AddModelError(nameof(vm.AnhTaiLen), "Vui long chon anh minh hoa.");
            }

            if (!Uri.TryCreate(vm.DuongDan, UriKind.Absolute, out _))
            {
                ModelState.AddModelError(nameof(vm.DuongDan), "Duong dan khong hop le.");
            }

            ValidateImage(vm.AnhTaiLen);
        }

        private void ValidateImage(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return;
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension) ||
                !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("AnhTaiLen", "Chi chap nhan anh JPG, PNG hoac WebP.");
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("AnhTaiLen", "Dung luong anh toi da la 5 MB.");
            }
        }

        private async Task<string?> SaveImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            var imageDirectory = Path.Combine(_environment.WebRootPath, "image");
            Directory.CreateDirectory(imageDirectory);

            var fileName = $"news_{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
            var fullPath = Path.Combine(imageDirectory, fileName);

            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await image.CopyToAsync(stream);
            return $"/image/{fileName}";
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) ||
                !imagePath.StartsWith("/image/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fullPath = Path.Combine(_environment.WebRootPath, "image", Path.GetFileName(imagePath));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        private string? FirstError()
        {
            return ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
        }

        private void WriteLog(string action, string content)
        {
            _nhatKyService.GhiLog(
                HttpContext.Session.GetString("Username") ?? "Admin",
                action,
                content);
        }
    }
}

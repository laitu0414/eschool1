using eSchool.Infrastructure;
using eSchool.Models;
using eSchool.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eSchool.Controllers
{
    [RoleAuthorize(1)]
    public class HocVuController : Controller
    {
        private readonly AppDbContext _context;

        public HocVuController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult LopHoc(string? keyword)
        {
            var query = _context.LopHocs
                .Include(x => x.GiaoVienChuNhiem)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaLop.Contains(keyword) ||
                    x.TenLop.Contains(keyword) ||
                    (x.Khoi != null && x.Khoi.Contains(keyword)));
            }

            ViewBag.Keyword = keyword;
            ViewBag.GiaoViens = GetGiaoVienSelectList();
            ViewBag.NamHocs = GetNamHocSelectList();
            ViewBag.PhongHocs = _context.PhongHocs.Select(x => new SelectListItem(x.TenPhong, x.IdPhongHoc.ToString())).ToList();
            
            var phongHocMap = _context.PhongHocs.Where(x => x.IdLop != null).ToDictionary(x => x.IdLop.Value, x => x.IdPhongHoc);
            ViewBag.PhongHocMap = phongHocMap;

            return View(query.OrderBy(x => x.TenLop).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoLop(LopHocFormViewModel vm)
        {
            NormalizeLop(vm);

            if (!ModelState.IsValid)
                return RedirectWithError(nameof(LopHoc), "Thông tin lớp học chưa hợp lệ.");

            if (_context.LopHocs.Any(x => x.MaLop == vm.MaLop))
                return RedirectWithError(nameof(LopHoc), "Mã lớp đã tồn tại.");

            var newLop = new LopHoc
            {
                MaLop = vm.MaLop.Trim(),
                TenLop = vm.TenLop.Trim(),
                Khoi = vm.Khoi,
                BuoiHoc = vm.BuoiHoc,
                NamHoc = vm.NamHoc,
                IdGiaoVienCN = vm.IdGiaoVienCN
            };
            
            _context.LopHocs.Add(newLop);
            _context.SaveChanges();

            if (vm.IdPhongHoc.HasValue)
            {
                var phong = _context.PhongHocs.Find(vm.IdPhongHoc.Value);
                if (phong != null)
                {
                    phong.IdLop = newLop.IdLop;
                    _context.SaveChanges();
                }
            }

            AutoGenerateSchedule(newLop);

            return RedirectWithSuccess(nameof(LopHoc), "Đã thêm lớp học và tự động xếp thời khóa biểu.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaLop(LopHocFormViewModel vm)
        {
            var lop = _context.LopHocs.Find(vm.IdLop);
            if (lop == null) return NotFound();

            NormalizeLop(vm);

            if (!ModelState.IsValid)
                return RedirectWithError(nameof(LopHoc), "Thông tin lớp học chưa hợp lệ.");

            if (_context.LopHocs.Any(x => x.MaLop == vm.MaLop && x.IdLop != vm.IdLop))
                return RedirectWithError(nameof(LopHoc), "Mã lớp đã tồn tại.");

            lop.MaLop = vm.MaLop.Trim();
            lop.TenLop = vm.TenLop.Trim();
            lop.Khoi = vm.Khoi;
            lop.BuoiHoc = vm.BuoiHoc;
            lop.NamHoc = vm.NamHoc;
            lop.IdGiaoVienCN = vm.IdGiaoVienCN;
            
            var existingPhong = _context.PhongHocs.FirstOrDefault(x => x.IdLop == lop.IdLop);
            if (existingPhong != null && existingPhong.IdPhongHoc != vm.IdPhongHoc)
            {
                existingPhong.IdLop = null;
            }
            if (vm.IdPhongHoc.HasValue)
            {
                var newPhong = _context.PhongHocs.Find(vm.IdPhongHoc.Value);
                if (newPhong != null)
                {
                    newPhong.IdLop = lop.IdLop;
                }
            }

            _context.SaveChanges();
            return RedirectWithSuccess(nameof(LopHoc), "Đã cập nhật lớp học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaLop(int id)
        {
            var lop = _context.LopHocs.Find(id);
            if (lop == null) return NotFound();

            var dangSuDung = _context.HocSinhs.Any(x => x.IdLopHoc == id)
                || _context.PhanCongGiangDays.Any(x => x.IdLop == id)
                || _context.DangKyLops.Any(x => x.IdLop == id)
                || _context.DiemDanhs.Any(x => x.IdLop == id);

            if (dangSuDung)
                return RedirectWithError(nameof(LopHoc), "Không thể xóa lớp đang có dữ liệu liên quan.");

            _context.LopHocs.Remove(lop);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(LopHoc), "Đã xóa lớp học.");
        }

        // --- QUẢN LÝ PHÒNG HỌC ---
        public IActionResult PhongHoc(string? keyword, string? loaiPhong, bool? trangThai)
        {
            var query = _context.PhongHocs.Include(x => x.LopHoc).AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x => x.MaPhong.Contains(keyword) || x.TenPhong.Contains(keyword) || (x.TrangThietBi != null && x.TrangThietBi.Contains(keyword)));
            }
            if (!string.IsNullOrWhiteSpace(loaiPhong))
            {
                query = query.Where(x => x.LoaiPhong == loaiPhong);
            }
            if (trangThai.HasValue)
            {
                query = query.Where(x => x.TrangThai == trangThai.Value);
            }

            ViewBag.Keyword = keyword;
            ViewBag.FilterLoaiPhong = loaiPhong;
            ViewBag.FilterTrangThai = trangThai;
            ViewBag.LopHocs = _context.LopHocs.OrderBy(x => x.TenLop).Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString())).ToList();
            
            // Dashboard stats
            int totalPhong = _context.PhongHocs.Count();
            int phongHoatDong = _context.PhongHocs.Count(x => x.TrangThai);
            
            int totalTB = _context.ThietBis.Count();
            int tbTot = _context.ThietBis.Count(x => x.TinhTrang == "Tốt");
            
            int dangBaoTri = _context.PhongHocs.Count(x => !x.TrangThai);
            int quaHan = _context.BaoTris.Count(x => x.KetQua != "Hoàn thành" && x.NgayBaoTri < DateTime.Now.Date);
            
            ViewBag.TotalPhong = totalPhong;
            ViewBag.PhongHoatDong = phongHoatDong;
            ViewBag.TotalTB = totalTB;
            ViewBag.TBTot = tbTot;
            ViewBag.DangBaoTri = dangBaoTri;
            ViewBag.QuaHan = quaHan;

            ViewBag.ThietBis = _context.ThietBis.Select(x => new SelectListItem { Value = x.IdThietBi.ToString(), Text = x.MaTB + " - " + x.TenTB }).ToList();
            ViewBag.BaoTris = _context.BaoTris.Include(x => x.ThietBi).OrderByDescending(x => x.NgayBaoTri).ToList();

            return View(query.OrderBy(x => x.MaPhong).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoPhong(PhongHoc model)
        {
            if (string.IsNullOrWhiteSpace(model.MaPhong) || string.IsNullOrWhiteSpace(model.TenPhong))
                return RedirectWithError(nameof(PhongHoc), "Mã phòng và tên phòng không được để trống.");

            if (_context.PhongHocs.Any(x => x.MaPhong == model.MaPhong.Trim()))
                return RedirectWithError(nameof(PhongHoc), "Mã phòng đã tồn tại.");

            model.MaPhong = model.MaPhong.Trim();
            model.TenPhong = model.TenPhong.Trim();
            
            _context.PhongHocs.Add(model);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(PhongHoc), "Đã thêm phòng học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaPhong(PhongHoc model)
        {
            var phong = _context.PhongHocs.Find(model.IdPhongHoc);
            if (phong == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.MaPhong) || string.IsNullOrWhiteSpace(model.TenPhong))
                return RedirectWithError(nameof(PhongHoc), "Mã phòng và tên phòng không được để trống.");

            if (_context.PhongHocs.Any(x => x.MaPhong == model.MaPhong.Trim() && x.IdPhongHoc != model.IdPhongHoc))
                return RedirectWithError(nameof(PhongHoc), "Mã phòng đã tồn tại.");

            bool isChangingToBaoTri = phong.TrangThai && !model.TrangThai;
            int? oldLopId = phong.IdLop;

            phong.MaPhong = model.MaPhong.Trim();
            phong.TenPhong = model.TenPhong.Trim();
            phong.TrangThai = model.TrangThai;
            phong.IdLop = model.IdLop;

            if (isChangingToBaoTri && oldLopId.HasValue)
            {
                var lopHienTai = _context.LopHocs.Find(oldLopId.Value);
                if (lopHienTai != null)
                {
                    bool isMorning = lopHienTai.BuoiHoc == "Sáng" || (string.IsNullOrEmpty(lopHienTai.BuoiHoc) && lopHienTai.Khoi != null && (lopHienTai.Khoi.Contains("6") || lopHienTai.Khoi.Contains("7")));
                    
                    var allPhongs = _context.PhongHocs.Include(x => x.LopHoc).Where(x => x.TrangThai && x.LopHoc != null).ToList();
                    var phongThayThe = allPhongs.FirstOrDefault(x => {
                        bool isRoomMorning = x.LopHoc.BuoiHoc == "Sáng" || (string.IsNullOrEmpty(x.LopHoc.BuoiHoc) && x.LopHoc.Khoi != null && (x.LopHoc.Khoi.Contains("6") || x.LopHoc.Khoi.Contains("7")));
                        return isMorning != isRoomMorning;
                    });

                    if (phongThayThe != null)
                    {
                        var thayDoi = new LichHocThayDoi
                        {
                            Ngay = DateTime.Today,
                            IdLop = oldLopId.Value,
                            IsNghi = false,
                            GhiChu = "Đổi phòng: " + phongThayThe.MaPhong
                        };
                        _context.LichHocThayDois.Add(thayDoi);
                        
                        TempData["Success"] = $"Đã chuyển lớp {lopHienTai.TenLop} sang phòng {phongThayThe.MaPhong} trong ngày {DateTime.Today.ToString("dd/MM/yyyy")}.";
                    }
                    else
                    {
                        TempData["Warning"] = $"Phòng đã chuyển sang bảo trì nhưng không tìm thấy phòng trống của buổi học ngược lại để chuyển cho lớp {lopHienTai.TenLop}.";
                    }
                }
            }

            _context.SaveChanges();
            return RedirectWithSuccess(nameof(PhongHoc), "Đã cập nhật phòng học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaPhong(int id)
        {
            var phong = _context.PhongHocs.Find(id);
            if (phong == null) return NotFound();

            _context.PhongHocs.Remove(phong);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(PhongHoc), "Đã xóa phòng học.");
        }

        public IActionResult ThietBi(int phongId)
        {
            var phong = _context.PhongHocs.Find(phongId);
            if (phong == null) return NotFound();

            var thietBis = _context.ThietBis.Where(x => x.IdPhongHoc == phongId).OrderBy(x => x.MaTB).ToList();
            var thietBiIds = thietBis.Select(x => x.IdThietBi).ToList();
            var baoTris = _context.BaoTris.Include(x => x.ThietBi).Where(x => thietBiIds.Contains(x.IdThietBi)).OrderByDescending(x => x.NgayBaoTri).ToList();
            
            ViewBag.PhongHoc = phong;
            ViewBag.BaoTris = baoTris;
            ViewBag.LopHocs = _context.LopHocs.OrderBy(x => x.TenLop).Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString())).ToList();
            ViewBag.ThietBis = thietBis.Select(x => new SelectListItem { Value = x.IdThietBi.ToString(), Text = x.MaTB + " - " + x.TenTB }).ToList();
            return View(thietBis);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoThietBi(ThietBi model)
        {
            if (string.IsNullOrWhiteSpace(model.MaTB) || string.IsNullOrWhiteSpace(model.TenTB))
                return RedirectWithError(nameof(ThietBi), "Mã TB và Tên TB không được để trống.", new { phongId = model.IdPhongHoc });

            if (_context.ThietBis.Any(x => x.MaTB == model.MaTB.Trim()))
                return RedirectWithError(nameof(ThietBi), "Mã thiết bị đã tồn tại.", new { phongId = model.IdPhongHoc });

            model.MaTB = model.MaTB.Trim();
            model.TenTB = model.TenTB.Trim();
            
            _context.ThietBis.Add(model);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(ThietBi), "Đã thêm thiết bị.", new { phongId = model.IdPhongHoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaThietBi(ThietBi model)
        {
            var tb = _context.ThietBis.Find(model.IdThietBi);
            if (tb == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.MaTB) || string.IsNullOrWhiteSpace(model.TenTB))
                return RedirectWithError(nameof(ThietBi), "Mã TB và Tên TB không được để trống.", new { phongId = tb.IdPhongHoc });

            if (_context.ThietBis.Any(x => x.MaTB == model.MaTB.Trim() && x.IdThietBi != model.IdThietBi))
                return RedirectWithError(nameof(ThietBi), "Mã thiết bị đã tồn tại.", new { phongId = tb.IdPhongHoc });

            tb.MaTB = model.MaTB.Trim();
            tb.TenTB = model.TenTB.Trim();
            tb.LoaiTB = model.LoaiTB;
            tb.SoLuong = model.SoLuong;
            tb.TinhTrang = model.TinhTrang;
            tb.NgayMua = model.NgayMua;

            _context.SaveChanges();
            return RedirectWithSuccess(nameof(ThietBi), "Đã cập nhật thiết bị.", new { phongId = tb.IdPhongHoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaThietBi(int id)
        {
            var tb = _context.ThietBis.Find(id);
            if (tb == null) return NotFound();

            int phongId = tb.IdPhongHoc;
            _context.ThietBis.Remove(tb);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(ThietBi), "Đã xóa thiết bị.", new { phongId = phongId });
        }

        // --- QUẢN LÝ BẢO TRÌ ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoBaoTri(BaoTri model)
        {
            if (!ModelState.IsValid)
                return RedirectWithError(nameof(PhongHoc), "Thông tin chưa hợp lệ.");
            
            if (string.IsNullOrEmpty(model.TrangThai))
                model.TrangThai = "Đang xử lý";
                
            model.NguoiThucHien = string.IsNullOrWhiteSpace(model.NguoiThucHien) 
                ? HttpContext.Session.GetString("Username") ?? "" 
                : model.NguoiThucHien;
            model.KetQua = model.KetQua ?? "";
                
            _context.BaoTris.Add(model);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(PhongHoc), "Đã thêm phiếu bảo trì.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaBaoTri(BaoTri model)
        {
            var bt = _context.BaoTris.Find(model.IdBaoTri);
            if (bt == null) return NotFound();

            bt.MaBaoTri = model.MaBaoTri;
            bt.IdThietBi = model.IdThietBi;
            bt.NgayBaoTri = model.NgayBaoTri;
            bt.NoiDung = model.NoiDung ?? "";
            bt.ChiPhi = model.ChiPhi;
            
            bt.NguoiThucHien = string.IsNullOrWhiteSpace(model.NguoiThucHien) 
                ? HttpContext.Session.GetString("Username") ?? "" 
                : model.NguoiThucHien;
                
            bt.KetQua = model.KetQua ?? "";
            
            // Set TrangThai based on KetQua or submitted form (Admin uses KetQua mostly)
            if (model.KetQua == "Hoàn thành")
            {
                bt.TrangThai = "Hoàn thành";
                var tb = _context.ThietBis.Find(bt.IdThietBi);
                if (tb != null) tb.TinhTrang = "Tốt";
            }
            else
            {
                bt.TrangThai = "Đang xử lý";
                var tb = _context.ThietBis.Find(bt.IdThietBi);
                if (tb != null) tb.TinhTrang = "Đang sửa";
            }
            
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(PhongHoc), "Đã cập nhật phiếu bảo trì.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaBaoTri(int id)
        {
            var bt = _context.BaoTris.Find(id);
            if (bt != null)
            {
                _context.BaoTris.Remove(bt);
                _context.SaveChanges();
            }
            return RedirectWithSuccess(nameof(PhongHoc), "Đã xóa phiếu bảo trì.");
        }

        // --- QUẢN LÝ MÔN HỌC ---

        public IActionResult MonHoc(string? keyword)
        {
            var query = _context.MonHocs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x => x.MaMon.Contains(keyword) || x.TenMon.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(query.OrderBy(x => x.TenMon).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoMon(MonHocFormViewModel vm)
        {
            NormalizeMon(vm);

            if (!ModelState.IsValid)
                return RedirectWithError(nameof(MonHoc), "Thông tin môn học chưa hợp lệ.");

            if (_context.MonHocs.Any(x => x.MaMon == vm.MaMon))
                return RedirectWithError(nameof(MonHoc), "Mã môn đã tồn tại.");

            _context.MonHocs.Add(new MonHoc
            {
                MaMon = vm.MaMon.Trim(),
                TenMon = vm.TenMon.Trim(),
                SoTiet = vm.SoTiet
            });
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(MonHoc), "Đã thêm môn học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaMon(MonHocFormViewModel vm)
        {
            var mon = _context.MonHocs.Find(vm.IdMonHoc);
            if (mon == null) return NotFound();

            NormalizeMon(vm);

            if (!ModelState.IsValid)
                return RedirectWithError(nameof(MonHoc), "Thông tin môn học chưa hợp lệ.");

            if (_context.MonHocs.Any(x => x.MaMon == vm.MaMon && x.IdMonHoc != vm.IdMonHoc))
                return RedirectWithError(nameof(MonHoc), "Mã môn đã tồn tại.");

            int oldSoTiet = mon.SoTiet;
            int diff = vm.SoTiet - oldSoTiet;

            mon.MaMon = vm.MaMon.Trim();
            mon.TenMon = vm.TenMon.Trim();
            mon.SoTiet = vm.SoTiet;

            if (diff != 0)
            {
                var affectedClasses = _context.PhanCongGiangDays
                    .Where(x => x.IdMonHoc == vm.IdMonHoc)
                    .Select(x => x.IdLop)
                    .Distinct()
                    .ToList();

                foreach (var lopId in affectedClasses)
                {
                    if (diff < 0)
                    {
                        var periodsToRemove = _context.PhanCongGiangDays
                            .Where(x => x.IdMonHoc == vm.IdMonHoc && x.IdLop == lopId)
                            .OrderByDescending(x => x.Thu).ThenByDescending(x => x.TietBatDau)
                            .Take(-diff)
                            .ToList();
                        _context.PhanCongGiangDays.RemoveRange(periodsToRemove);
                    }
                    else if (diff > 0)
                    {
                        var existingLopSchedules = _context.PhanCongGiangDays.Where(x => x.IdLop == lopId).ToList();
                        var lop = _context.LopHocs.Find(lopId);
                        if (lop == null) continue;

                        var classGrid = new bool[8, 6];
                        foreach (var s in existingLopSchedules)
                        {
                            if (s.Thu.HasValue && s.TietBatDau.HasValue)
                            {
                                for (int i = 0; i < (s.SoTiet ?? 1); i++)
                                {
                                    if (s.Thu.Value <= 7 && s.TietBatDau.Value + i <= 5)
                                        classGrid[s.Thu.Value, s.TietBatDau.Value + i] = true;
                                }
                            }
                        }

                        var currentTeacherId = existingLopSchedules.FirstOrDefault(x => x.IdMonHoc == vm.IdMonHoc && x.IdGiaoVien != null)?.IdGiaoVien;

                        List<PhanCongGiangDay> teacherSchedules = new List<PhanCongGiangDay>();
                        if (currentTeacherId.HasValue)
                        {
                            teacherSchedules = _context.PhanCongGiangDays.Include(x => x.LopHoc).Where(x => x.IdGiaoVien == currentTeacherId.Value && x.NamHoc == lop.NamHoc).ToList();
                        }

                        int added = 0;
                        // Pass 1: Try to assign the teacher if they are free
                        for (int thu = 2; thu <= 7 && added < diff; thu++)
                        {
                            for (int tiet = 1; tiet <= 5 && added < diff; tiet++)
                            {
                                if (!classGrid[thu, tiet])
                                {
                                    if (currentTeacherId.HasValue)
                                    {
                                        var teacherBusy = teacherSchedules.Any(s => s.Thu == thu && s.TietBatDau.HasValue && s.LopHoc?.BuoiHoc == lop.BuoiHoc && tiet >= s.TietBatDau.Value && tiet < s.TietBatDau.Value + (s.SoTiet ?? 1));
                                        var teacherPeriodsDay = teacherSchedules.Where(s => s.Thu == thu).Sum(s => s.SoTiet ?? 1);
                                        var teacherPeriodsWeek = teacherSchedules.Sum(s => s.SoTiet ?? 1);
                                        
                                        if (!teacherBusy && teacherPeriodsDay < 5 && teacherPeriodsWeek < 19)
                                        {
                                            classGrid[thu, tiet] = true;
                                            var newSch = new PhanCongGiangDay
                                            {
                                                IdGiaoVien = currentTeacherId,
                                                IdMonHoc = vm.IdMonHoc,
                                                IdLop = lopId,
                                                NamHoc = lop.NamHoc,
                                                HocKy = "Cả năm",
                                                Thu = thu,
                                                TietBatDau = tiet,
                                                SoTiet = 1,
                                                LopHoc = lop
                                            };
                                            _context.PhanCongGiangDays.Add(newSch);
                                            teacherSchedules.Add(newSch);
                                            added++;
                                        }
                                    }
                                }
                            }
                        }

                        // Pass 2: Fill remaining required slots, but leave teacher blank
                        for (int thu = 2; thu <= 7 && added < diff; thu++)
                        {
                            for (int tiet = 1; tiet <= 5 && added < diff; tiet++)
                            {
                                if (!classGrid[thu, tiet])
                                {
                                    classGrid[thu, tiet] = true;
                                    _context.PhanCongGiangDays.Add(new PhanCongGiangDay
                                    {
                                        IdGiaoVien = null,
                                        IdMonHoc = vm.IdMonHoc,
                                        IdLop = lopId,
                                        NamHoc = lop.NamHoc,
                                        HocKy = "Cả năm",
                                        Thu = thu,
                                        TietBatDau = tiet,
                                        SoTiet = 1
                                    });
                                    added++;
                                }
                            }
                        }
                    }
                }
            }

            _context.SaveChanges();
            return RedirectWithSuccess(nameof(MonHoc), "Đã cập nhật môn học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaMon(int id)
        {
            var mon = _context.MonHocs.Find(id);
            if (mon == null) return NotFound();

            if (_context.PhanCongGiangDays.Any(x => x.IdMonHoc == id) ||
                _context.Diems.Any(x => x.IdMonHoc == id))
            {
                return RedirectWithError(nameof(MonHoc), "Không thể xóa môn đang có dữ liệu liên quan.");
            }

            _context.MonHocs.Remove(mon);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(MonHoc), "Đã xóa môn học.");
        }

        public IActionResult ThoiKhoaBieu(string? namHoc, string? hocKy, int? lopId, DateTime? tuan)
        {
            var selectedDate = tuan;
            if (!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }
            
            var vm = BuildThoiKhoaBieuViewModel();
            var query = _context.PhanCongGiangDays
                .Include(x => x.GiaoVien)
                .Include(x => x.MonHoc)
                .Include(x => x.LopHoc)
                .Include(x => x.PhongHoc)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(namHoc) && !string.IsNullOrWhiteSpace(hocKy) && lopId.HasValue)
            {
                query = query.Where(x => x.NamHoc == namHoc && (x.HocKy == hocKy || x.HocKy == "Cả năm") && x.IdLop == lopId.Value);
                vm.DanhSach = query.OrderBy(x => x.Thu).ThenBy(x => x.TietBatDau).ToList();
            }
            else
            {
                vm.DanhSach = new List<PhanCongGiangDay>();
            }
            
            var diff = (7 + (selectedDate.Value.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = selectedDate.Value.AddDays(-diff);
            var endDate = startOfWeek.AddDays(6);
            var thayDois = _context.LichHocThayDois
                .Include(x => x.MonHocThayThe)
                .Include(x => x.GiaoVienThayThe)
                .Where(x => x.Ngay >= startOfWeek && x.Ngay <= endDate)
                .ToList();

            ViewBag.FilterNamHoc = namHoc;
            ViewBag.FilterHocKy = hocKy;
            ViewBag.FilterLopId = lopId;
            ViewBag.Tuan = selectedDate.Value.ToString("yyyy-MM-dd");
            ViewBag.StartOfWeek = startOfWeek.ToString("yyyy-MM-dd");
            ViewBag.ThayDois = thayDois;
            
            var phongHocMap = _context.PhongHocs.Where(x => x.IdLop != null).ToDictionary(x => x.IdLop.Value, x => x.MaPhong);
            ViewBag.PhongHocMap = phongHocMap;
            
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleNgayNghi(DateTime ngay, int? lopId)
        {
            var existing = _context.LichHocThayDois.FirstOrDefault(x => x.Ngay.Date == ngay.Date && x.IdLop == lopId);
            if (existing != null)
            {
                existing.IsNghi = !existing.IsNghi;
            }
            else
            {
                _context.LichHocThayDois.Add(new LichHocThayDoi
                {
                    Ngay = ngay.Date,
                    IdLop = lopId,
                    IsNghi = true
                });
            }
            _context.SaveChanges();
            
            return RedirectToAction("ThoiKhoaBieu", new { lopId = lopId, tuan = ngay.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemLichNghiNangCao(string doiTuong, string? khoi, int? lopId, string kieuNghi, string? buoi, DateTime ngayNghi, List<int>? tiets, string lyDo, string? ghiChu)
        {
            var lops = new List<int?>();

            if (doiTuong == "ToanTruong")
            {
                lops.Add(null);
            }
            else if (doiTuong == "Khoi" && !string.IsNullOrEmpty(khoi))
            {
                var lopHocs = _context.LopHocs.Where(x => x.Khoi == khoi).Select(x => x.IdLop).ToList();
                foreach (var l in lopHocs) lops.Add(l);
            }
            else if (doiTuong == "Lop" && lopId.HasValue)
            {
                lops.Add(lopId.Value);
            }

            var reason = lyDo + (string.IsNullOrEmpty(ghiChu) ? "" : $" ({ghiChu})");

            foreach (var lId in lops)
            {
                if (kieuNghi == "CaNgay")
                {
                    var thayDoi = new LichHocThayDoi
                    {
                        Ngay = ngayNghi.Date,
                        IdLop = lId,
                        IsNghi = true,
                        GhiChu = reason
                    };
                    _context.LichHocThayDois.Add(thayDoi);
                }
                else if (kieuNghi == "TheoBuoi" && !string.IsNullOrEmpty(buoi))
                {
                    // Logic to add specific periods could go here if the model supports it.
                    // Currently, the view simply checks if there's a holiday for the whole day.
                    // Since the current model doesn't fully support period-specific holidays cleanly in the view,
                    // we'll store it but the view may need adjustments to respect it.
                    var thayDoi = new LichHocThayDoi
                    {
                        Ngay = ngayNghi.Date,
                        IdLop = lId,
                        IsNghi = true,
                        GhiChu = $"{reason} [Nghỉ buổi {buoi}]"
                    };
                    _context.LichHocThayDois.Add(thayDoi);
                }
                else if (kieuNghi == "TheoTiet" && tiets != null && tiets.Any())
                {
                    foreach (var tiet in tiets)
                    {
                        var thayDoi = new LichHocThayDoi
                        {
                            Ngay = ngayNghi.Date,
                            IdLop = lId,
                            TietBatDau = tiet,
                            SoTiet = 1,
                            IsNghi = true,
                            GhiChu = $"{reason} [Nghỉ tiết {tiet}]"
                        };
                        _context.LichHocThayDois.Add(thayDoi);
                    }
                }
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã áp dụng lịch nghỉ.";

            return RedirectToAction("ThoiKhoaBieu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoLich(ThoiKhoaBieuHocVuViewModel vm)
        {
            if (!ModelState.IsValid)
                return RedirectWithError(nameof(ThoiKhoaBieu), "Thông tin thời khóa biểu chưa hợp lệ.");

            var tietCuoi = vm.TietBatDau + vm.SoTiet - 1;
            var trungLich = _context.PhanCongGiangDays.Any(x =>
                (x.IdGiaoVien == vm.IdGiaoVien || x.IdLop == vm.IdLop) &&
                x.NamHoc == vm.NamHoc &&
                x.Thu == vm.Thu &&
                x.TietBatDau.HasValue &&
                x.SoTiet.HasValue &&
                vm.TietBatDau <= x.TietBatDau.Value + x.SoTiet.Value - 1 &&
                tietCuoi >= x.TietBatDau.Value);

            if (trungLich)
                return RedirectWithError(nameof(ThoiKhoaBieu), "Giáo viên hoặc lớp đã có lịch trùng tiết.");

            if (vm.IdGiaoVien > 0)
            {
                var schedules = _context.PhanCongGiangDays
                    .Where(x => x.IdGiaoVien == vm.IdGiaoVien && x.NamHoc == vm.NamHoc && x.HocKy == vm.HocKy)
                    .ToList();
                    
                var totalWeek = schedules.Sum(x => x.SoTiet) ?? 0;
                if (totalWeek + vm.SoTiet > 19)
                {
                    return RedirectWithError(nameof(ThoiKhoaBieu), $"Giáo viên này đã có {totalWeek} tiết trong tuần. Không thể xếp vượt quá 19 tiết/tuần.");
                }

                var totalDay = schedules.Where(x => x.Thu == vm.Thu).Sum(x => x.SoTiet) ?? 0;
                if (totalDay + vm.SoTiet > 5)
                {
                    return RedirectWithError(nameof(ThoiKhoaBieu), $"Giáo viên này đã có {totalDay} tiết dạy trong ngày Thứ {vm.Thu}. Không thể xếp vượt quá 5 tiết/ngày.");
                }
            }

            _context.PhanCongGiangDays.Add(new PhanCongGiangDay
            {
                IdGiaoVien = vm.IdGiaoVien,
                IdMonHoc = vm.IdMonHoc,
                IdLop = vm.IdLop,
                NamHoc = vm.NamHoc,
                HocKy = "Cả năm",
                Thu = vm.Thu,
                TietBatDau = vm.TietBatDau,
                SoTiet = vm.SoTiet
            });
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(ThoiKhoaBieu), "Đã thêm lịch học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaLich(int id)
        {
            var lich = _context.PhanCongGiangDays.Find(id);
            if (lich == null) return NotFound();

            _context.PhanCongGiangDays.Remove(lich);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(ThoiKhoaBieu), "Đã xóa lịch học.");
        }

        public IActionResult NamHoc()
        {
            return View(_context.NamHocs
                .Include(x => x.HocKys)
                .OrderByDescending(x => x.NgayBatDau)
                .ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoNamHoc(NamHoc model)
        {
            if (!ModelState.IsValid || model.NgayKetThuc <= model.NgayBatDau)
                return RedirectWithError(nameof(NamHoc), "Thông tin năm học chưa hợp lệ.");

            if (_context.NamHocs.Any(x => x.TenNamHoc == model.TenNamHoc))
                return RedirectWithError(nameof(NamHoc), "Năm học đã tồn tại.");

            var newNamHoc = new NamHoc
            {
                TenNamHoc = model.TenNamHoc.Trim(),
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc,
                TrangThai = model.TrangThai
            };
            
            _context.NamHocs.Add(newNamHoc);
            _context.SaveChanges();

            // Tự động tạo 2 học kỳ nếu đúng định dạng (VD: 2026-2027)
            var match = System.Text.RegularExpressions.Regex.Match(newNamHoc.TenNamHoc, @"^(\d{4})-(\d{4})$");
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int startYear) && int.TryParse(match.Groups[2].Value, out int endYear))
                {
                    _context.HocKys.Add(new HocKy
                    {
                        IdNamHoc = newNamHoc.IdNamHoc,
                        TenHocKy = "Học kỳ 1",
                        NgayBatDau = new DateTime(startYear, 9, 5),
                        NgayKetThuc = new DateTime(startYear, 12, 31),
                        TrangThai = true
                    });

                    _context.HocKys.Add(new HocKy
                    {
                        IdNamHoc = newNamHoc.IdNamHoc,
                        TenHocKy = "Học kỳ 2",
                        NgayBatDau = new DateTime(endYear, 1, 1),
                        NgayKetThuc = new DateTime(endYear, 5, 31),
                        TrangThai = true
                    });

                    _context.SaveChanges();
                }
            }

            return RedirectWithSuccess(nameof(NamHoc), "Đã thêm năm học và tự động tạo 2 học kỳ.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaNamHoc(NamHoc model)
        {
            var namHoc = _context.NamHocs.Find(model.IdNamHoc);
            if (namHoc == null) return NotFound();

            if (!ModelState.IsValid || model.NgayKetThuc <= model.NgayBatDau)
                return RedirectWithError(nameof(NamHoc), "Thông tin năm học chưa hợp lệ.");

            if (_context.NamHocs.Any(x => x.TenNamHoc == model.TenNamHoc && x.IdNamHoc != model.IdNamHoc))
                return RedirectWithError(nameof(NamHoc), "Năm học đã tồn tại.");

            namHoc.TenNamHoc = model.TenNamHoc.Trim();
            namHoc.NgayBatDau = model.NgayBatDau;
            namHoc.NgayKetThuc = model.NgayKetThuc;
            namHoc.TrangThai = model.TrangThai;
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(NamHoc), "Đã cập nhật năm học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaNamHoc(int id)
        {
            var namHoc = _context.NamHocs.Include(x => x.HocKys).FirstOrDefault(x => x.IdNamHoc == id);
            if (namHoc == null) return NotFound();

            var dangSuDung = namHoc.HocKys?.Any() == true
                || _context.LopHocs.Any(x => x.NamHoc == namHoc.TenNamHoc)
                || _context.PhanCongGiangDays.Any(x => x.NamHoc == namHoc.TenNamHoc);

            if (dangSuDung)
                return RedirectWithError(nameof(NamHoc), "Không thể xóa năm học đang được sử dụng.");

            _context.NamHocs.Remove(namHoc);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(NamHoc), "Đã xóa năm học.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TongKetNamHoc(int idNamHoc)
        {
            var namHoc = _context.NamHocs.Find(idNamHoc);
            if (namHoc == null) return NotFound();

            var hocSinhs = _context.HocSinhs
                .Include(x => x.LopHoc)
                .Where(x => x.TrangThai == true && x.IdLopHoc != null)
                .ToList();

            var diems = _context.Diems
                .Where(x => x.IdNamHoc == idNamHoc && x.DiemTB.HasValue)
                .ToList();

            int passCount = 0;
            int failCount = 0;
            int gradCount = 0;

            foreach (var hs in hocSinhs)
            {
                var hsDiems = diems.Where(x => x.IdHocSinh == hs.IdHocSinh).ToList();
                
                var diemsHK1 = hsDiems.Where(x => x.HocKy == "Học kỳ 1" || x.HocKyInfo?.TenHocKy == "Học kỳ 1").ToList();
                var diemsHK2 = hsDiems.Where(x => x.HocKy == "Học kỳ 2" || x.HocKyInfo?.TenHocKy == "Học kỳ 2").ToList();

                decimal avg1 = diemsHK1.Any() ? diemsHK1.Average(x => x.DiemTB.Value) : 0;
                decimal avg2 = diemsHK2.Any() ? diemsHK2.Average(x => x.DiemTB.Value) : 0;

                decimal avg = 0;
                if (diemsHK1.Any() && diemsHK2.Any())
                {
                    // If both semesters have grades, calculate (HK1 + HK2 * 2) / 3
                    avg = (avg1 + avg2 * 2) / 3;
                }
                else if (diemsHK1.Any())
                {
                    avg = avg1;
                }
                else if (diemsHK2.Any())
                {
                    avg = avg2;
                }
                else
                {
                    // Fallback to simple average if no semester info matches
                    avg = hsDiems.Any() ? hsDiems.Average(x => x.DiemTB.Value) : 0;
                }
                
                if (avg >= 5.0m)
                {
                    var currentTenLop = hs.LopHoc?.TenLop ?? "";
                    var match = System.Text.RegularExpressions.Regex.Match(currentTenLop, @"^(\d+)(.*)$");
                    if (match.Success)
                    {
                        int currentGrade = int.Parse(match.Groups[1].Value);
                        int nextGrade = currentGrade + 1;

                        if (nextGrade > 12)
                        {
                            _context.ChuyenLops.Add(new ChuyenLop
                            {
                                IdHocSinh = hs.IdHocSinh,
                                IdLopCu = hs.IdLopHoc.Value,
                                IdLopMoi = hs.IdLopHoc.Value,
                                NgayChuyen = DateTime.Now,
                                LyDo = "Tốt nghiệp",
                                GhiChu = $"Học sinh {hs.HoTen} tốt nghiệp ra trường (ĐTB: {Math.Round(avg, 2)})"
                            });
                            hs.TrangThai = false;
                            gradCount++;
                        }
                        else
                        {
                            var nextTenLop = $"{nextGrade}{match.Groups[2].Value}";
                            var nextLop = _context.LopHocs.FirstOrDefault(x => x.TenLop.ToLower() == nextTenLop.ToLower());
                            if (nextLop == null)
                            {
                                nextLop = new LopHoc
                                {
                                    MaLop = nextTenLop,
                                    TenLop = nextTenLop,
                                    Khoi = nextGrade.ToString(),
                                    NamHoc = namHoc.TenNamHoc
                                };
                                _context.LopHocs.Add(nextLop);
                                _context.SaveChanges();
                            }

                            _context.ChuyenLops.Add(new ChuyenLop
                            {
                                IdHocSinh = hs.IdHocSinh,
                                IdLopCu = hs.IdLopHoc.Value,
                                IdLopMoi = nextLop.IdLop,
                                NgayChuyen = DateTime.Now,
                                LyDo = "Lên lớp",
                                GhiChu = $"Học sinh {hs.HoTen} lên lớp {nextTenLop} (ĐTB: {Math.Round(avg, 2)})"
                            });
                            hs.IdLopHoc = nextLop.IdLop;
                            passCount++;
                        }
                    }
                }
                else
                {
                    _context.ChuyenLops.Add(new ChuyenLop
                    {
                        IdHocSinh = hs.IdHocSinh,
                        IdLopCu = hs.IdLopHoc.Value,
                        IdLopMoi = hs.IdLopHoc.Value,
                        NgayChuyen = DateTime.Now,
                        LyDo = "Ở lại lớp",
                        GhiChu = $"Học sinh {hs.HoTen} ở lại lớp {hs.LopHoc?.TenLop} (ĐTB: {Math.Round(avg, 2)})"
                    });
                    failCount++;
                }
            }
            
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(NamHoc), $"Tổng kết năm học hoàn tất. Lên lớp: {passCount}, Ở lại lớp: {failCount}, Tốt nghiệp: {gradCount}.");
        }

        public IActionResult HocKy()
        {
            var vm = new HocKyPageViewModel
            {
                DanhSach = _context.HocKys.Include(x => x.NamHoc)
                    .OrderByDescending(x => x.NamHoc!.NgayBatDau)
                    .ThenBy(x => x.NgayBatDau)
                    .ToList(),
                NamHocs = GetNamHocIdSelectList()
            };
            ViewBag.NamHocs = vm.NamHocs;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoHocKy(HocKy model)
        {
            if (!ValidateHocKy(model, out var error))
                return RedirectWithError(nameof(HocKy), error);

            if (_context.HocKys.Any(x => x.IdNamHoc == model.IdNamHoc && x.TenHocKy == model.TenHocKy))
                return RedirectWithError(nameof(HocKy), "Học kỳ đã tồn tại trong năm học.");

            _context.HocKys.Add(new HocKy
            {
                TenHocKy = model.TenHocKy.Trim(),
                IdNamHoc = model.IdNamHoc,
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc,
                TrangThai = model.TrangThai
            });
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(HocKy), "Đã thêm học kỳ.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaHocKy(HocKy model)
        {
            var hocKy = _context.HocKys.Find(model.IdHocKy);
            if (hocKy == null) return NotFound();

            if (!ValidateHocKy(model, out var error))
                return RedirectWithError(nameof(HocKy), error);

            if (_context.HocKys.Any(x =>
                x.IdNamHoc == model.IdNamHoc &&
                x.TenHocKy == model.TenHocKy &&
                x.IdHocKy != model.IdHocKy))
            {
                return RedirectWithError(nameof(HocKy), "Học kỳ đã tồn tại trong năm học.");
            }

            hocKy.TenHocKy = model.TenHocKy.Trim();
            hocKy.IdNamHoc = model.IdNamHoc;
            hocKy.NgayBatDau = model.NgayBatDau;
            hocKy.NgayKetThuc = model.NgayKetThuc;
            hocKy.TrangThai = model.TrangThai;
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(HocKy), "Đã cập nhật học kỳ.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaHocKy(int id)
        {
            var hocKy = _context.HocKys.Include(x => x.NamHoc).FirstOrDefault(x => x.IdHocKy == id);
            if (hocKy == null) return NotFound();

            if (_context.PhanCongGiangDays.Any(x =>
                x.HocKy == hocKy.TenHocKy &&
                x.NamHoc == hocKy.NamHoc!.TenNamHoc))
            {
                return RedirectWithError(nameof(HocKy), "Không thể xóa học kỳ đang được sử dụng.");
            }

            _context.HocKys.Remove(hocKy);
            _context.SaveChanges();
            return RedirectWithSuccess(nameof(HocKy), "Đã xóa học kỳ.");
        }

        private bool ValidateHocKy(HocKy model, out string error)
        {
            error = "Thông tin học kỳ chưa hợp lệ.";
            if (!ModelState.IsValid || model.NgayKetThuc <= model.NgayBatDau)
                return false;

            var namHoc = _context.NamHocs.Find(model.IdNamHoc);
            if (namHoc == null)
            {
                error = "Năm học không tồn tại.";
                return false;
            }

            if (model.NgayBatDau < namHoc.NgayBatDau || model.NgayKetThuc > namHoc.NgayKetThuc)
            {
                error = "Thời gian học kỳ phải nằm trong năm học.";
                return false;
            }

            return true;
        }

        private ThoiKhoaBieuHocVuViewModel BuildThoiKhoaBieuViewModel()
        {
            return new ThoiKhoaBieuHocVuViewModel
            {
                GiaoViens = GetGiaoVienSelectList(),
                MonHocs = _context.MonHocs.OrderBy(x => x.TenMon)
                    .Select(x => new SelectListItem(x.TenMon, x.IdMonHoc.ToString())).ToList(),
                LopHocs = _context.LopHocs.OrderBy(x => x.TenLop)
                    .Select(x => new SelectListItem(x.TenLop, x.IdLop.ToString())).ToList(),
                NamHocs = GetNamHocSelectList(),
                HocKys = _context.HocKys.OrderBy(x => x.TenHocKy)
                    .Select(x => new SelectListItem(x.TenHocKy, x.TenHocKy)).Distinct().ToList()
            };
        }

        private List<SelectListItem> GetGiaoVienSelectList()
        {
            return _context.GiaoViens.OrderBy(x => x.HoTen)
                .Select(x => new SelectListItem($"{x.MaGV} - {x.HoTen}", x.IdGiaoVien.ToString())).ToList();
        }

        private List<SelectListItem> GetNamHocSelectList()
        {
            return _context.NamHocs.OrderByDescending(x => x.NgayBatDau)
                .Select(x => new SelectListItem(x.TenNamHoc, x.TenNamHoc)).ToList();
        }

        private List<SelectListItem> GetNamHocIdSelectList()
        {
            return _context.NamHocs.OrderByDescending(x => x.NgayBatDau)
                .Select(x => new SelectListItem(x.TenNamHoc, x.IdNamHoc.ToString())).ToList();
        }

        private static void NormalizeLop(LopHocFormViewModel vm)
        {
            vm.MaLop = vm.MaLop?.Trim() ?? string.Empty;
            vm.TenLop = vm.TenLop?.Trim() ?? string.Empty;
            vm.Khoi = vm.Khoi?.Trim();
            vm.BuoiHoc = vm.BuoiHoc?.Trim();
            vm.NamHoc = vm.NamHoc?.Trim();
        }

        private static void NormalizeMon(MonHocFormViewModel vm)
        {
            vm.MaMon = vm.MaMon?.Trim() ?? string.Empty;
            vm.TenMon = vm.TenMon?.Trim() ?? string.Empty;
        }

        private IActionResult RedirectWithSuccess(string action, string message, object routeValues = null)
        {
            TempData["Success"] = message;
            if (routeValues != null) return RedirectToAction(action, routeValues);
            return RedirectToAction(action);
        }

        private IActionResult RedirectWithError(string action, string message, object routeValues = null)
        {
            TempData["Error"] = message;
            if (routeValues != null) return RedirectToAction(action, routeValues);
            return RedirectToAction(action);
        }

        public IActionResult PhanCongGiaoVien(int lopId)
        {
            var lop = _context.LopHocs.Find(lopId);
            if (lop == null) return NotFound();

            var phanCongs = _context.PhanCongGiangDays
                .Include(x => x.MonHoc)
                .Include(x => x.GiaoVien)
                .Where(x => x.IdLop == lopId && x.MonHoc != null)
                .ToList();

            var monHocs = phanCongs
                .GroupBy(x => x.IdMonHoc)
                .Select(g => {
                    var chiTiet = new List<string>();
                    var requiredPeriods = new List<string>();
                    var grouped = g.Where(x => x.Thu.HasValue && x.TietBatDau.HasValue)
                                   .GroupBy(x => new { Thu = x.Thu ?? 0, BuoiHoc = x.LopHoc?.BuoiHoc ?? "" })
                                   .OrderBy(x => x.Key.Thu).ThenBy(x => x.Key.BuoiHoc);
                    foreach (var group in grouped)
                    {
                        var periods = new List<int>();
                        foreach(var s in group)
                        {
                            var start = s.TietBatDau ?? 0;
                            var length = s.SoTiet ?? 1;
                            for(int i = 0; i < length; i++) 
                            {
                                periods.Add(start + i);
                                requiredPeriods.Add($"{lop.BuoiHoc}_{group.Key.Thu}_{start + i}");
                            }
                        }
                        if (periods.Any()) {
                            periods.Sort();
                            var buoiStr = string.IsNullOrWhiteSpace(group.Key.BuoiHoc) ? "" : $" ({group.Key.BuoiHoc})";
                            chiTiet.Add($"Thứ {group.Key.Thu}{buoiStr}: Tiết {string.Join(", ", periods)}");
                        }
                    }

                    return new PhanCongMonHocItem
                    {
                        IdMonHoc = g.Key,
                        TenMonHoc = g.First().MonHoc!.TenMon,
                        IdGiaoVien = g.First().IdGiaoVien,
                        TenGiaoVien = g.First().GiaoVien?.HoTen,
                        ChiTietTietHoc = chiTiet,
                        RequiredPeriods = requiredPeriods
                    };
                })
                .OrderBy(x => x.TenMonHoc)
                .ToList();

            var allGiaoViens = _context.GiaoViens.Include(x => x.MonHoc).OrderBy(x => x.HoTen).ToList();
            var allSchedules = _context.PhanCongGiangDays
                .Include(x => x.LopHoc)
                .Where(x => x.NamHoc == lop.NamHoc && x.IdGiaoVien != null)
                .ToList();
            
            var giaoVienItems = new List<GiaoVienInfoItem>();
            foreach (var gv in allGiaoViens)
            {
                var schedules = allSchedules.Where(x => x.IdGiaoVien == gv.IdGiaoVien).ToList();
                var chiTiet = new List<string>();
                var busyPeriods = new List<string>();
                
                var grouped = schedules.Where(x => x.Thu.HasValue && x.TietBatDau.HasValue)
                                       .GroupBy(x => new { Thu = x.Thu ?? 0, BuoiHoc = x.LopHoc?.BuoiHoc ?? "" })
                                       .OrderBy(g => g.Key.Thu).ThenBy(g => g.Key.BuoiHoc);
                foreach (var group in grouped)
                {
                    var periods = new List<int>();
                    foreach(var s in group)
                    {
                        var start = s.TietBatDau ?? 0;
                        var length = s.SoTiet ?? 1;
                        for(int i = 0; i < length; i++) 
                        {
                            periods.Add(start + i);
                            if (s.IdLop != lopId) 
                            {
                                var buoi = s.LopHoc?.BuoiHoc ?? "";
                                busyPeriods.Add($"{buoi}_{group.Key.Thu}_{start + i}");
                            }
                        }
                    }
                    if (periods.Any()) {
                        periods.Sort();
                        var buoiStr = string.IsNullOrWhiteSpace(group.Key.BuoiHoc) ? "" : $" ({group.Key.BuoiHoc})";
                        chiTiet.Add($"Thứ {group.Key.Thu}{buoiStr}: Tiết {string.Join(", ", periods)}");
                    }
                }
                
                giaoVienItems.Add(new GiaoVienInfoItem
                {
                    GiaoVien = gv,
                    TongSoTiet = schedules.Sum(x => x.SoTiet ?? 0),
                    ChiTietLichDay = chiTiet,
                    BusyPeriods = busyPeriods
                });
            }

            var vm = new PhanCongGiaoVienViewModel
            {
                LopHoc = lop,
                DanhSachGiaoVien = giaoVienItems,
                DanhSachMonHoc = monHocs
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult CapNhatPhanCongGiaoVien([FromBody] CapNhatPhanCongRequest req)
        {
            if (req == null || req.LopId <= 0 || req.MonHocId <= 0 || req.GiaoVienId <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            var phanCongs = _context.PhanCongGiangDays
                .Include(x => x.LopHoc)
                .Where(x => x.IdLop == req.LopId && x.IdMonHoc == req.MonHocId)
                .ToList();

            if (!phanCongs.Any())
                return NotFound(new { success = false, message = "Không tìm thấy môn học trong lịch của lớp." });

            var giaoVien = _context.GiaoViens.Find(req.GiaoVienId);
            if (giaoVien == null) 
                return NotFound(new { success = false, message = "Giáo viên không tồn tại." });

            if (giaoVien.IdMonHoc.HasValue && giaoVien.IdMonHoc != req.MonHocId)
            {
                return BadRequest(new { success = false, message = $"Giáo viên {giaoVien.HoTen} không dạy môn học này!" });
            }

            var currentNamHoc = phanCongs.FirstOrDefault()?.NamHoc;
            if (!string.IsNullOrEmpty(currentNamHoc))
            {
                var gvSchedules = _context.PhanCongGiangDays
                    .Include(x => x.LopHoc)
                    .Where(x => x.IdGiaoVien == req.GiaoVienId && x.NamHoc == currentNamHoc && x.IdLop != req.LopId)
                    .ToList();

                foreach (var pc in phanCongs)
                {
                    if (pc.Thu.HasValue && pc.TietBatDau.HasValue)
                    {
                        var pcStart = pc.TietBatDau.Value;
                        var pcEnd = pcStart + (pc.SoTiet ?? 1) - 1;

                        foreach (var gvS in gvSchedules)
                        {
                            if (gvS.Thu == pc.Thu && gvS.TietBatDau.HasValue && gvS.LopHoc?.BuoiHoc == pc.LopHoc?.BuoiHoc)
                            {
                                var gvStart = gvS.TietBatDau.Value;
                                var gvEnd = gvStart + (gvS.SoTiet ?? 1) - 1;

                                if (Math.Max(pcStart, gvStart) <= Math.Min(pcEnd, gvEnd))
                                {
                                    return BadRequest(new { success = false, message = $"Trùng lịch! Giáo viên đã có giờ dạy ở lớp {gvS.LopHoc?.TenLop} ({gvS.LopHoc?.BuoiHoc}) vào Thứ {gvS.Thu} (Tiết {gvStart}-{gvEnd})." });
                                }
                            }
                        }
                    }
                }

                var totalAddingWeek = phanCongs.Sum(x => x.SoTiet ?? 1);
                var totalExistingWeek = gvSchedules.Sum(x => x.SoTiet ?? 1);
                if (totalExistingWeek + totalAddingWeek > 19)
                {
                    return BadRequest(new { success = false, message = $"Giáo viên {giaoVien.HoTen} đã có {totalExistingWeek} tiết trong tuần. Thêm {totalAddingWeek} tiết sẽ vượt quá giới hạn tối đa 19 tiết/tuần." });
                }

                var periodsByDay = phanCongs.Where(x => x.Thu.HasValue).GroupBy(x => x.Thu.Value);
                foreach (var group in periodsByDay)
                {
                    var thu = group.Key;
                    var totalAdding = group.Sum(x => x.SoTiet ?? 1);
                    var totalExisting = gvSchedules.Where(x => x.Thu == thu).Sum(x => x.SoTiet ?? 1);
                    if (totalExisting + totalAdding > 5)
                    {
                        return BadRequest(new { success = false, message = $"Giáo viên {giaoVien.HoTen} đã có {totalExisting} tiết dạy vào Thứ {thu}. Thêm {totalAdding} tiết sẽ vượt quá giới hạn tối đa 5 tiết/ngày." });
                    }
                }
            }

            foreach (var pc in phanCongs)
            {
                pc.IdGiaoVien = req.GiaoVienId;
            }

            _context.SaveChanges();

            return Ok(new { success = true, tenGiaoVien = giaoVien.HoTen });
        }

        public class CapNhatPhanCongRequest
        {
            public int LopId { get; set; }
            public int MonHocId { get; set; }
            public int GiaoVienId { get; set; }
        }

        private void AutoGenerateSchedule(LopHoc lop)
        {
            var subjectConfigs = new System.Collections.Generic.List<(string MaMon, string TenMon, int SoTiet)>
            {
                ("NV", "Ngữ văn", 4), ("TOAN", "Toán", 4), ("TA", "Tiếng Anh", 3),
                ("GDCD", "Giáo dục công dân", 1), ("LS", "Lịch sử", 1), ("DL", "Địa lí", 2),
                ("VL", "Vật lí", 1), ("HH", "Hóa học", 1), ("SH", "Sinh học", 2),
                ("CN", "Công nghệ", 1), ("TH", "Tin học", 1), ("GDTC", "Giáo dục thể chất", 2),
                ("NT", "Nghệ thuật (Âm nhạc, Mỹ thuật)", 2), ("HDTN", "Hoạt động trải nghiệm, hướng nghiệp", 3),
                ("GDDP", "Nội dung giáo dục địa phương", 1)
            };

            var allMonHocs = _context.MonHocs.ToList();
            var allGiaoViens = _context.GiaoViens.ToList();
            var scheduleItems = new System.Collections.Generic.List<PhanCongGiangDay>();

            var existingSchedules = _context.PhanCongGiangDays
                .Include(x => x.LopHoc)
                .Where(x => x.NamHoc == lop.NamHoc && x.IdGiaoVien != null)
                .ToList();

            var allSchedulesInYear = _context.PhanCongGiangDays
                .Include(x => x.LopHoc)
                .Where(x => x.NamHoc == lop.NamHoc)
                .ToList();

            var specialRooms = _context.PhongHocs.Where(x => x.MaPhong == "PTIN" || x.MaPhong == "PTIN2" || x.MaPhong == "PTC" || x.MaPhong == "PTC2").ToList();
            var ptinIds = specialRooms.Where(x => x.MaPhong.StartsWith("PTIN")).Select(x => x.IdPhongHoc).ToList();
            var ptcIds = specialRooms.Where(x => x.MaPhong.StartsWith("PTC")).Select(x => x.IdPhongHoc).ToList();

            var random = new System.Random();
            var dayLoads = new int[8]; // index 2-7, value: number of periods assigned so far (0-5)

            foreach (var req in subjectConfigs)
            {
                var monHoc = allMonHocs.FirstOrDefault(m => m.TenMon.ToLower() == req.TenMon.ToLower() || m.MaMon.ToUpper() == req.MaMon.ToUpper());
                if (monHoc == null)
                {
                    monHoc = new MonHoc { MaMon = req.MaMon, TenMon = req.TenMon, SoTiet = req.SoTiet };
                    _context.MonHocs.Add(monHoc);
                    _context.SaveChanges();
                    allMonHocs.Add(monHoc);
                }

                var blocks = new System.Collections.Generic.List<int>();
                int remaining = req.SoTiet;
                while (remaining > 0)
                {
                    if (remaining >= 2) { blocks.Add(2); remaining -= 2; }
                    else { blocks.Add(1); remaining -= 1; }
                }

                var subjectSlots = new System.Collections.Generic.List<(int Thu, int TietBatDau, int? IdPhongHoc)>();
                
                foreach (var b in blocks)
                {
                    var possibleDays = new System.Collections.Generic.List<(int Thu, int? RoomId)>();
                    
                    for (int fallback = 0; fallback <= 1; fallback++)
                    {
                        for (int thu = 2; thu <= 7; thu++)
                        {
                            bool alreadyHasSubjectToday = subjectSlots.Any(s => s.Thu == thu);
                            if (fallback == 0 && alreadyHasSubjectToday) continue;
                            
                            if (dayLoads[thu] + b <= 5)
                            {
                                int? selectedRoomId = null;
                                bool roomOk = true;

                                if (req.MaMon == "TH")
                                {
                                    selectedRoomId = GetFreeRoom(allSchedulesInYear, ptinIds, lop.BuoiHoc, thu, dayLoads[thu] + 1, b);
                                    if (selectedRoomId == null) roomOk = false;
                                }
                                else if (req.MaMon == "GDTC")
                                {
                                    selectedRoomId = GetFreeRoom(allSchedulesInYear, ptcIds, lop.BuoiHoc, thu, dayLoads[thu] + 1, b);
                                    if (selectedRoomId == null) roomOk = false;
                                }

                                if (roomOk)
                                {
                                    possibleDays.Add((thu, selectedRoomId));
                                }
                            }
                        }
                        if (possibleDays.Any()) break;
                    }

                    if (possibleDays.Any())
                    {
                        var chosenDay = possibleDays[random.Next(possibleDays.Count)];
                        var chosenThu = chosenDay.Thu;
                        
                        for (int i = 0; i < b; i++)
                        {
                            int tiet = dayLoads[chosenThu] + 1;
                            subjectSlots.Add((chosenThu, tiet, chosenDay.RoomId));
                            dayLoads[chosenThu]++;
                        }
                        
                        // Temporarily add to allSchedulesInYear so subsequent blocks know it's taken
                        if (chosenDay.RoomId != null)
                        {
                            var tempSch = new PhanCongGiangDay
                            {
                                IdPhongHoc = chosenDay.RoomId,
                                NamHoc = lop.NamHoc,
                                Thu = chosenThu,
                                TietBatDau = dayLoads[chosenThu] - b + 1,
                                SoTiet = b,
                                LopHoc = new LopHoc { BuoiHoc = lop.BuoiHoc }
                            };
                            allSchedulesInYear.Add(tempSch);
                        }
                    }
                }

                var subjectTeachers = allGiaoViens.Where(g => g.IdMonHoc == monHoc.IdMonHoc).ToList();
                int? assignedGiaoVienId = null;

                var distinctDays = subjectSlots.Select(x => x.Thu).Distinct().ToList();
                var orderedTeachers = subjectTeachers.OrderBy(gv => 
                    distinctDays.Sum(thu => existingSchedules.Where(s => s.IdGiaoVien == gv.IdGiaoVien && s.Thu == thu).Sum(s => s.SoTiet ?? 1))
                ).ThenBy(x => random.Next()).ToList();

                foreach (var gv in orderedTeachers)
                {
                    var existingTotalForWeek = existingSchedules.Where(s => s.IdGiaoVien == gv.IdGiaoVien).Sum(s => s.SoTiet ?? 1);
                    if (existingTotalForWeek + subjectSlots.Count > 19) continue;

                    bool isAvailable = true;
                    foreach (var slot in subjectSlots)
                    {
                        var conflict = existingSchedules.Any(s =>
                            s.IdGiaoVien == gv.IdGiaoVien && s.Thu == slot.Thu && s.LopHoc?.BuoiHoc == lop.BuoiHoc &&
                            s.TietBatDau.HasValue && slot.TietBatDau >= s.TietBatDau.Value && slot.TietBatDau <= s.TietBatDau.Value + (s.SoTiet ?? 1) - 1);

                        if (conflict) { isAvailable = false; break; }
                    }

                    if (isAvailable)
                    {
                        var slotsByDay = subjectSlots.GroupBy(x => x.Thu);
                        foreach (var group in slotsByDay)
                        {
                            var thu = group.Key;
                            var adding = group.Count();
                            var existing = existingSchedules.Where(s => s.IdGiaoVien == gv.IdGiaoVien && s.Thu == thu).Sum(s => s.SoTiet ?? 1);
                            if (existing + adding > 5) { isAvailable = false; break; }
                        }
                    }

                    if (isAvailable)
                    {
                        assignedGiaoVienId = gv.IdGiaoVien;
                        break;
                    }
                }

                foreach (var slot in subjectSlots)
                {
                    var newSchedule = new PhanCongGiangDay
                    {
                        IdGiaoVien = assignedGiaoVienId,
                        IdMonHoc = monHoc.IdMonHoc,
                        IdLop = lop.IdLop,
                        NamHoc = lop.NamHoc,
                        HocKy = "Cả năm",
                        Thu = slot.Thu,
                        TietBatDau = slot.TietBatDau,
                        SoTiet = 1,
                        IdPhongHoc = slot.IdPhongHoc,
                        LopHoc = lop
                    };
                    scheduleItems.Add(newSchedule);

                    if (assignedGiaoVienId != null)
                    {
                        existingSchedules.Add(newSchedule);
                    }
                }
            }

            _context.PhanCongGiangDays.AddRange(scheduleItems);
            _context.SaveChanges();
        }

        private int? GetFreeRoom(System.Collections.Generic.List<PhanCongGiangDay> allSchedules, System.Collections.Generic.List<int> roomIds, string? buoiHoc, int thu, int startTiet, int soTiet)
        {
            foreach (var roomId in roomIds)
            {
                bool isTaken = allSchedules.Any(s =>
                    s.IdPhongHoc == roomId &&
                    s.Thu == thu &&
                    s.LopHoc?.BuoiHoc == buoiHoc &&
                    s.TietBatDau.HasValue &&
                    startTiet <= s.TietBatDau.Value + (s.SoTiet ?? 1) - 1 &&
                    (startTiet + soTiet - 1) >= s.TietBatDau.Value);
                if (!isTaken) return roomId;
            }
            return null;
        }
        public IActionResult ChuyenLich(string? namHoc, int? lopId, DateTime? tuan)
        {
            var selectedDate = tuan ?? DateTime.Today;
            
            ViewBag.FilterNamHoc = namHoc;
            ViewBag.FilterLopId = lopId;
            ViewBag.Tuan = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.NamHocs = GetNamHocSelectList();
            
            var classes = new List<LopHoc>();
            if (!string.IsNullOrEmpty(namHoc))
            {
                classes = _context.LopHocs
                    .Where(x => x.NamHoc == namHoc)
                    .OrderBy(x => x.TenLop)
                    .ToList();
            }
            ViewBag.Classes = classes;

            if (lopId.HasValue)
            {
                var lop = _context.LopHocs.Find(lopId.Value);
                ViewBag.SelectedClass = lop;
                
                var diff = (7 + (selectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                var startOfWeek = selectedDate.AddDays(-diff);
                var endDate = startOfWeek.AddDays(6);

                var schedules = _context.PhanCongGiangDays
                    .Include(x => x.GiaoVien)
                    .Include(x => x.MonHoc)
                    .Include(x => x.PhongHoc)
                    .Where(x => x.IdLop == lopId.Value && x.NamHoc == namHoc)
                    .ToList();
                
                ViewBag.Schedules = schedules;
                ViewBag.StartOfWeek = startOfWeek.ToString("yyyy-MM-dd");
                
                var thayDois = _context.LichHocThayDois
                    .Include(x => x.MonHocThayThe)
                    .Include(x => x.GiaoVienThayThe)
                    .Where(x => x.IdLop == lopId.Value && x.Ngay >= startOfWeek && x.Ngay <= endDate)
                    .ToList();
                ViewBag.ThayDois = thayDois;
                
                var lichSu = _context.LichHocThayDois
                    .Include(x => x.LopHoc)
                    .Include(x => x.MonHocThayThe)
                    .Include(x => x.GiaoVienThayThe)
                    .Where(x => x.IdLop == lopId.Value)
                    .OrderByDescending(x => x.Ngay)
                    .ToList();
                ViewBag.LichSu = lichSu;

                var phongHocMap = _context.PhongHocs.Where(x => x.IdLop == lopId.Value).ToDictionary(x => x.IdLop.Value, x => !string.IsNullOrEmpty(x.TenPhong) ? x.TenPhong : (!string.IsNullOrEmpty(x.MaPhong) ? x.MaPhong : "-"));
                ViewBag.PhongHocMap = phongHocMap;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XuLyChuyenLich(string NamHoc, int LopId, DateTime CurDate, int CurPeriod, DateTime TarDate, int TarPeriod, string LyDo)
        {
            int oldThu = (int)CurDate.DayOfWeek + 1 == 1 ? 8 : (int)CurDate.DayOfWeek + 1;
            int newThu = (int)TarDate.DayOfWeek + 1 == 1 ? 8 : (int)TarDate.DayOfWeek + 1;

            var oldSchedule = _context.PhanCongGiangDays
                .Include(x => x.MonHoc)
                .Include(x => x.GiaoVien)
                .Include(x => x.PhongHoc)
                .FirstOrDefault(x => x.IdLop == LopId && x.Thu == oldThu && x.TietBatDau <= CurPeriod && (x.TietBatDau + (x.SoTiet ?? 1) - 1) >= CurPeriod);

            if (oldSchedule != null)
            {
                string phong = oldSchedule.PhongHoc?.TenPhong ?? oldSchedule.PhongHoc?.MaPhong;
                if (string.IsNullOrEmpty(phong))
                {
                    var defaultRoom = _context.PhongHocs.FirstOrDefault(x => x.IdLop == LopId);
                    phong = defaultRoom?.TenPhong ?? defaultRoom?.MaPhong ?? "-";
                }

                // Lưu lịch sử: Tiết cũ bị đổi
                var thayDoiNghi = new LichHocThayDoi
                {
                    Ngay = CurDate.Date.Add(DateTime.Now.TimeOfDay),
                    IdLop = LopId,
                    TietBatDau = CurPeriod,
                    SoTiet = oldSchedule.SoTiet ?? 1,
                    IsNghi = true,
                    GhiChu = $"Chuyển sang {TarDate:dd/MM/yyyy} Tiết {TarPeriod} | Môn: {oldSchedule.MonHoc?.TenMon ?? "-"} | GV: {oldSchedule.GiaoVien?.HoTen ?? "-"} | Phòng: {phong} | Lý do: {LyDo}"
                };
                _context.LichHocThayDois.Add(thayDoiNghi);

                // Kiểm tra xem tiết đích có lịch chưa, nếu có thì có thể là hoán đổi
                var targetSchedule = _context.PhanCongGiangDays
                    .Include(x => x.MonHoc)
                    .Include(x => x.GiaoVien)
                    .Include(x => x.PhongHoc)
                    .FirstOrDefault(x => x.IdLop == LopId && x.Thu == newThu && x.TietBatDau == TarPeriod);
                
                if (targetSchedule != null)
                {
                    string targetPhong = targetSchedule.PhongHoc?.TenPhong ?? targetSchedule.PhongHoc?.MaPhong;
                    if (string.IsNullOrEmpty(targetPhong))
                    {
                        var defaultRoom = _context.PhongHocs.FirstOrDefault(x => x.IdLop == LopId);
                        targetPhong = defaultRoom?.TenPhong ?? defaultRoom?.MaPhong ?? "-";
                    }

                    // Tiết đích cũ nghỉ học
                    var targetNghi = new LichHocThayDoi
                    {
                        Ngay = TarDate.Date.AddSeconds(1),
                        IdLop = LopId,
                        TietBatDau = TarPeriod,
                        SoTiet = targetSchedule.SoTiet ?? 1,
                        IsNghi = true,
                        GhiChu = $"Chuyển sang {CurDate:dd/MM/yyyy} Tiết {CurPeriod} | Môn: {targetSchedule.MonHoc?.TenMon ?? "-"} | GV: {targetSchedule.GiaoVien?.HoTen ?? "-"} | Phòng: {targetPhong} | Lý do: Hoán đổi với lịch {oldSchedule.MonHoc?.TenMon}"
                    };
                    _context.LichHocThayDois.Add(targetNghi);

                    // Dạy bù cho tiết cũ
                    var targetDayBu = new LichHocThayDoi
                    {
                        Ngay = TarDate.Date.AddSeconds(2),
                        IdLop = LopId,
                        TietBatDau = TarPeriod,
                        SoTiet = oldSchedule.SoTiet ?? 1,
                        IsNghi = false,
                        IdMonHocThayThe = oldSchedule.IdMonHoc,
                        IdGiaoVienThayThe = oldSchedule.IdGiaoVien,
                        GhiChu = $"Dạy bù | Phòng: {targetPhong}"
                    };
                    _context.LichHocThayDois.Add(targetDayBu);

                    // Dạy bù cho tiết đích
                    var oldDayBu = new LichHocThayDoi
                    {
                        Ngay = CurDate.Date.AddSeconds(2),
                        IdLop = LopId,
                        TietBatDau = CurPeriod,
                        SoTiet = targetSchedule.SoTiet ?? 1,
                        IsNghi = false,
                        IdMonHocThayThe = targetSchedule.IdMonHoc,
                        IdGiaoVienThayThe = targetSchedule.IdGiaoVien,
                        GhiChu = $"Dạy bù | Phòng: {phong}"
                    };
                    _context.LichHocThayDois.Add(oldDayBu);
                }
                else
                {
                    // Dạy bù cho tiết cũ tại ngày đích
                    var dayBu = new LichHocThayDoi
                    {
                        Ngay = TarDate.Date.AddSeconds(2),
                        IdLop = LopId,
                        TietBatDau = TarPeriod,
                        SoTiet = oldSchedule.SoTiet ?? 1,
                        IsNghi = false,
                        IdMonHocThayThe = oldSchedule.IdMonHoc,
                        IdGiaoVienThayThe = oldSchedule.IdGiaoVien,
                        GhiChu = $"Dạy bù | Phòng: {phong}"
                    };
                    _context.LichHocThayDois.Add(dayBu);
                }

                _context.SaveChanges();
                TempData["Success"] = "Chuyển lịch trong tuần thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tiết học cần chuyển.";
            }

            return RedirectToAction(nameof(ChuyenLich), new { namHoc = NamHoc, lopId = LopId });
        }

        [HttpPost]
        public IActionResult KiemTraXungDot(int lopId, string curDate, int curPeriod, string tarDate, int tarPeriod)
        {
            try
            {
                DateTime dtCur = DateTime.Parse(curDate);
                DateTime dtTar = DateTime.Parse(tarDate);

                int oldThu = (int)dtCur.DayOfWeek + 1 == 1 ? 8 : (int)dtCur.DayOfWeek + 1;
                int newThu = (int)dtTar.DayOfWeek + 1 == 1 ? 8 : (int)dtTar.DayOfWeek + 1;

                var oldSchedule = _context.PhanCongGiangDays
                    .Include(x => x.LopHoc)
                    .FirstOrDefault(x => x.IdLop == lopId && x.Thu == oldThu && x.TietBatDau <= curPeriod && (x.TietBatDau + (x.SoTiet ?? 1) - 1) >= curPeriod);

                if (oldSchedule == null)
                    return Json(new { success = false, message = "Không tìm thấy tiết học." });

                // Kiểm tra lớp: Vì đổi trong cùng 1 lớp (hoặc hoán đổi với môn khác của cùng lớp) nên lớp không bao giờ xung đột với chính nó.
                bool classOk = true;
                string classMsg = "Không xung đột";

                // Kiểm tra giáo viên: Giáo viên có đang dạy lớp KHÁC vào tiết mới và CÙNG BUỔI không?
                bool gvOk = true;
                string gvMsg = "Không xung đột";
                if (oldSchedule.IdGiaoVien.HasValue)
                {
                    var teacherConflict = _context.PhanCongGiangDays
                        .Include(x => x.LopHoc)
                        .FirstOrDefault(x => x.IdGiaoVien == oldSchedule.IdGiaoVien.Value && x.Thu == newThu && x.TietBatDau == tarPeriod && x.IdLop != lopId && x.LopHoc.BuoiHoc == oldSchedule.LopHoc.BuoiHoc);
                    
                    if (teacherConflict != null)
                    {
                        gvOk = false;
                        gvMsg = $"Giáo viên đang dạy lớp {teacherConflict.LopHoc?.TenLop}";
                    }
                }

                // Kiểm tra phòng: Phòng học có đang được lớp KHÁC sử dụng vào tiết mới và CÙNG BUỔI không?
                bool roomOk = true;
                string roomMsg = "Không xung đột";
                if (oldSchedule.IdPhongHoc.HasValue)
                {
                    var roomConflict = _context.PhanCongGiangDays
                        .Include(x => x.LopHoc)
                        .FirstOrDefault(x => x.IdPhongHoc == oldSchedule.IdPhongHoc.Value && x.Thu == newThu && x.TietBatDau == tarPeriod && x.IdLop != lopId && x.LopHoc.BuoiHoc == oldSchedule.LopHoc.BuoiHoc);
                    
                    if (roomConflict != null)
                    {
                        roomOk = false;
                        roomMsg = $"Phòng đang được dùng bởi {roomConflict.LopHoc?.TenLop}";
                    }
                }

                // Tiết mới luôn hợp lệ vì giao diện chỉ cho chọn tiết 1-5
                bool tietOk = true;
                string tietMsg = "Hợp lệ";

                bool isValid = classOk && gvOk && roomOk && tietOk;

                return Json(new
                {
                    success = true,
                    isValid = isValid,
                    chkLop = new { ok = classOk, msg = classMsg },
                    chkGv = new { ok = gvOk, msg = gvMsg },
                    chkPhong = new { ok = roomOk, msg = roomMsg },
                    chkTiet = new { ok = tietOk, msg = tietMsg }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoNamHocTuDong(NamHoc model)
        {
            if (!ModelState.IsValid || model.NgayKetThuc <= model.NgayBatDau)
            {
                TempData["Error"] = "Thông tin năm học chưa hợp lệ.";
                return RedirectToAction("Index", "LenLop");
            }

            if (_context.NamHocs.Any(x => x.TenNamHoc == model.TenNamHoc))
            {
                TempData["Error"] = "Năm học đã tồn tại.";
                return RedirectToAction("Index", "LenLop");
            }

            var newNamHoc = new NamHoc
            {
                TenNamHoc = model.TenNamHoc.Trim(),
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc,
                TrangThai = model.TrangThai
            };
            
            _context.NamHocs.Add(newNamHoc);
            _context.SaveChanges();

            var match = System.Text.RegularExpressions.Regex.Match(newNamHoc.TenNamHoc, @"^(\d{4})-(\d{4})$");
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int startYear) && int.TryParse(match.Groups[2].Value, out int endYear))
                {
                    _context.HocKys.Add(new HocKy
                    {
                        IdNamHoc = newNamHoc.IdNamHoc,
                        TenHocKy = "Học kỳ 1",
                        NgayBatDau = new DateTime(startYear, 9, 5),
                        NgayKetThuc = new DateTime(startYear, 12, 31),
                        TrangThai = true
                    });

                    _context.HocKys.Add(new HocKy
                    {
                        IdNamHoc = newNamHoc.IdNamHoc,
                        TenHocKy = "Học kỳ 2",
                        NgayBatDau = new DateTime(endYear, 1, 1),
                        NgayKetThuc = new DateTime(endYear, 5, 31),
                        TrangThai = true
                    });

                    _context.SaveChanges();

                    string prefix = startYear.ToString().Substring(2, 2);
                    for (int khoi = 6; khoi <= 9; khoi++)
                    {
                        for (int i = 1; i <= 5; i++)
                        {
                            string className = $"{khoi}A{i}";
                            string classCode = $"K{prefix}_{className}";
                            
                            var newLop = new LopHoc
                            {
                                MaLop = classCode,
                                TenLop = className,
                                Khoi = khoi.ToString(),
                                BuoiHoc = (khoi == 6 || khoi == 7) ? "Sáng" : "Chiều",
                                NamHoc = newNamHoc.TenNamHoc
                            };
                            _context.LopHocs.Add(newLop);
                            _context.SaveChanges();

                            AutoGenerateSchedule(newLop);
                        }
                    }
                }
            }

            TempData["Success"] = "Đã thêm năm học, tạo 20 lớp và xếp TKB thành công.";
            return RedirectToAction("Index", "LenLop");
        }
    }
}

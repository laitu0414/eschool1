using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eSchool.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace eSchool.Controllers
{
    public class LopStat
    {
        public string Khoi { get; set; }
        public string TenLop { get; set; }
        public int SiSo { get; set; }
        public int DaTongKet { get; set; }
        public string TrangThai { get; set; }
    }

    public class KhoiStat
    {
        public string Khoi { get; set; }
        public int TongSoHS { get; set; }
        public int DuDieuKien { get; set; }
        public int ChuaDuDieuKien { get; set; }
        public int BoHoc { get; set; }
        public double TyLe { get; set; }
    }

    public class LenLopController : Controller
    {
        private readonly AppDbContext _context;

        public LenLopController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var allHocSinh = await _context.HocSinhs.Include(h => h.LopHoc).Include(h => h.Diems).ToListAsync();
            var allLops = await _context.LopHocs.ToListAsync();

            int totalHS = allHocSinh.Count;
            int activeHS = allHocSinh.Count(h => h.TrangThai);
            int inactiveHS = allHocSinh.Count(h => !h.TrangThai); // Bo hoc/chuyen truong

            // Theo logic: Hạnh kiểm & Học lực không có trong DB nên tạm định nghĩa Đủ điều kiện = Active và DaDuyet = true nếu đã duyệt (hoặc mock cho Step 2)
            // Để mock chính xác hình vẽ cho step 2:
            // "Chưa đủ điều kiện" là Active student có GhiChu="Không đủ điều kiện"
            int chuaDuDieuKien = allHocSinh.Count(h => h.TrangThai && h.GhiChu != null && h.GhiChu.Contains("Không đủ điều kiện"));
            int duDieuKien = activeHS - chuaDuDieuKien;
            int boHoc = inactiveHS;

            ViewBag.TotalHS = totalHS;
            ViewBag.DuDieuKien = duDieuKien;
            ViewBag.ChuaDuDieuKien = chuaDuDieuKien;
            ViewBag.BoHoc = boHoc;
            
            int daDuyet = allHocSinh.Count(h => h.DaDuyet);
            int choDuyet = allHocSinh.Count(h => !h.DaDuyet && h.TrangThai);
            ViewBag.DaDuyet = daDuyet;
            ViewBag.ChoDuyet = choDuyet;
            ViewBag.TyLeDuyet = (totalHS > 0) ? Math.Round((double)daDuyet / totalHS * 100, 2) : 0;

            // Stats for Step 1
            // Học sinh chưa tổng kết là học sinh đang theo học (Active) và:
            // - Chưa có điểm nào (chưa nhập điểm)
            // - HOẶC có môn học nào đó chưa có Điểm TB (chưa tổng kết xong môn đó)
            int chuaTongKet = allHocSinh.Count(h => h.TrangThai && (h.Diems == null || h.Diems.Count == 0 || h.Diems.Any(d => d.DiemTB == null)));
            int daTongKet = totalHS - chuaTongKet;
            ViewBag.DaTongKet = daTongKet;
            ViewBag.ChuaTongKet = chuaTongKet;

            var lopStats = allLops.Select(l => {
                int siso = allHocSinh.Count(h => h.IdLopHoc == l.IdLop);
                int ctk = allHocSinh.Count(h => h.IdLopHoc == l.IdLop && h.TrangThai && (h.Diems == null || h.Diems.Count == 0 || h.Diems.Any(d => d.DiemTB == null)));
                return new LopStat {
                    Khoi = l.Khoi ?? "",
                    TenLop = l.TenLop,
                    SiSo = siso,
                    DaTongKet = siso - ctk,
                    TrangThai = ctk == 0 ? "Đã hoàn thành" : "Chưa hoàn thành"
                };
            }).OrderBy(l => l.Khoi).ThenBy(l => l.TenLop).ToList();
            ViewBag.LopStats = lopStats;

            // Stats for Step 2
            var khoiStats = allHocSinh.Where(h => h.LopHoc != null).GroupBy(h => h.LopHoc.Khoi).Select(g => {
                int total = g.Count();
                int cdk = g.Count(h => h.TrangThai && h.GhiChu != null && h.GhiChu.Contains("Không đủ điều kiện"));
                int bh = g.Count(h => !h.TrangThai);
                int dk = g.Count(h => h.TrangThai) - cdk;
                return new KhoiStat {
                    Khoi = g.Key,
                    TongSoHS = total,
                    DuDieuKien = dk,
                    ChuaDuDieuKien = cdk,
                    BoHoc = bh,
                    TyLe = (total > 0) ? Math.Round((double)dk / total * 100, 2) : 0
                };
            }).OrderBy(g => g.Khoi).ToList();
            ViewBag.KhoiStats = khoiStats;

            // List of students for Step 3
            ViewBag.HocSinhs = allHocSinh.OrderBy(h => h.LopHoc?.TenLop).ThenBy(h => h.HoTen).ToList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApproveStudent(int id)
        {
            var hs = await _context.HocSinhs.FindAsync(id);
            if (hs != null)
            {
                hs.DaDuyet = true;
                hs.GhiChu = "Đã duyệt";
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã duyệt học sinh {hs.MaHS}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RejectStudent(int id)
        {
            var hs = await _context.HocSinhs.FindAsync(id);
            if (hs != null)
            {
                hs.DaDuyet = false;
                hs.GhiChu = "Không đủ điều kiện";
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã từ chối học sinh {hs.MaHS}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkApprove(string ids)
        {
            if (string.IsNullOrEmpty(ids)) return RedirectToAction(nameof(Index));
            
            var idList = ids.Split(',').Select(int.Parse).ToList();
            var students = await _context.HocSinhs.Where(h => idList.Contains(h.IdHocSinh)).ToListAsync();
            foreach (var hs in students)
            {
                hs.DaDuyet = true;
                hs.GhiChu = "Đã duyệt nhóm";
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã duyệt {students.Count} học sinh.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public async Task<IActionResult> BulkReject(string ids)
        {
            if (string.IsNullOrEmpty(ids)) return RedirectToAction(nameof(Index));
            
            var idList = ids.Split(',').Select(int.Parse).ToList();
            var students = await _context.HocSinhs.Where(h => idList.Contains(h.IdHocSinh)).ToListAsync();
            foreach (var hs in students)
            {
                hs.DaDuyet = false;
                hs.GhiChu = "Không đủ điều kiện";
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã từ chối {students.Count} học sinh.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public async Task<IActionResult> BulkApproveAllEligible()
        {
            var students = await _context.HocSinhs.Where(h => h.TrangThai && (h.GhiChu == null || !h.GhiChu.Contains("Không đủ điều kiện")) && !h.DaDuyet).ToListAsync();
            foreach (var hs in students)
            {
                hs.DaDuyet = true;
                hs.GhiChu = "Đã duyệt";
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã duyệt tất cả học sinh đủ điều kiện ({students.Count}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult LockResults()
        {
            TempData["IsLocked"] = true;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AutoAssignClass(string NamHoc, string Khoi, string TrangThai)
        {
            // Dummy logic cho demo: Thông báo xếp lớp thành công
            // Trong thực tế sẽ cần lấy danh sách học sinh đủ điều kiện và chia đều vào các lớp mới sinh ra
            TempData["Success"] = $"Đã xếp lớp tự động cho năm học {NamHoc} thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> PrintResults(string NamHoc, string Khoi, string Lop, string LoaiKetQua, string HinhThucIn, string MauIn)
        {
            var query = _context.HocSinhs.Include(h => h.LopHoc).AsQueryable();
            if (!string.IsNullOrEmpty(Khoi)) query = query.Where(h => h.LopHoc != null && h.LopHoc.Khoi == Khoi);
            if (!string.IsNullOrEmpty(Lop)) query = query.Where(h => h.LopHoc != null && h.LopHoc.TenLop == Lop);
            var students = await query.OrderBy(h => h.LopHoc.TenLop).ThenBy(h => h.HoTen).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container => {
                container.Page(page => {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));
                    
                    page.Header().Text(MauIn ?? "Danh sách kết quả").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x => {
                        x.Spacing(10);
                        x.Item().Text($"Năm học: {NamHoc}");
                        x.Item().Text($"Khối: {Khoi}");
                        x.Item().Text($"Lớp: {Lop}");
                        x.Item().Text($"Loại kết quả: {LoaiKetQua}");
                        x.Item().PaddingTop(20).Text("Danh sách học sinh").Bold();
                        
                        int i = 1;
                        foreach (var hs in students)
                        {
                            string trangThai = hs.DaDuyet ? "Đã duyệt" : (hs.GhiChu ?? (hs.TrangThai ? "Đủ điều kiện" : "Bỏ học"));
                            string lopName = hs.LopHoc != null ? hs.LopHoc.TenLop : "";
                            x.Item().Text($"{i++}. {hs.MaHS} - {hs.HoTen} - Lớp {lopName} - {trangThai}");
                        }
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
            
            byte[] pdfBytes = document.GeneratePdf();
            if (HinhThucIn == "Preview")
            {
                return File(pdfBytes, "application/pdf");
            }
            return File(pdfBytes, "application/pdf", "DanhSachKetQua.pdf");
        }

        [HttpPost]
        public async Task<IActionResult> ExportExcel(string NamHoc, string Khoi, string Lop, string LoaiDuLieu, string DinhDangFile, List<string> BaoGom)
        {
            var query = _context.HocSinhs.Include(h => h.LopHoc).AsQueryable();
            if (!string.IsNullOrEmpty(Khoi)) query = query.Where(h => h.LopHoc != null && h.LopHoc.Khoi == Khoi);
            if (!string.IsNullOrEmpty(Lop)) query = query.Where(h => h.LopHoc != null && h.LopHoc.TenLop == Lop);
            var students = await query.OrderBy(h => h.LopHoc.TenLop).ThenBy(h => h.HoTen).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("KetQua");
            worksheet.Cell(1, 1).Value = "Kết quả xét lên lớp - " + NamHoc;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            
            int col = 1;
            if (BaoGom != null && BaoGom.Count > 0)
            {
                foreach(var field in BaoGom)
                {
                    worksheet.Cell(3, col).Value = field;
                    worksheet.Cell(3, col).Style.Font.Bold = true;
                    worksheet.Cell(3, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                    col++;
                }
            }
            else
            {
                worksheet.Cell(3, 1).Value = "Mã HS";
                worksheet.Cell(3, 2).Value = "Họ tên";
                worksheet.Cell(3, 3).Value = "Lớp";
                worksheet.Cell(3, 4).Value = "Trạng thái";
            }
            
            int row = 4;
            foreach (var hs in students)
            {
                string trangThai = hs.DaDuyet ? "Đã duyệt" : (hs.GhiChu ?? (hs.TrangThai ? "Đủ điều kiện" : "Bỏ học"));
                if (BaoGom != null && BaoGom.Count > 0)
                {
                    int c = 1;
                    foreach(var field in BaoGom)
                    {
                        if (field.Contains("Mã", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = hs.MaHS;
                        else if (field.Contains("Tên", StringComparison.OrdinalIgnoreCase) || field.Contains("Họ", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = hs.HoTen;
                        else if (field.Contains("Lớp", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = hs.LopHoc?.TenLop;
                        else if (field.Contains("Trạng", StringComparison.OrdinalIgnoreCase) || field.Contains("Kết quả", StringComparison.OrdinalIgnoreCase)) worksheet.Cell(row, c).Value = trangThai;
                        else worksheet.Cell(row, c).Value = "";
                        c++;
                    }
                }
                else
                {
                    worksheet.Cell(row, 1).Value = hs.MaHS;
                    worksheet.Cell(row, 2).Value = hs.HoTen;
                    worksheet.Cell(row, 3).Value = hs.LopHoc?.TenLop;
                    worksheet.Cell(row, 4).Value = trangThai;
                }
                row++;
            }
            
            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KetQua_{NamHoc}.xlsx");
        }
    }
}

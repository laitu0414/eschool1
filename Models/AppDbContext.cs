    using Microsoft.EntityFrameworkCore;

    namespace eSchool.Models
    {
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
            {
            }

            public DbSet<ChucVu> ChucVus { get; set; }
            public DbSet<TaiKhoan> TaiKhoans { get; set; }
            public DbSet<GiaoVien> GiaoViens { get; set; }
            public DbSet<HocSinh> HocSinhs { get; set; }
            public DbSet<LopHoc> LopHocs { get; set; }
            public DbSet<MonHoc> MonHocs { get; set; }
            public DbSet<DangKyLop> DangKyLops { get; set; }
            public DbSet<PhanCongGiangDay> PhanCongGiangDays { get; set; }
            public DbSet<Diem> Diems { get; set; }
            public DbSet<DiemDanh> DiemDanhs { get; set; }
            public DbSet<HocPhi> HocPhis { get; set; }
            public DbSet<ThongBao> ThongBaos { get; set; }
            public DbSet<NhatKyHoatDong> NhatKyHoatDongs { get; set; }
            public DbSet<PhuHuynh> PhuHuynhs { get; set; }
            public DbSet<HocSinhPhuHuynh> HocSinhPhuHuynhs { get; set; }
            public DbSet<ChuyenLop> ChuyenLops { get; set; }
            public DbSet<NamHoc> NamHocs { get; set; }
            public DbSet<HocKy> HocKys { get; set; }
            public DbSet<PhieuDiem> PhieuDiems { get; set; }
            public DbSet<TinTucSuKien> TinTucSuKiens { get; set; }
            public DbSet<LichHocThayDoi> LichHocThayDois { get; set; }
            public DbSet<PhongHoc> PhongHocs { get; set; }
            public DbSet<KyLuat> KyLuats { get; set; }
            public DbSet<ThietBi> ThietBis { get; set; }
            public DbSet<BaoTri> BaoTris { get; set; }
            public DbSet<ChinhSachMienGiam> ChinhSachMienGiams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Quan hệ HocSinh - HocSinhPhuHuynh
            modelBuilder.Entity<HocSinhPhuHuynh>()
                .HasOne(x => x.HocSinh)
                .WithMany(x => x.HocSinhPhuHuynhs)
                .HasForeignKey(x => x.IdHocSinh)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ PhuHuynh - HocSinhPhuHuynh
            modelBuilder.Entity<HocSinhPhuHuynh>()
                .HasOne(x => x.PhuHuynh)
                .WithMany(x => x.HocSinhPhuHuynhs)
                .HasForeignKey(x => x.IdPhuHuynh)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ HocSinh - ChuyenLop
            modelBuilder.Entity<ChuyenLop>()
                .HasOne(x => x.HocSinh)
                .WithMany(x => x.ChuyenLops)
                .HasForeignKey(x => x.IdHocSinh)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HocKy>()
                .HasOne(x => x.NamHoc)
                .WithMany(x => x.HocKys)
                .HasForeignKey(x => x.IdNamHoc)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HocSinh>()
                .HasIndex(x => x.IdTaiKhoan)
                .IsUnique()
                .HasFilter("[IdTaiKhoan] IS NOT NULL");

            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(x => x.IdChucVu)
                .IsUnique()
                .HasFilter("[IdChucVu] = 5");

            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(x => new { x.Username, x.IdChucVu })
                .IsUnique();

            modelBuilder.Entity<ChucVu>().HasData(
                new ChucVu { IdChucVu = 2, TenChucVu = "Giáo viên" },
                new ChucVu { IdChucVu = 3, TenChucVu = "Học sinh" },
                new ChucVu { IdChucVu = 4, TenChucVu = "Phụ huynh" },
                new ChucVu { IdChucVu = 5, TenChucVu = "System Admin" }
            );

            modelBuilder.Entity<MonHoc>().HasData(
                new MonHoc { IdMonHoc = 1, MaMon = "TOAN", TenMon = "Toán", SoTiet = 4 },
                new MonHoc { IdMonHoc = 2, MaMon = "VAN", TenMon = "Ngữ văn", SoTiet = 4 },
                new MonHoc { IdMonHoc = 3, MaMon = "ANH", TenMon = "Tiếng Anh", SoTiet = 4 },
                new MonHoc { IdMonHoc = 4, MaMon = "LY", TenMon = "Vật lý", SoTiet = 2 },
                new MonHoc { IdMonHoc = 5, MaMon = "HOA", TenMon = "Hóa học", SoTiet = 2 },
                new MonHoc { IdMonHoc = 6, MaMon = "SINH", TenMon = "Sinh học", SoTiet = 2 },
                new MonHoc { IdMonHoc = 7, MaMon = "SU", TenMon = "Lịch sử", SoTiet = 2 },
                new MonHoc { IdMonHoc = 8, MaMon = "DIA", TenMon = "Địa lý", SoTiet = 2 },
                new MonHoc { IdMonHoc = 9, MaMon = "TIN", TenMon = "Tin học", SoTiet = 2 },
                new MonHoc { IdMonHoc = 10, MaMon = "CN", TenMon = "Công nghệ", SoTiet = 2 },
                new MonHoc { IdMonHoc = 11, MaMon = "GDTC", TenMon = "Giáo dục thể chất", SoTiet = 2 },
                new MonHoc { IdMonHoc = 12, MaMon = "GDCD", TenMon = "Giáo dục công dân", SoTiet = 1 },
                new MonHoc { IdMonHoc = 13, MaMon = "NT", TenMon = "Nghệ thuật (Âm nhạc, Mỹ thuật)", SoTiet = 2 },
                new MonHoc { IdMonHoc = 14, MaMon = "HDTN", TenMon = "Hoạt động trải nghiệm, hướng nghiệp", SoTiet = 3 },
                new MonHoc { IdMonHoc = 15, MaMon = "GDDP", TenMon = "Nội dung giáo dục địa phương", SoTiet = 1 }
            );
        }
    }
    }

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class eschool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChucVus",
                columns: table => new
                {
                    IdChucVu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChucVu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucVus", x => x.IdChucVu);
                });

            migrationBuilder.CreateTable(
                name: "MonHocs",
                columns: table => new
                {
                    IdMonHoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaMon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenMon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoTiet = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonHocs", x => x.IdMonHoc);
                });

            migrationBuilder.CreateTable(
                name: "NamHocs",
                columns: table => new
                {
                    IdNamHoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNamHoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamHocs", x => x.IdNamHoc);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHoatDongs",
                columns: table => new
                {
                    IdNhatKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HanhDong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHoatDongs", x => x.IdNhatKy);
                });

            migrationBuilder.CreateTable(
                name: "TinTucSuKiens",
                columns: table => new
                {
                    IdTinTucSuKien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AnhMinhHoa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    LuotXem = table.Column<int>(type: "int", nullable: false),
                    ThoiGianDoc = table.Column<int>(type: "int", nullable: false),
                    LoaiTin = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinTucSuKiens", x => x.IdTinTucSuKien);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoans",
                columns: table => new
                {
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    BatBuocDoiMatKhau = table.Column<bool>(type: "bit", nullable: false),
                    IdChucVu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoans", x => x.IdTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoans_ChucVus_IdChucVu",
                        column: x => x.IdChucVu,
                        principalTable: "ChucVus",
                        principalColumn: "IdChucVu",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HocKys",
                columns: table => new
                {
                    IdHocKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenHocKy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdNamHoc = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocKys", x => x.IdHocKy);
                    table.ForeignKey(
                        name: "FK_HocKys_NamHocs_IdNamHoc",
                        column: x => x.IdNamHoc,
                        principalTable: "NamHocs",
                        principalColumn: "IdNamHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiaoViens",
                columns: table => new
                {
                    IdGiaoVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    IdMonHoc = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoViens", x => x.IdGiaoVien);
                    table.ForeignKey(
                        name: "FK_GiaoViens_MonHocs_IdMonHoc",
                        column: x => x.IdMonHoc,
                        principalTable: "MonHocs",
                        principalColumn: "IdMonHoc");
                    table.ForeignKey(
                        name: "FK_GiaoViens_TaiKhoans_IdTaiKhoan",
                        column: x => x.IdTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "IdTaiKhoan");
                });

            migrationBuilder.CreateTable(
                name: "PhuHuynhs",
                columns: table => new
                {
                    IdPhuHuynh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgheNghiep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuHuynhs", x => x.IdPhuHuynh);
                    table.ForeignKey(
                        name: "FK_PhuHuynhs_TaiKhoans_IdTaiKhoan",
                        column: x => x.IdTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "IdTaiKhoan");
                });

            migrationBuilder.CreateTable(
                name: "ThongBaos",
                columns: table => new
                {
                    IdThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoiTuongNhan = table.Column<int>(type: "int", nullable: false),
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaos", x => x.IdThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBaos_TaiKhoans_IdTaiKhoan",
                        column: x => x.IdTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "IdTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LopHocs",
                columns: table => new
                {
                    IdLop = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLop = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenLop = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Khoi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BuoiHoc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NamHoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdGiaoVienCN = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocs", x => x.IdLop);
                    table.ForeignKey(
                        name: "FK_LopHocs_GiaoViens_IdGiaoVienCN",
                        column: x => x.IdGiaoVienCN,
                        principalTable: "GiaoViens",
                        principalColumn: "IdGiaoVien");
                });

            migrationBuilder.CreateTable(
                name: "HocSinhs",
                columns: table => new
                {
                    IdHocSinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoiSinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DanToc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TonGiao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuocTich = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayNhapHoc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    DaDuyet = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    IdLopHoc = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocSinhs", x => x.IdHocSinh);
                    table.ForeignKey(
                        name: "FK_HocSinhs_LopHocs_IdLopHoc",
                        column: x => x.IdLopHoc,
                        principalTable: "LopHocs",
                        principalColumn: "IdLop");
                    table.ForeignKey(
                        name: "FK_HocSinhs_TaiKhoans_IdTaiKhoan",
                        column: x => x.IdTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "IdTaiKhoan");
                });

            migrationBuilder.CreateTable(
                name: "LichHocThayDois",
                columns: table => new
                {
                    IdThayDoi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdLop = table.Column<int>(type: "int", nullable: true),
                    TietBatDau = table.Column<int>(type: "int", nullable: true),
                    SoTiet = table.Column<int>(type: "int", nullable: true),
                    IsNghi = table.Column<bool>(type: "bit", nullable: false),
                    IdMonHocThayThe = table.Column<int>(type: "int", nullable: true),
                    IdGiaoVienThayThe = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichHocThayDois", x => x.IdThayDoi);
                    table.ForeignKey(
                        name: "FK_LichHocThayDois_GiaoViens_IdGiaoVienThayThe",
                        column: x => x.IdGiaoVienThayThe,
                        principalTable: "GiaoViens",
                        principalColumn: "IdGiaoVien");
                    table.ForeignKey(
                        name: "FK_LichHocThayDois_LopHocs_IdLop",
                        column: x => x.IdLop,
                        principalTable: "LopHocs",
                        principalColumn: "IdLop");
                    table.ForeignKey(
                        name: "FK_LichHocThayDois_MonHocs_IdMonHocThayThe",
                        column: x => x.IdMonHocThayThe,
                        principalTable: "MonHocs",
                        principalColumn: "IdMonHoc");
                });

            migrationBuilder.CreateTable(
                name: "PhongHocs",
                columns: table => new
                {
                    IdPhongHoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenPhong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SucChua = table.Column<int>(type: "int", nullable: false),
                    LoaiPhong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrangThietBi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    IdLop = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongHocs", x => x.IdPhongHoc);
                    table.ForeignKey(
                        name: "FK_PhongHocs_LopHocs_IdLop",
                        column: x => x.IdLop,
                        principalTable: "LopHocs",
                        principalColumn: "IdLop");
                });

            migrationBuilder.CreateTable(
                name: "ChinhSachMienGiams",
                columns: table => new
                {
                    IdMienGiam = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    PhanTramGiam = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HieuLuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChinhSachMienGiams", x => x.IdMienGiam);
                    table.ForeignKey(
                        name: "FK_ChinhSachMienGiams_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChuyenLops",
                columns: table => new
                {
                    IdChuyenLop = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdLopCu = table.Column<int>(type: "int", nullable: false),
                    IdLopMoi = table.Column<int>(type: "int", nullable: false),
                    NgayChuyen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuyenLops", x => x.IdChuyenLop);
                    table.ForeignKey(
                        name: "FK_ChuyenLops_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DangKyLops",
                columns: table => new
                {
                    IdDangKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdLop = table.Column<int>(type: "int", nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyLops", x => x.IdDangKy);
                    table.ForeignKey(
                        name: "FK_DangKyLops_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DangKyLops_LopHocs_IdLop",
                        column: x => x.IdLop,
                        principalTable: "LopHocs",
                        principalColumn: "IdLop",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiemDanhs",
                columns: table => new
                {
                    IdDiemDanh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdLop = table.Column<int>(type: "int", nullable: false),
                    NgayHoc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdTietHoc = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemDanhs", x => x.IdDiemDanh);
                    table.ForeignKey(
                        name: "FK_DiemDanhs_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiemDanhs_LopHocs_IdLop",
                        column: x => x.IdLop,
                        principalTable: "LopHocs",
                        principalColumn: "IdLop",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Diems",
                columns: table => new
                {
                    IdDiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdMonHoc = table.Column<int>(type: "int", nullable: false),
                    IdHocKy = table.Column<int>(type: "int", nullable: true),
                    IdNamHoc = table.Column<int>(type: "int", nullable: true),
                    HocKy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Diem15Phut = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Diem1Tiet = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiemGiuaKy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiemCuoiKy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiemTB = table.Column<decimal>(type: "decimal(4,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diems", x => x.IdDiem);
                    table.ForeignKey(
                        name: "FK_Diems_HocKys_IdHocKy",
                        column: x => x.IdHocKy,
                        principalTable: "HocKys",
                        principalColumn: "IdHocKy");
                    table.ForeignKey(
                        name: "FK_Diems_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Diems_MonHocs_IdMonHoc",
                        column: x => x.IdMonHoc,
                        principalTable: "MonHocs",
                        principalColumn: "IdMonHoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Diems_NamHocs_IdNamHoc",
                        column: x => x.IdNamHoc,
                        principalTable: "NamHocs",
                        principalColumn: "IdNamHoc");
                });

            migrationBuilder.CreateTable(
                name: "HocPhis",
                columns: table => new
                {
                    IdHocPhi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdNamHoc = table.Column<int>(type: "int", nullable: true),
                    IdHocKy = table.Column<int>(type: "int", nullable: true),
                    HocKy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    NgayDuKien = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HanDongTien = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayDong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhanTramMienGiam = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SoTienMienGiam = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    LyDoMienGiam = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocPhis", x => x.IdHocPhi);
                    table.ForeignKey(
                        name: "FK_HocPhis_HocKys_IdHocKy",
                        column: x => x.IdHocKy,
                        principalTable: "HocKys",
                        principalColumn: "IdHocKy");
                    table.ForeignKey(
                        name: "FK_HocPhis_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HocPhis_NamHocs_IdNamHoc",
                        column: x => x.IdNamHoc,
                        principalTable: "NamHocs",
                        principalColumn: "IdNamHoc");
                });

            migrationBuilder.CreateTable(
                name: "HocSinhPhuHuynhs",
                columns: table => new
                {
                    IdHocSinhPhuHuynh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdPhuHuynh = table.Column<int>(type: "int", nullable: false),
                    QuanHe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LaLienHeChinh = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocSinhPhuHuynhs", x => x.IdHocSinhPhuHuynh);
                    table.ForeignKey(
                        name: "FK_HocSinhPhuHuynhs_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HocSinhPhuHuynhs_PhuHuynhs_IdPhuHuynh",
                        column: x => x.IdPhuHuynh,
                        principalTable: "PhuHuynhs",
                        principalColumn: "IdPhuHuynh",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KyLuats",
                columns: table => new
                {
                    IdKyLuat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdGiaoVien = table.Column<int>(type: "int", nullable: true),
                    IdHocKy = table.Column<int>(type: "int", nullable: true),
                    NgayViPham = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HinhThuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KyLuats", x => x.IdKyLuat);
                    table.ForeignKey(
                        name: "FK_KyLuats_GiaoViens_IdGiaoVien",
                        column: x => x.IdGiaoVien,
                        principalTable: "GiaoViens",
                        principalColumn: "IdGiaoVien");
                    table.ForeignKey(
                        name: "FK_KyLuats_HocKys_IdHocKy",
                        column: x => x.IdHocKy,
                        principalTable: "HocKys",
                        principalColumn: "IdHocKy");
                    table.ForeignKey(
                        name: "FK_KyLuats_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuDiems",
                columns: table => new
                {
                    IdPhieuDiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdLop = table.Column<int>(type: "int", nullable: true),
                    IdHocKy = table.Column<int>(type: "int", nullable: true),
                    IdNamHoc = table.Column<int>(type: "int", nullable: true),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiLap = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuDiems", x => x.IdPhieuDiem);
                    table.ForeignKey(
                        name: "FK_PhieuDiems_HocKys_IdHocKy",
                        column: x => x.IdHocKy,
                        principalTable: "HocKys",
                        principalColumn: "IdHocKy");
                    table.ForeignKey(
                        name: "FK_PhieuDiems_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhieuDiems_LopHocs_IdLop",
                        column: x => x.IdLop,
                        principalTable: "LopHocs",
                        principalColumn: "IdLop");
                    table.ForeignKey(
                        name: "FK_PhieuDiems_NamHocs_IdNamHoc",
                        column: x => x.IdNamHoc,
                        principalTable: "NamHocs",
                        principalColumn: "IdNamHoc");
                    table.ForeignKey(
                        name: "FK_PhieuDiems_TaiKhoans_NguoiLap",
                        column: x => x.NguoiLap,
                        principalTable: "TaiKhoans",
                        principalColumn: "IdTaiKhoan");
                });

            migrationBuilder.CreateTable(
                name: "PhanCongGiangDays",
                columns: table => new
                {
                    IdPhanCong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdGiaoVien = table.Column<int>(type: "int", nullable: true),
                    IdMonHoc = table.Column<int>(type: "int", nullable: false),
                    IdLop = table.Column<int>(type: "int", nullable: false),
                    HocKy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NamHoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Thu = table.Column<int>(type: "int", nullable: true),
                    TietBatDau = table.Column<int>(type: "int", nullable: true),
                    SoTiet = table.Column<int>(type: "int", nullable: true),
                    IdPhongHoc = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCongGiangDays", x => x.IdPhanCong);
                    table.ForeignKey(
                        name: "FK_PhanCongGiangDays_GiaoViens_IdGiaoVien",
                        column: x => x.IdGiaoVien,
                        principalTable: "GiaoViens",
                        principalColumn: "IdGiaoVien");
                    table.ForeignKey(
                        name: "FK_PhanCongGiangDays_LopHocs_IdLop",
                        column: x => x.IdLop,
                        principalTable: "LopHocs",
                        principalColumn: "IdLop",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhanCongGiangDays_MonHocs_IdMonHoc",
                        column: x => x.IdMonHoc,
                        principalTable: "MonHocs",
                        principalColumn: "IdMonHoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhanCongGiangDays_PhongHocs_IdPhongHoc",
                        column: x => x.IdPhongHoc,
                        principalTable: "PhongHocs",
                        principalColumn: "IdPhongHoc");
                });

            migrationBuilder.CreateTable(
                name: "ThietBis",
                columns: table => new
                {
                    IdThietBi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTB = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenTB = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LoaiTB = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    IdPhongHoc = table.Column<int>(type: "int", nullable: false),
                    TinhTrang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayMua = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThietBis", x => x.IdThietBi);
                    table.ForeignKey(
                        name: "FK_ThietBis_PhongHocs_IdPhongHoc",
                        column: x => x.IdPhongHoc,
                        principalTable: "PhongHocs",
                        principalColumn: "IdPhongHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaoTris",
                columns: table => new
                {
                    IdBaoTri = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBaoTri = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdThietBi = table.Column<int>(type: "int", nullable: false),
                    NgayBaoTri = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChiPhi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KetQua = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoTris", x => x.IdBaoTri);
                    table.ForeignKey(
                        name: "FK_BaoTris_ThietBis_IdThietBi",
                        column: x => x.IdThietBi,
                        principalTable: "ThietBis",
                        principalColumn: "IdThietBi",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ChucVus",
                columns: new[] { "IdChucVu", "TenChucVu" },
                values: new object[,]
                {
                    { 2, "Giáo viên" },
                    { 3, "Học sinh" },
                    { 4, "Phụ huynh" },
                    { 5, "System Admin" }
                });

            migrationBuilder.InsertData(
                table: "MonHocs",
                columns: new[] { "IdMonHoc", "MaMon", "SoTiet", "TenMon" },
                values: new object[,]
                {
                    { 1, "TOAN", 4, "Toán" },
                    { 2, "VAN", 4, "Ngữ văn" },
                    { 3, "ANH", 4, "Tiếng Anh" },
                    { 4, "LY", 2, "Vật lý" },
                    { 5, "HOA", 2, "Hóa học" },
                    { 6, "SINH", 2, "Sinh học" },
                    { 7, "SU", 2, "Lịch sử" },
                    { 8, "DIA", 2, "Địa lý" },
                    { 9, "TIN", 2, "Tin học" },
                    { 10, "CN", 2, "Công nghệ" },
                    { 11, "GDTC", 2, "Giáo dục thể chất" },
                    { 12, "GDCD", 1, "Giáo dục công dân" },
                    { 13, "NT", 2, "Nghệ thuật (Âm nhạc, Mỹ thuật)" },
                    { 14, "HDTN", 3, "Hoạt động trải nghiệm, hướng nghiệp" },
                    { 15, "GDDP", 1, "Nội dung giáo dục địa phương" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaoTris_IdThietBi",
                table: "BaoTris",
                column: "IdThietBi");

            migrationBuilder.CreateIndex(
                name: "IX_ChinhSachMienGiams_IdHocSinh",
                table: "ChinhSachMienGiams",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenLops_IdHocSinh",
                table: "ChuyenLops",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyLops_IdHocSinh",
                table: "DangKyLops",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyLops_IdLop",
                table: "DangKyLops",
                column: "IdLop");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_IdHocSinh",
                table: "DiemDanhs",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_IdLop",
                table: "DiemDanhs",
                column: "IdLop");

            migrationBuilder.CreateIndex(
                name: "IX_Diems_IdHocKy",
                table: "Diems",
                column: "IdHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_Diems_IdHocSinh",
                table: "Diems",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_Diems_IdMonHoc",
                table: "Diems",
                column: "IdMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_Diems_IdNamHoc",
                table: "Diems",
                column: "IdNamHoc");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoViens_IdMonHoc",
                table: "GiaoViens",
                column: "IdMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoViens_IdTaiKhoan",
                table: "GiaoViens",
                column: "IdTaiKhoan",
                unique: true,
                filter: "[IdTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HocKys_IdNamHoc",
                table: "HocKys",
                column: "IdNamHoc");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhis_IdHocKy",
                table: "HocPhis",
                column: "IdHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhis_IdHocSinh",
                table: "HocPhis",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhis_IdNamHoc",
                table: "HocPhis",
                column: "IdNamHoc");

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhPhuHuynhs_IdHocSinh",
                table: "HocSinhPhuHuynhs",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhPhuHuynhs_IdPhuHuynh",
                table: "HocSinhPhuHuynhs",
                column: "IdPhuHuynh");

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhs_IdLopHoc",
                table: "HocSinhs",
                column: "IdLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhs_IdTaiKhoan",
                table: "HocSinhs",
                column: "IdTaiKhoan",
                unique: true,
                filter: "[IdTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KyLuats_IdGiaoVien",
                table: "KyLuats",
                column: "IdGiaoVien");

            migrationBuilder.CreateIndex(
                name: "IX_KyLuats_IdHocKy",
                table: "KyLuats",
                column: "IdHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_KyLuats_IdHocSinh",
                table: "KyLuats",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_LichHocThayDois_IdGiaoVienThayThe",
                table: "LichHocThayDois",
                column: "IdGiaoVienThayThe");

            migrationBuilder.CreateIndex(
                name: "IX_LichHocThayDois_IdLop",
                table: "LichHocThayDois",
                column: "IdLop");

            migrationBuilder.CreateIndex(
                name: "IX_LichHocThayDois_IdMonHocThayThe",
                table: "LichHocThayDois",
                column: "IdMonHocThayThe");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocs_IdGiaoVienCN",
                table: "LopHocs",
                column: "IdGiaoVienCN");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiangDays_IdGiaoVien",
                table: "PhanCongGiangDays",
                column: "IdGiaoVien");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiangDays_IdLop",
                table: "PhanCongGiangDays",
                column: "IdLop");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiangDays_IdMonHoc",
                table: "PhanCongGiangDays",
                column: "IdMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiangDays_IdPhongHoc",
                table: "PhanCongGiangDays",
                column: "IdPhongHoc");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiems_IdHocKy",
                table: "PhieuDiems",
                column: "IdHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiems_IdHocSinh",
                table: "PhieuDiems",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiems_IdLop",
                table: "PhieuDiems",
                column: "IdLop");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiems_IdNamHoc",
                table: "PhieuDiems",
                column: "IdNamHoc");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiems_NguoiLap",
                table: "PhieuDiems",
                column: "NguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhongHocs_IdLop",
                table: "PhongHocs",
                column: "IdLop");

            migrationBuilder.CreateIndex(
                name: "IX_PhuHuynhs_IdTaiKhoan",
                table: "PhuHuynhs",
                column: "IdTaiKhoan",
                unique: true,
                filter: "[IdTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans",
                column: "IdChucVu",
                unique: true,
                filter: "[IdChucVu] = 5");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_Username_IdChucVu",
                table: "TaiKhoans",
                columns: new[] { "Username", "IdChucVu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThietBis_IdPhongHoc",
                table: "ThietBis",
                column: "IdPhongHoc");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaos_IdTaiKhoan",
                table: "ThongBaos",
                column: "IdTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaoTris");

            migrationBuilder.DropTable(
                name: "ChinhSachMienGiams");

            migrationBuilder.DropTable(
                name: "ChuyenLops");

            migrationBuilder.DropTable(
                name: "DangKyLops");

            migrationBuilder.DropTable(
                name: "DiemDanhs");

            migrationBuilder.DropTable(
                name: "Diems");

            migrationBuilder.DropTable(
                name: "HocPhis");

            migrationBuilder.DropTable(
                name: "HocSinhPhuHuynhs");

            migrationBuilder.DropTable(
                name: "KyLuats");

            migrationBuilder.DropTable(
                name: "LichHocThayDois");

            migrationBuilder.DropTable(
                name: "NhatKyHoatDongs");

            migrationBuilder.DropTable(
                name: "PhanCongGiangDays");

            migrationBuilder.DropTable(
                name: "PhieuDiems");

            migrationBuilder.DropTable(
                name: "ThongBaos");

            migrationBuilder.DropTable(
                name: "TinTucSuKiens");

            migrationBuilder.DropTable(
                name: "ThietBis");

            migrationBuilder.DropTable(
                name: "PhuHuynhs");

            migrationBuilder.DropTable(
                name: "HocKys");

            migrationBuilder.DropTable(
                name: "HocSinhs");

            migrationBuilder.DropTable(
                name: "PhongHocs");

            migrationBuilder.DropTable(
                name: "NamHocs");

            migrationBuilder.DropTable(
                name: "LopHocs");

            migrationBuilder.DropTable(
                name: "GiaoViens");

            migrationBuilder.DropTable(
                name: "MonHocs");

            migrationBuilder.DropTable(
                name: "TaiKhoans");

            migrationBuilder.DropTable(
                name: "ChucVus");
        }
    }
}

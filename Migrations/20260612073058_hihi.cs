using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class hihi : Migration
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
                name: "TaiKhoans",
                columns: table => new
                {
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
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
                name: "GiaoViens",
                columns: table => new
                {
                    IdGiaoVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoViens", x => x.IdGiaoVien);
                    table.ForeignKey(
                        name: "FK_GiaoViens_TaiKhoans_IdTaiKhoan",
                        column: x => x.IdTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "IdTaiKhoan");
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
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocSinhs", x => x.IdHocSinh);
                    table.ForeignKey(
                        name: "FK_HocSinhs_TaiKhoans_IdTaiKhoan",
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
                name: "Diems",
                columns: table => new
                {
                    IdDiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    IdMonHoc = table.Column<int>(type: "int", nullable: false),
                    HocKy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiemMieng = table.Column<double>(type: "float", nullable: true),
                    Diem15Phut = table.Column<double>(type: "float", nullable: true),
                    Diem1Tiet = table.Column<double>(type: "float", nullable: true),
                    DiemThi = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diems", x => x.IdDiem);
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
                });

            migrationBuilder.CreateTable(
                name: "HocPhis",
                columns: table => new
                {
                    IdHocPhi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    HocKy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayDong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocPhis", x => x.IdHocPhi);
                    table.ForeignKey(
                        name: "FK_HocPhis_HocSinhs_IdHocSinh",
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
                name: "PhanCongGiangDays",
                columns: table => new
                {
                    IdPhanCong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdGiaoVien = table.Column<int>(type: "int", nullable: false),
                    IdMonHoc = table.Column<int>(type: "int", nullable: false),
                    IdLop = table.Column<int>(type: "int", nullable: false),
                    HocKy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NamHoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCongGiangDays", x => x.IdPhanCong);
                    table.ForeignKey(
                        name: "FK_PhanCongGiangDays_GiaoViens_IdGiaoVien",
                        column: x => x.IdGiaoVien,
                        principalTable: "GiaoViens",
                        principalColumn: "IdGiaoVien",
                        onDelete: ReferentialAction.Cascade);
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
                });

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
                name: "IX_Diems_IdHocSinh",
                table: "Diems",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_Diems_IdMonHoc",
                table: "Diems",
                column: "IdMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoViens_IdTaiKhoan",
                table: "GiaoViens",
                column: "IdTaiKhoan",
                unique: true,
                filter: "[IdTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhis_IdHocSinh",
                table: "HocPhis",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhs_IdTaiKhoan",
                table: "HocSinhs",
                column: "IdTaiKhoan",
                unique: true,
                filter: "[IdTaiKhoan] IS NOT NULL");

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
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans",
                column: "IdChucVu");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaos_IdTaiKhoan",
                table: "ThongBaos",
                column: "IdTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DangKyLops");

            migrationBuilder.DropTable(
                name: "DiemDanhs");

            migrationBuilder.DropTable(
                name: "Diems");

            migrationBuilder.DropTable(
                name: "HocPhis");

            migrationBuilder.DropTable(
                name: "PhanCongGiangDays");

            migrationBuilder.DropTable(
                name: "ThongBaos");

            migrationBuilder.DropTable(
                name: "HocSinhs");

            migrationBuilder.DropTable(
                name: "LopHocs");

            migrationBuilder.DropTable(
                name: "MonHocs");

            migrationBuilder.DropTable(
                name: "GiaoViens");

            migrationBuilder.DropTable(
                name: "TaiKhoans");

            migrationBuilder.DropTable(
                name: "ChucVus");
        }
    }
}

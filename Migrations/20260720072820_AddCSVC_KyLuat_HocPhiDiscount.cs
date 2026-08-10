using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddCSVC_KyLuat_HocPhiDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LyDoMienGiam",
                table: "HocPhis",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PhanTramMienGiam",
                table: "HocPhis",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienMienGiam",
                table: "HocPhis",
                type: "decimal(12,2)",
                nullable: true);

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
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongHocs", x => x.IdPhongHoc);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KyLuats");

            migrationBuilder.DropTable(
                name: "PhongHocs");

            migrationBuilder.DropColumn(
                name: "LyDoMienGiam",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "PhanTramMienGiam",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "SoTienMienGiam",
                table: "HocPhis");
        }
    }
}

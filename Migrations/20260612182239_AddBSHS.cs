using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddBSHS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SDT",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GioiTinh",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiaChi",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DanToc",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdLopHoc",
                table: "HocSinhs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayNhapHoc",
                table: "HocSinhs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "HocSinhs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiSinh",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuocTich",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TonGiao",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true);

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
                name: "PhuHuynhs",
                columns: table => new
                {
                    IdPhuHuynh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgheNghiep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuHuynhs", x => x.IdPhuHuynh);
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

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhs_IdLopHoc",
                table: "HocSinhs",
                column: "IdLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenLops_IdHocSinh",
                table: "ChuyenLops",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhPhuHuynhs_IdHocSinh",
                table: "HocSinhPhuHuynhs",
                column: "IdHocSinh");

            migrationBuilder.CreateIndex(
                name: "IX_HocSinhPhuHuynhs_IdPhuHuynh",
                table: "HocSinhPhuHuynhs",
                column: "IdPhuHuynh");

            migrationBuilder.AddForeignKey(
                name: "FK_HocSinhs_LopHocs_IdLopHoc",
                table: "HocSinhs",
                column: "IdLopHoc",
                principalTable: "LopHocs",
                principalColumn: "IdLop");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HocSinhs_LopHocs_IdLopHoc",
                table: "HocSinhs");

            migrationBuilder.DropTable(
                name: "ChuyenLops");

            migrationBuilder.DropTable(
                name: "HocSinhPhuHuynhs");

            migrationBuilder.DropTable(
                name: "PhuHuynhs");

            migrationBuilder.DropIndex(
                name: "IX_HocSinhs_IdLopHoc",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "DanToc",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "IdLopHoc",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "NgayNhapHoc",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "NoiSinh",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "QuocTich",
                table: "HocSinhs");

            migrationBuilder.DropColumn(
                name: "TonGiao",
                table: "HocSinhs");

            migrationBuilder.AlterColumn<string>(
                name: "SDT",
                table: "HocSinhs",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GioiTinh",
                table: "HocSinhs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "HocSinhs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiaChi",
                table: "HocSinhs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}

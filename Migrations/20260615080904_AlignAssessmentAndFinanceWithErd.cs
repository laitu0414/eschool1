using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AlignAssessmentAndFinanceWithErd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiemMieng",
                table: "Diems",
                newName: "DiemGiuaKy");

            migrationBuilder.RenameColumn(
                name: "DiemThi",
                table: "Diems",
                newName: "DiemCuoiKy");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "HocPhis",
                newName: "TrangThaiCu");

            migrationBuilder.AddColumn<int>(
                name: "TrangThai",
                table: "HocPhis",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE [HocPhis]
                SET [TrangThai] = CASE
                    WHEN [TrangThaiCu] IN (N'Đã đóng', N'Đã thanh toán', N'1') THEN 1
                    WHEN [TrangThaiCu] IN (N'Quá hạn', N'2') THEN 2
                    ELSE 0
                END
                """);

            migrationBuilder.DropColumn(
                name: "TrangThaiCu",
                table: "HocPhis");

            migrationBuilder.AlterColumn<decimal>(
                name: "SoTien",
                table: "HocPhis",
                type: "decimal(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "HocPhis",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HanDongTien",
                table: "HocPhis",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdHocKy",
                table: "HocPhis",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdNamHoc",
                table: "HocPhis",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuKien",
                table: "HocPhis",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhuongThuc",
                table: "HocPhis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Diem1Tiet",
                table: "Diems",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Diem15Phut",
                table: "Diems",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiemCuoiKy",
                table: "Diems",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiemGiuaKy",
                table: "Diems",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiemTB",
                table: "Diems",
                type: "decimal(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdHocKy",
                table: "Diems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdNamHoc",
                table: "Diems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTietHoc",
                table: "DiemDanhs",
                type: "int",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_HocPhis_IdHocKy",
                table: "HocPhis",
                column: "IdHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhis_IdNamHoc",
                table: "HocPhis",
                column: "IdNamHoc");

            migrationBuilder.CreateIndex(
                name: "IX_Diems_IdHocKy",
                table: "Diems",
                column: "IdHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_Diems_IdNamHoc",
                table: "Diems",
                column: "IdNamHoc");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Diems_HocKys_IdHocKy",
                table: "Diems",
                column: "IdHocKy",
                principalTable: "HocKys",
                principalColumn: "IdHocKy");

            migrationBuilder.AddForeignKey(
                name: "FK_Diems_NamHocs_IdNamHoc",
                table: "Diems",
                column: "IdNamHoc",
                principalTable: "NamHocs",
                principalColumn: "IdNamHoc");

            migrationBuilder.AddForeignKey(
                name: "FK_HocPhis_HocKys_IdHocKy",
                table: "HocPhis",
                column: "IdHocKy",
                principalTable: "HocKys",
                principalColumn: "IdHocKy");

            migrationBuilder.AddForeignKey(
                name: "FK_HocPhis_NamHocs_IdNamHoc",
                table: "HocPhis",
                column: "IdNamHoc",
                principalTable: "NamHocs",
                principalColumn: "IdNamHoc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diems_HocKys_IdHocKy",
                table: "Diems");

            migrationBuilder.DropForeignKey(
                name: "FK_Diems_NamHocs_IdNamHoc",
                table: "Diems");

            migrationBuilder.DropForeignKey(
                name: "FK_HocPhis_HocKys_IdHocKy",
                table: "HocPhis");

            migrationBuilder.DropForeignKey(
                name: "FK_HocPhis_NamHocs_IdNamHoc",
                table: "HocPhis");

            migrationBuilder.DropTable(
                name: "PhieuDiems");

            migrationBuilder.DropIndex(
                name: "IX_HocPhis_IdHocKy",
                table: "HocPhis");

            migrationBuilder.DropIndex(
                name: "IX_HocPhis_IdNamHoc",
                table: "HocPhis");

            migrationBuilder.DropIndex(
                name: "IX_Diems_IdHocKy",
                table: "Diems");

            migrationBuilder.DropIndex(
                name: "IX_Diems_IdNamHoc",
                table: "Diems");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "HanDongTien",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "IdHocKy",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "IdNamHoc",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "NgayDuKien",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "PhuongThuc",
                table: "HocPhis");

            migrationBuilder.DropColumn(
                name: "DiemTB",
                table: "Diems");

            migrationBuilder.DropColumn(
                name: "IdHocKy",
                table: "Diems");

            migrationBuilder.DropColumn(
                name: "IdNamHoc",
                table: "Diems");

            migrationBuilder.DropColumn(
                name: "IdTietHoc",
                table: "DiemDanhs");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "HocPhis",
                newName: "TrangThaiMoi");

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "HocPhis",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [HocPhis]
                SET [TrangThai] = CASE
                    WHEN [TrangThaiMoi] = 1 THEN N'Đã đóng'
                    WHEN [TrangThaiMoi] = 2 THEN N'Quá hạn'
                    ELSE N'Chưa đóng'
                END
                """);

            migrationBuilder.DropColumn(
                name: "TrangThaiMoi",
                table: "HocPhis");

            migrationBuilder.AlterColumn<decimal>(
                name: "SoTien",
                table: "HocPhis",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Diem1Tiet",
                table: "Diems",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Diem15Phut",
                table: "Diems",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "DiemGiuaKy",
                table: "Diems",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "DiemCuoiKy",
                table: "Diems",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "DiemGiuaKy",
                table: "Diems",
                newName: "DiemMieng");

            migrationBuilder.RenameColumn(
                name: "DiemCuoiKy",
                table: "Diems",
                newName: "DiemThi");
        }
    }
}

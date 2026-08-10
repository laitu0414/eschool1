using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddLichHocThayDoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichHocThayDois");
        }
    }
}

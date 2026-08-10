using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddThietBi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_ThietBis_IdPhongHoc",
                table: "ThietBis",
                column: "IdPhongHoc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThietBis");
        }
    }
}

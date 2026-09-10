using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using eSchool.Models;

#nullable disable

namespace eSchool.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260905000000_AddIdMonHocToGiaoVien")]
    /// <inheritdoc />
    public partial class AddIdMonHocToGiaoVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdMonHoc",
                table: "GiaoViens",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiaoViens_IdMonHoc",
                table: "GiaoViens",
                column: "IdMonHoc");

            migrationBuilder.AddForeignKey(
                name: "FK_GiaoViens_MonHocs_IdMonHoc",
                table: "GiaoViens",
                column: "IdMonHoc",
                principalTable: "MonHocs",
                principalColumn: "IdMonHoc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GiaoViens_MonHocs_IdMonHoc",
                table: "GiaoViens");

            migrationBuilder.DropIndex(
                name: "IX_GiaoViens_IdMonHoc",
                table: "GiaoViens");

            migrationBuilder.DropColumn(
                name: "IdMonHoc",
                table: "GiaoViens");
        }
    }
}
using eSchool.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260910210000_AddAnhDaiDienToGiaoVien")]
    public partial class AddAnhDaiDienToGiaoVien : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "GiaoViens",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "GiaoViens");
        }
    }
}

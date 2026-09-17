using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using eSchool.Models;
namespace eSchool.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20260917090000_AddGraduationStatus")]
public partial class AddGraduationStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("DaTotNghiep", "HocSinhs", "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>("NgayTotNghiep", "HocSinhs", "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>("NamHocTotNghiep", "HocSinhs", "nvarchar(20)", maxLength: 20, nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("DaTotNghiep", "HocSinhs");
        migrationBuilder.DropColumn("NgayTotNghiep", "HocSinhs");
        migrationBuilder.DropColumn("NamHocTotNghiep", "HocSinhs");
    }
}
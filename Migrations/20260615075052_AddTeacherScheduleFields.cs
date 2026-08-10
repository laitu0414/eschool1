using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherScheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoTiet",
                table: "PhanCongGiangDays",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Thu",
                table: "PhanCongGiangDays",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TietBatDau",
                table: "PhanCongGiangDays",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoTiet",
                table: "PhanCongGiangDays");

            migrationBuilder.DropColumn(
                name: "Thu",
                table: "PhanCongGiangDays");

            migrationBuilder.DropColumn(
                name: "TietBatDau",
                table: "PhanCongGiangDays");
        }
    }
}

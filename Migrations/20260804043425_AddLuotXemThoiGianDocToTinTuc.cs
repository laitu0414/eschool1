using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddLuotXemThoiGianDocToTinTuc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LuotXem",
                table: "TinTucSuKiens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThoiGianDoc",
                table: "TinTucSuKiens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdPhongHoc",
                table: "PhanCongGiangDays",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "BaoTris",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiangDays_IdPhongHoc",
                table: "PhanCongGiangDays",
                column: "IdPhongHoc");

            migrationBuilder.AddForeignKey(
                name: "FK_PhanCongGiangDays_PhongHocs_IdPhongHoc",
                table: "PhanCongGiangDays",
                column: "IdPhongHoc",
                principalTable: "PhongHocs",
                principalColumn: "IdPhongHoc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhanCongGiangDays_PhongHocs_IdPhongHoc",
                table: "PhanCongGiangDays");

            migrationBuilder.DropIndex(
                name: "IX_PhanCongGiangDays_IdPhongHoc",
                table: "PhanCongGiangDays");

            migrationBuilder.DropColumn(
                name: "LuotXem",
                table: "TinTucSuKiens");

            migrationBuilder.DropColumn(
                name: "ThoiGianDoc",
                table: "TinTucSuKiens");

            migrationBuilder.DropColumn(
                name: "IdPhongHoc",
                table: "PhanCongGiangDays");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "BaoTris");
        }
    }
}

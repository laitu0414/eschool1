using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullGiaoVienInPhanCong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhanCongGiangDays_GiaoViens_IdGiaoVien",
                table: "PhanCongGiangDays");

            migrationBuilder.AlterColumn<int>(
                name: "IdGiaoVien",
                table: "PhanCongGiangDays",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_PhanCongGiangDays_GiaoViens_IdGiaoVien",
                table: "PhanCongGiangDays",
                column: "IdGiaoVien",
                principalTable: "GiaoViens",
                principalColumn: "IdGiaoVien");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IdGiaoVien",
                table: "PhanCongGiangDays",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PhanCongGiangDays_GiaoViens_IdGiaoVien",
                table: "PhanCongGiangDays",
                column: "IdGiaoVien",
                principalTable: "GiaoViens",
                principalColumn: "IdGiaoVien",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

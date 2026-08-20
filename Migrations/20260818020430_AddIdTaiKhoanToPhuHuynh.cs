using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddIdTaiKhoanToPhuHuynh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTaiKhoan",
                table: "PhuHuynhs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhuHuynhs_IdTaiKhoan",
                table: "PhuHuynhs",
                column: "IdTaiKhoan",
                unique: true,
                filter: "[IdTaiKhoan] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PhuHuynhs_TaiKhoans_IdTaiKhoan",
                table: "PhuHuynhs",
                column: "IdTaiKhoan",
                principalTable: "TaiKhoans",
                principalColumn: "IdTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhuHuynhs_TaiKhoans_IdTaiKhoan",
                table: "PhuHuynhs");

            migrationBuilder.DropIndex(
                name: "IX_PhuHuynhs_IdTaiKhoan",
                table: "PhuHuynhs");

            migrationBuilder.DropColumn(
                name: "IdTaiKhoan",
                table: "PhuHuynhs");
        }
    }
}

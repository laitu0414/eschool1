using eSchool.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260909002000_EnsureSingleSystemAdminAccount")]
    public partial class EnsureSingleSystemAdminAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF (SELECT COUNT(*) FROM [TaiKhoans] WHERE [IdChucVu] = 5) > 1
                    THROW 51000, N'Hệ thống đang có nhiều tài khoản System Admin. Hãy giữ lại một tài khoản trước khi áp dụng thay đổi này.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans",
                column: "IdChucVu",
                unique: true,
                filter: "[IdChucVu] = 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans",
                column: "IdChucVu");
        }
    }
}

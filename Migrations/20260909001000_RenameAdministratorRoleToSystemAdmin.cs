using eSchool.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260909001000_RenameAdministratorRoleToSystemAdmin")]
    public partial class RenameAdministratorRoleToSystemAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [ChucVus] WHERE [IdChucVu] = 5)
                BEGIN
                    SET IDENTITY_INSERT [ChucVus] ON;
                    INSERT INTO [ChucVus] ([IdChucVu], [TenChucVu]) VALUES (5, N'System Admin');
                    SET IDENTITY_INSERT [ChucVus] OFF;
                END;

                UPDATE [ChucVus]
                SET [TenChucVu] = N'System Admin'
                WHERE [IdChucVu] = 5;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [ChucVus]
                SET [TenChucVu] = N'Cán bộ đào tạo'
                WHERE [IdChucVu] = 5;
                """);
        }
    }
}

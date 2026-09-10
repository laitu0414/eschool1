using eSchool.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260909003000_MoveDefaultSystemAdminToRole5")]
    public partial class MoveDefaultSystemAdminToRole5 : Migration
    {
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

                UPDATE [ChucVus]
                SET [TenChucVu] = N'Cán bộ đào tạo'
                WHERE [IdChucVu] = 1;

                IF EXISTS (SELECT 1 FROM [TaiKhoans] WHERE [Username] = N'admin' AND [IdChucVu] = 1)
                    AND NOT EXISTS (SELECT 1 FROM [TaiKhoans] WHERE [IdChucVu] = 5)
                BEGIN
                    UPDATE [TaiKhoans]
                    SET [IdChucVu] = 5
                    WHERE [Username] = N'admin' AND [IdChucVu] = 1;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddTaiKhoanBatBuocDoiMatKhau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.TaiKhoans', N'BatBuocDoiMatKhau') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[TaiKhoans]
                    ADD [BatBuocDoiMatKhau] bit NOT NULL
                        CONSTRAINT [DF_TaiKhoans_BatBuocDoiMatKhau] DEFAULT 0
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.TaiKhoans', N'BatBuocDoiMatKhau') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[TaiKhoans] DROP CONSTRAINT IF EXISTS [DF_TaiKhoans_BatBuocDoiMatKhau]
                    ALTER TABLE [dbo].[TaiKhoans] DROP COLUMN [BatBuocDoiMatKhau]
                END
                """);
        }
    }
}

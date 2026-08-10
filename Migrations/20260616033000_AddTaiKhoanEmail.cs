using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddTaiKhoanEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.TaiKhoans', N'Email') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[TaiKhoans]
                    ADD [Email] nvarchar(100) NULL
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.TaiKhoans', N'Email') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[TaiKhoans] DROP COLUMN [Email]
                END
                """);
        }
    }
}

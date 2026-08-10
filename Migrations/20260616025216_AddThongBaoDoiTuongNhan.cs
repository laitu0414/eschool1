using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddThongBaoDoiTuongNhan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ThongBaos', N'DoiTuongNhan') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[ThongBaos]
                    ADD [DoiTuongNhan] int NOT NULL
                        CONSTRAINT [DF_ThongBaos_DoiTuongNhan] DEFAULT 0
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ThongBaos', N'DoiTuongNhan') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[ThongBaos] DROP CONSTRAINT IF EXISTS [DF_ThongBaos_DoiTuongNhan]
                    ALTER TABLE [dbo].[ThongBaos] DROP COLUMN [DoiTuongNhan]
                END
                """);
        }
    }
}

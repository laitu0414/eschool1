using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddTinTucSuKienTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SDT",
                table: "HocSinhs",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "HocSinhs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.TinTucSuKiens', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[TinTucSuKiens]
                    (
                        [IdTinTucSuKien] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [TieuDe] NVARCHAR(200) NOT NULL,
                        [MoTa] NVARCHAR(1000) NOT NULL,
                        [DuongDan] NVARCHAR(500) NOT NULL,
                        [AnhMinhHoa] NVARCHAR(255) NULL,
                        [NgayTao] DATETIME2 NOT NULL CONSTRAINT [DF_TinTucSuKiens_NgayTao] DEFAULT GETDATE(),
                        [TrangThai] BIT NOT NULL CONSTRAINT [DF_TinTucSuKiens_TrangThai] DEFAULT 1
                    )
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.TinTucSuKiens', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[TinTucSuKiens]
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "SDT",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "HocSinhs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}

using eSchool.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260910190000_AllowSharedUsernameAcrossRoles")]
    public partial class AllowSharedUsernameAcrossRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @sql nvarchar(max) = N'';

                SELECT @sql = @sql + CASE
                    WHEN keyConstraint.[name] IS NOT NULL
                        THEN N'ALTER TABLE [dbo].[TaiKhoans] DROP CONSTRAINT ' + QUOTENAME(keyConstraint.[name]) + N';'
                    ELSE N'DROP INDEX ' + QUOTENAME(idx.[name]) + N' ON [dbo].[TaiKhoans];'
                END + NCHAR(10)
                FROM sys.indexes AS idx
                LEFT JOIN sys.key_constraints AS keyConstraint
                    ON keyConstraint.parent_object_id = idx.object_id
                    AND keyConstraint.unique_index_id = idx.index_id
                WHERE idx.object_id = OBJECT_ID(N'[dbo].[TaiKhoans]')
                  AND idx.is_unique = 1
                  AND idx.is_primary_key = 0
                  AND (SELECT COUNT(*)
                       FROM sys.index_columns AS indexColumn
                       WHERE indexColumn.object_id = idx.object_id
                         AND indexColumn.index_id = idx.index_id
                         AND indexColumn.key_ordinal > 0) = 1
                  AND EXISTS
                  (
                      SELECT 1
                      FROM sys.index_columns AS indexColumn
                      INNER JOIN sys.columns AS columnInfo
                          ON columnInfo.object_id = indexColumn.object_id
                          AND columnInfo.column_id = indexColumn.column_id
                      WHERE indexColumn.object_id = idx.object_id
                        AND indexColumn.index_id = idx.index_id
                        AND indexColumn.key_ordinal = 1
                        AND columnInfo.[name] = N'Username'
                  );

                IF LEN(@sql) > 0
                    EXEC sys.sp_executesql @sql;
                """);
            // Các tài khoản học sinh hiện có thường đang dùng Mã HS. Chuyển sang
            // SĐT khi SĐT đó là duy nhất trong vai trò học sinh; SĐT trùng với
            // phụ huynh vẫn hợp lệ vì hai vai trò dùng hai bản ghi khác nhau.
            migrationBuilder.Sql("""
                UPDATE tk
                SET tk.Username = LTRIM(RTRIM(hs.SDT)),
                    tk.Email = COALESCE(tk.Email, hs.Email)
                FROM [TaiKhoans] AS tk
                INNER JOIN [HocSinhs] AS hs ON hs.IdTaiKhoan = tk.IdTaiKhoan
                WHERE tk.IdChucVu = 3
                  AND hs.SDT IS NOT NULL
                  AND LTRIM(RTRIM(hs.SDT)) <> N''
                  AND LEN(LTRIM(RTRIM(hs.SDT))) = 10
                  AND LEFT(LTRIM(RTRIM(hs.SDT)), 1) = N'0'
                  AND LTRIM(RTRIM(hs.SDT)) NOT LIKE N'%[^0-9]%'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [TaiKhoans] AS otherTk
                      WHERE otherTk.IdChucVu = 3
                        AND otherTk.Username = LTRIM(RTRIM(hs.SDT))
                        AND otherTk.IdTaiKhoan <> tk.IdTaiKhoan
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [HocSinhs] AS otherHs
                      INNER JOIN [TaiKhoans] AS otherTk ON otherTk.IdTaiKhoan = otherHs.IdTaiKhoan
                      WHERE otherTk.IdChucVu = 3
                        AND otherTk.IdTaiKhoan <> tk.IdTaiKhoan
                        AND LTRIM(RTRIM(otherHs.SDT)) = LTRIM(RTRIM(hs.SDT))
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_Username_IdChucVu",
                table: "TaiKhoans",
                columns: new[] { "Username", "IdChucVu" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaiKhoans_Username_IdChucVu",
                table: "TaiKhoans");
        }
    }
}

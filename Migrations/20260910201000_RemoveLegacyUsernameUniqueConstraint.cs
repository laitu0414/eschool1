using eSchool.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260910201000_RemoveLegacyUsernameUniqueConstraint")]
    public partial class RemoveLegacyUsernameUniqueConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older databases may still have a unique index or constraint on
            // Username alone. The valid uniqueness rule is Username + role.
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

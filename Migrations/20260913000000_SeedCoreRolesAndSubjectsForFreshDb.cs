using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    public partial class SeedCoreRolesAndSubjectsForFreshDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [ChucVus] WHERE [IdChucVu] = 2)
                    INSERT INTO [ChucVus] ([IdChucVu], [TenChucVu]) VALUES (2, N'Giáo viên');
                IF NOT EXISTS (SELECT 1 FROM [ChucVus] WHERE [IdChucVu] = 3)
                    INSERT INTO [ChucVus] ([IdChucVu], [TenChucVu]) VALUES (3, N'Học sinh');
                IF NOT EXISTS (SELECT 1 FROM [ChucVus] WHERE [IdChucVu] = 4)
                    INSERT INTO [ChucVus] ([IdChucVu], [TenChucVu]) VALUES (4, N'Phụ huynh');
                IF NOT EXISTS (SELECT 1 FROM [ChucVus] WHERE [IdChucVu] = 5)
                    INSERT INTO [ChucVus] ([IdChucVu], [TenChucVu]) VALUES (5, N'System Admin');
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'TOAN')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('TOAN', N'Toán', 4);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'VAN')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('VAN', N'Ngữ văn', 4);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'ANH')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('ANH', N'Tiếng Anh', 4);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'LY')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('LY', N'Vật lý', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'HOA')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('HOA', N'Hóa học', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'SINH')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('SINH', N'Sinh học', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'SU')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('SU', N'Lịch sử', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'DIA')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('DIA', N'Địa lý', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'TIN')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('TIN', N'Tin học', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'CN')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('CN', N'Công nghệ', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'GDTC')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('GDTC', N'Giáo dục thể chất', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'GDCD')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('GDCD', N'Giáo dục công dân', 1);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'NT')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('NT', N'Nghệ thuật (Âm nhạc, Mỹ thuật)', 2);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'HDTN')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('HDTN', N'Hoạt động trải nghiệm, hướng nghiệp', 3);
                IF NOT EXISTS (SELECT 1 FROM [MonHocs] WHERE [MaMon] = 'GDDP')
                    INSERT INTO [MonHocs] ([MaMon], [TenMon], [SoTiet]) VALUES ('GDDP', N'Nội dung giáo dục địa phương', 1);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [MonHocs]
                WHERE [MaMon] IN ('TOAN','VAN','ANH','LY','HOA','SINH','SU','DIA','TIN','CN','GDTC','GDCD','NT','HDTN','GDDP');

                DELETE FROM [ChucVus]
                WHERE [IdChucVu] IN (2,3,4,5);
            ");
        }
    }
}

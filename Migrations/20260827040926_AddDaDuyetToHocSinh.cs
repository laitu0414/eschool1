using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddDaDuyetToHocSinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DaDuyet",
                table: "HocSinhs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaDuyet",
                table: "HocSinhs");
        }
    }
}

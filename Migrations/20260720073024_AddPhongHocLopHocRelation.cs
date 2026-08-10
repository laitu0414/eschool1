using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddPhongHocLopHocRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdLop",
                table: "PhongHocs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhongHocs_IdLop",
                table: "PhongHocs",
                column: "IdLop");

            migrationBuilder.AddForeignKey(
                name: "FK_PhongHocs_LopHocs_IdLop",
                table: "PhongHocs",
                column: "IdLop",
                principalTable: "LopHocs",
                principalColumn: "IdLop");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhongHocs_LopHocs_IdLop",
                table: "PhongHocs");

            migrationBuilder.DropIndex(
                name: "IX_PhongHocs_IdLop",
                table: "PhongHocs");

            migrationBuilder.DropColumn(
                name: "IdLop",
                table: "PhongHocs");
        }
    }
}

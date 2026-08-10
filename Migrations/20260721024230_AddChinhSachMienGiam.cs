using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddChinhSachMienGiam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChinhSachMienGiams",
                columns: table => new
                {
                    IdMienGiam = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHocSinh = table.Column<int>(type: "int", nullable: false),
                    PhanTramGiam = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HieuLuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChinhSachMienGiams", x => x.IdMienGiam);
                    table.ForeignKey(
                        name: "FK_ChinhSachMienGiams_HocSinhs_IdHocSinh",
                        column: x => x.IdHocSinh,
                        principalTable: "HocSinhs",
                        principalColumn: "IdHocSinh",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChinhSachMienGiams_IdHocSinh",
                table: "ChinhSachMienGiams",
                column: "IdHocSinh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChinhSachMienGiams");
        }
    }
}

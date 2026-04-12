using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evenote.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfPathNota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "Notas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "Notas");
        }
    }
}

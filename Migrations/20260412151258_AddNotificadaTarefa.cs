using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evenote.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificadaTarefa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Notificada",
                table: "Tarefas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notificada",
                table: "Tarefas");
        }
    }
}

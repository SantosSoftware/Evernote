using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parch.Migrations
{
    /// <inheritdoc />
    public partial class AddCriadoEmNota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Notas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Notas");
        }
    }
}

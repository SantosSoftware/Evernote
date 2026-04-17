using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parch.Migrations
{
    /// <inheritdoc />
    public partial class AddRecorrenciaTarefas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiasSemanais",
                table: "Tarefas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IntervaloRecorrencia",
                table: "Tarefas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Recorrencia",
                table: "Tarefas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecorrenciaFim",
                table: "Tarefas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasSemanais",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "IntervaloRecorrencia",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "Recorrencia",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "RecorrenciaFim",
                table: "Tarefas");
        }
    }
}

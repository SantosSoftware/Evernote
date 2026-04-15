using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evenote.Migrations
{
    /// <inheritdoc />
    public partial class AddGravacaoTela : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GravacoesTela",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NotaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "TEXT", nullable: false),
                    NomeOriginal = table.Column<string>(type: "TEXT", nullable: false),
                    TamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GravacoesTela", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GravacoesTela_Notas_NotaId",
                        column: x => x.NotaId,
                        principalTable: "Notas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GravacoesTela_NotaId",
                table: "GravacoesTela",
                column: "NotaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GravacoesTela");
        }
    }
}

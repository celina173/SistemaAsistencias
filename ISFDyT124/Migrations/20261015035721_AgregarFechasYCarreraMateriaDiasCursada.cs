using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFechasYCarreraMateriaDiasCursada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CaCoFechaFin",
                table: "CarreraCohortes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CaCoFechaInicio",
                table: "CarreraCohortes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarreraMateriaDiasCursada",
                columns: table => new
                {
                    CaMaDiId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaMaId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarreraMateriaDiasCursada", x => x.CaMaDiId);
                    table.ForeignKey(
                        name: "FK_CarreraMateriaDiasCursada_CarreraMateria_CaMaId",
                        column: x => x.CaMaId,
                        principalTable: "CarreraMateria",
                        principalColumn: "CaMaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarreraMateriaDiasCursada_CaMaId_DiaSemana",
                table: "CarreraMateriaDiasCursada",
                columns: new[] { "CaMaId", "DiaSemana" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarreraMateriaDiasCursada");

            migrationBuilder.DropColumn(
                name: "CaCoFechaFin",
                table: "CarreraCohortes");

            migrationBuilder.DropColumn(
                name: "CaCoFechaInicio",
                table: "CarreraCohortes");
        }
    }
}

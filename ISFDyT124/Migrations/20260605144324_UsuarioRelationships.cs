using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CaCoId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UsuarioCarreraMateria",
                columns: table => new
                {
                    CarreraMateriasCaMaId = table.Column<int>(type: "int", nullable: false),
                    UsuariosUsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioCarreraMateria", x => new { x.CarreraMateriasCaMaId, x.UsuariosUsId });
                    table.ForeignKey(
                        name: "FK_UsuarioCarreraMateria_CarreraMaterias_CarreraMateriasCaMaId",
                        column: x => x.CarreraMateriasCaMaId,
                        principalTable: "CarreraMaterias",
                        principalColumn: "CaMaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioCarreraMateria_Usuarios_UsuariosUsId",
                        column: x => x.UsuariosUsId,
                        principalTable: "Usuarios",
                        principalColumn: "UsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CaCoId",
                table: "Usuarios",
                column: "CaCoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioCarreraMateria_UsuariosUsId",
                table: "UsuarioCarreraMateria",
                column: "UsuariosUsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CaCoId",
                table: "Usuarios",
                column: "CaCoId",
                principalTable: "CarreraCohortes",
                principalColumn: "CaCoId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CaCoId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "UsuarioCarreraMateria");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CaCoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CaCoId",
                table: "Usuarios");
        }
    }
}

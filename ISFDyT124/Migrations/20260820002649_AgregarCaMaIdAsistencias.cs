using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCaMaIdAsistencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles");

            migrationBuilder.AddColumn<bool>(
                name: "CoEstado",
                table: "Cohortes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CaMaId",
                table: "Asistencias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarrerasMaterias",
                columns: table => new
                {
                    CaMaId = table.Column<int>(type: "int", nullable: false),
                    CaId = table.Column<int>(type: "int", nullable: false),
                    MaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrerasMaterias", x => x.CaMaId);
                    table.ForeignKey(
                        name: "FK_CarrerasMaterias_Carreras_CaId",
                        column: x => x.CaId,
                        principalTable: "Carreras",
                        principalColumn: "CaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarrerasMaterias_Materias_MaId",
                        column: x => x.MaId,
                        principalTable: "Materias",
                        principalColumn: "MaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    InId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsId = table.Column<int>(type: "int", nullable: false),
                    CaMaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => x.InId);
                    table.ForeignKey(
                        name: "FK_Inscripciones_CarreraMaterias_CaMaId",
                        column: x => x.CaMaId,
                        principalTable: "CarreraMaterias",
                        principalColumn: "CaMaId");
                    table.ForeignKey(
                        name: "FK_Inscripciones_Usuarios_UsId",
                        column: x => x.UsId,
                        principalTable: "Usuarios",
                        principalColumn: "UsId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_CaMaId",
                table: "Asistencias",
                column: "CaMaId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrerasMaterias_CaId",
                table: "CarrerasMaterias",
                column: "CaId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrerasMaterias_MaId",
                table: "CarrerasMaterias",
                column: "MaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_CaMaId",
                table: "Inscripciones",
                column: "CaMaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_UsId",
                table: "Inscripciones",
                column: "UsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_CarreraMaterias_CaMaId",
                table: "Asistencias",
                column: "CaMaId",
                principalTable: "CarreraMaterias",
                principalColumn: "CaMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles",
                column: "RoId",
                principalTable: "Roles",
                principalColumn: "RoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_CarreraMaterias_CaMaId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "CarrerasMaterias");

            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_CaMaId",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "CoEstado",
                table: "Cohortes");

            migrationBuilder.DropColumn(
                name: "CaMaId",
                table: "Asistencias");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles",
                column: "RoId",
                principalTable: "Roles",
                principalColumn: "RoId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

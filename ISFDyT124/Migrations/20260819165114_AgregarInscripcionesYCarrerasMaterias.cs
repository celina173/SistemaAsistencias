using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInscripcionesYCarrerasMaterias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarreraMaterias_Carreras_CaId",
                table: "CarreraMaterias");

            migrationBuilder.DropForeignKey(
                name: "FK_CarreraMaterias_Materias_MaId",
                table: "CarreraMaterias");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioCarreraMateria_CarreraMaterias_CarreraMateriasCaMaId",
                table: "UsuarioCarreraMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CaCoId",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarreraMaterias",
                table: "CarreraMaterias");

            migrationBuilder.RenameTable(
                name: "CarreraMaterias",
                newName: "CarreraMateria");

            migrationBuilder.RenameIndex(
                name: "IX_CarreraMaterias_MaId",
                table: "CarreraMateria",
                newName: "IX_CarreraMateria_MaId");

            migrationBuilder.RenameIndex(
                name: "IX_CarreraMaterias_CaId",
                table: "CarreraMateria",
                newName: "IX_CarreraMateria_CaId");

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

            migrationBuilder.AddColumn<int>(
                name: "CarreraMateriaCaMaId",
                table: "Asistencias",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarreraMateria",
                table: "CarreraMateria",
                column: "CaMaId");

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    InId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsId = table.Column<int>(type: "int", nullable: false),
                    CaMaId = table.Column<int>(type: "int", nullable: false),
                    UsuariosUsId = table.Column<int>(type: "int", nullable: true),
                    CarreraMateriaCaMaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => x.InId);
                    table.ForeignKey(
                        name: "FK_Inscripciones_CarreraMateria_CarreraMateriaCaMaId",
                        column: x => x.CarreraMateriaCaMaId,
                        principalTable: "CarreraMateria",
                        principalColumn: "CaMaId");
                    table.ForeignKey(
                        name: "FK_Inscripciones_Usuarios_UsuariosUsId",
                        column: x => x.UsuariosUsId,
                        principalTable: "Usuarios",
                        principalColumn: "UsId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_CarreraMateriaCaMaId",
                table: "Asistencias",
                column: "CarreraMateriaCaMaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_CarreraMateriaCaMaId",
                table: "Inscripciones",
                column: "CarreraMateriaCaMaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_UsuariosUsId",
                table: "Inscripciones",
                column: "UsuariosUsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId",
                table: "Asistencias",
                column: "CarreraMateriaCaMaId",
                principalTable: "CarreraMateria",
                principalColumn: "CaMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMateria_Carreras_CaId",
                table: "CarreraMateria",
                column: "CaId",
                principalTable: "Carreras",
                principalColumn: "CaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMateria_Materias_MaId",
                table: "CarreraMateria",
                column: "MaId",
                principalTable: "Materias",
                principalColumn: "MaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioCarreraMateria_CarreraMateria_CarreraMateriasCaMaId",
                table: "UsuarioCarreraMateria",
                column: "CarreraMateriasCaMaId",
                principalTable: "CarreraMateria",
                principalColumn: "CaMaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles",
                column: "RoId",
                principalTable: "Roles",
                principalColumn: "RoId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CaCoId",
                table: "Usuarios",
                column: "CaCoId",
                principalTable: "CarreraCohortes",
                principalColumn: "CaCoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_CarreraMateria_Carreras_CaId",
                table: "CarreraMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_CarreraMateria_Materias_MaId",
                table: "CarreraMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioCarreraMateria_CarreraMateria_CarreraMateriasCaMaId",
                table: "UsuarioCarreraMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CaCoId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_CarreraMateriaCaMaId",
                table: "Asistencias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarreraMateria",
                table: "CarreraMateria");

            migrationBuilder.DropColumn(
                name: "CoEstado",
                table: "Cohortes");

            migrationBuilder.DropColumn(
                name: "CaMaId",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "CarreraMateriaCaMaId",
                table: "Asistencias");

            migrationBuilder.RenameTable(
                name: "CarreraMateria",
                newName: "CarreraMaterias");

            migrationBuilder.RenameIndex(
                name: "IX_CarreraMateria_MaId",
                table: "CarreraMaterias",
                newName: "IX_CarreraMaterias_MaId");

            migrationBuilder.RenameIndex(
                name: "IX_CarreraMateria_CaId",
                table: "CarreraMaterias",
                newName: "IX_CarreraMaterias_CaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarreraMaterias",
                table: "CarreraMaterias",
                column: "CaMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMaterias_Carreras_CaId",
                table: "CarreraMaterias",
                column: "CaId",
                principalTable: "Carreras",
                principalColumn: "CaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMaterias_Materias_MaId",
                table: "CarreraMaterias",
                column: "MaId",
                principalTable: "Materias",
                principalColumn: "MaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioCarreraMateria_CarreraMaterias_CarreraMateriasCaMaId",
                table: "UsuarioCarreraMateria",
                column: "CarreraMateriasCaMaId",
                principalTable: "CarreraMaterias",
                principalColumn: "CaMaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Roles_RoId",
                table: "UsuarioRoles",
                column: "RoId",
                principalTable: "Roles",
                principalColumn: "RoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CaCoId",
                table: "Usuarios",
                column: "CaCoId",
                principalTable: "CarreraCohortes",
                principalColumn: "CaCoId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

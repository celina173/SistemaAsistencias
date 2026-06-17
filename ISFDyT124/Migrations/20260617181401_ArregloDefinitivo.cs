using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class ArregloDefinitivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Materias_MaId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioCarreraMateria_CarreraMaterias_CarreraMateriasCaMaId",
                table: "UsuarioCarreraMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioCarreraMateria_Usuarios_UsuariosUsId",
                table: "UsuarioCarreraMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CaCoId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CaCoId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_UsDni",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_MaId",
                table: "Asistencias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioCarreraMateria",
                table: "UsuarioCarreraMateria");

            migrationBuilder.RenameTable(
                name: "UsuarioCarreraMateria",
                newName: "CarreraMateriaUsuario");

            migrationBuilder.RenameIndex(
                name: "IX_UsuarioCarreraMateria_UsuariosUsId",
                table: "CarreraMateriaUsuario",
                newName: "IX_CarreraMateriaUsuario_UsuariosUsId");

            migrationBuilder.AddColumn<int>(
                name: "CarreraCohorteCaCoId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MateriasMaId",
                table: "Asistencias",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarreraMateriaUsuario",
                table: "CarreraMateriaUsuario",
                columns: new[] { "CarreraMateriasCaMaId", "UsuariosUsId" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CarreraCohorteCaCoId",
                table: "Usuarios",
                column: "CarreraCohorteCaCoId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_MateriasMaId",
                table: "Asistencias",
                column: "MateriasMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Materias_MateriasMaId",
                table: "Asistencias",
                column: "MateriasMaId",
                principalTable: "Materias",
                principalColumn: "MaId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMateriaUsuario_CarreraMaterias_CarreraMateriasCaMaId",
                table: "CarreraMateriaUsuario",
                column: "CarreraMateriasCaMaId",
                principalTable: "CarreraMaterias",
                principalColumn: "CaMaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMateriaUsuario_Usuarios_UsuariosUsId",
                table: "CarreraMateriaUsuario",
                column: "UsuariosUsId",
                principalTable: "Usuarios",
                principalColumn: "UsId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CarreraCohorteCaCoId",
                table: "Usuarios",
                column: "CarreraCohorteCaCoId",
                principalTable: "CarreraCohortes",
                principalColumn: "CaCoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Materias_MateriasMaId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_CarreraMateriaUsuario_CarreraMaterias_CarreraMateriasCaMaId",
                table: "CarreraMateriaUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_CarreraMateriaUsuario_Usuarios_UsuariosUsId",
                table: "CarreraMateriaUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_CarreraCohortes_CarreraCohorteCaCoId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CarreraCohorteCaCoId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_MateriasMaId",
                table: "Asistencias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarreraMateriaUsuario",
                table: "CarreraMateriaUsuario");

            migrationBuilder.DropColumn(
                name: "CarreraCohorteCaCoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MateriasMaId",
                table: "Asistencias");

            migrationBuilder.RenameTable(
                name: "CarreraMateriaUsuario",
                newName: "UsuarioCarreraMateria");

            migrationBuilder.RenameIndex(
                name: "IX_CarreraMateriaUsuario_UsuariosUsId",
                table: "UsuarioCarreraMateria",
                newName: "IX_UsuarioCarreraMateria_UsuariosUsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioCarreraMateria",
                table: "UsuarioCarreraMateria",
                columns: new[] { "CarreraMateriasCaMaId", "UsuariosUsId" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CaCoId",
                table: "Usuarios",
                column: "CaCoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsDni",
                table: "Usuarios",
                column: "UsDni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_MaId",
                table: "Asistencias",
                column: "MaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Materias_MaId",
                table: "Asistencias",
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
                name: "FK_UsuarioCarreraMateria_Usuarios_UsuariosUsId",
                table: "UsuarioCarreraMateria",
                column: "UsuariosUsId",
                principalTable: "Usuarios",
                principalColumn: "UsId",
                onDelete: ReferentialAction.Cascade);

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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class LimpiarColumnasSombraAsistenciaInscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscripciones_CarreraMateria_CarreraMateriaCaMaId",
                table: "Inscripciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscripciones_Usuarios_UsuariosUsId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_CarreraMateriaCaMaId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_UsuariosUsId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_CarreraMateriaCaMaId",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "CarreraMateriaCaMaId",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "UsuariosUsId",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "CarreraMateriaCaMaId",
                table: "Asistencias");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_CaMaId",
                table: "Inscripciones",
                column: "CaMaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_UsId",
                table: "Inscripciones",
                column: "UsId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_CaMaId",
                table: "Asistencias",
                column: "CaMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_CarreraMateria_CaMaId",
                table: "Asistencias",
                column: "CaMaId",
                principalTable: "CarreraMateria",
                principalColumn: "CaMaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscripciones_CarreraMateria_CaMaId",
                table: "Inscripciones",
                column: "CaMaId",
                principalTable: "CarreraMateria",
                principalColumn: "CaMaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscripciones_Usuarios_UsId",
                table: "Inscripciones",
                column: "UsId",
                principalTable: "Usuarios",
                principalColumn: "UsId",
                onDelete: ReferentialAction.Cascade);

            // AsNumeroModulo no está mapeada en ningún modelo de C# ni aparece en ninguna
            // migración anterior — quedó de una ALTER TABLE hecha directo sobre la base
            // compartida, nunca reflejada en el código. Está en NULL en las 3 filas que hay
            // hoy en Asistencias, así que no hay pérdida de datos real al sacarla.
            migrationBuilder.DropColumn(
                name: "AsNumeroModulo",
                table: "Asistencias");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AsNumeroModulo",
                table: "Asistencias",
                type: "int",
                nullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_CarreraMateria_CaMaId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscripciones_CarreraMateria_CaMaId",
                table: "Inscripciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscripciones_Usuarios_UsId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_CaMaId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_UsId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_CaMaId",
                table: "Asistencias");

            migrationBuilder.AddColumn<int>(
                name: "CarreraMateriaCaMaId",
                table: "Inscripciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuariosUsId",
                table: "Inscripciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CarreraMateriaCaMaId",
                table: "Asistencias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_CarreraMateriaCaMaId",
                table: "Inscripciones",
                column: "CarreraMateriaCaMaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_UsuariosUsId",
                table: "Inscripciones",
                column: "UsuariosUsId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_CarreraMateriaCaMaId",
                table: "Asistencias",
                column: "CarreraMateriaCaMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId",
                table: "Asistencias",
                column: "CarreraMateriaCaMaId",
                principalTable: "CarreraMateria",
                principalColumn: "CaMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inscripciones_CarreraMateria_CarreraMateriaCaMaId",
                table: "Inscripciones",
                column: "CarreraMateriaCaMaId",
                principalTable: "CarreraMateria",
                principalColumn: "CaMaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inscripciones_Usuarios_UsuariosUsId",
                table: "Inscripciones",
                column: "UsuariosUsId",
                principalTable: "Usuarios",
                principalColumn: "UsId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class RestringirBorradoAlumnoConAsistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Usuarios_UsId",
                table: "Asistencias");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Usuarios_UsId",
                table: "Asistencias",
                column: "UsId",
                principalTable: "Usuarios",
                principalColumn: "UsId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Usuarios_UsId",
                table: "Asistencias");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Usuarios_UsId",
                table: "Asistencias",
                column: "UsId",
                principalTable: "Usuarios",
                principalColumn: "UsId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

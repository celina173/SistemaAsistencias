using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndicesUnicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IX_CarreraMateria_CaId e IX_CarreraCohortes_CaId no se borran acá porque
            // ninguno de los dos existe (ni en una base nueva ni en Railway, verificado):
            // el primero se perdió en la migración MateriaCarreras (recreó la tabla sin
            // recrear ese índice), el segundo nunca llegó a existir en la práctica pese a
            // que InitialCreate lo declaraba. Intentar borrarlos tira "no existe" y aborta
            // toda la migración.
            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_UsId",
                table: "Inscripciones");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_UsId_CaMaId",
                table: "Inscripciones",
                columns: new[] { "UsId", "CaMaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarreraMateria_CaId_MaId",
                table: "CarreraMateria",
                columns: new[] { "CaId", "MaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarreraCohortes_CaId_CoId",
                table: "CarreraCohortes",
                columns: new[] { "CaId", "CoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_UsId_CaMaId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_CarreraMateria_CaId_MaId",
                table: "CarreraMateria");

            migrationBuilder.DropIndex(
                name: "IX_CarreraCohortes_CaId_CoId",
                table: "CarreraCohortes");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_UsId",
                table: "Inscripciones",
                column: "UsId");
        }
    }
}

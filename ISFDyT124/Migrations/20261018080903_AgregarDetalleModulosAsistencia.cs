using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDetalleModulosAsistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsistenciaModulos",
                columns: table => new
                {
                    AsMoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AsId = table.Column<int>(type: "int", nullable: false),
                    NumeroModulo = table.Column<int>(type: "int", nullable: false),
                    AsMoPresente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsistenciaModulos", x => x.AsMoId);
                    table.ForeignKey(
                        name: "FK_AsistenciaModulos_Asistencias_AsId",
                        column: x => x.AsId,
                        principalTable: "Asistencias",
                        principalColumn: "AsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsistenciaModulos_AsId_NumeroModulo",
                table: "AsistenciaModulos",
                columns: new[] { "AsId", "NumeroModulo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsistenciaModulos");
        }
    }
}

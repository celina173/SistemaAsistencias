using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAsPorcentajeAsistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AsPorcentaje",
                table: "Asistencias",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsPorcentaje",
                table: "Asistencias");
        }
    }
}

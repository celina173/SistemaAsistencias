using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoActivoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: true -- el modelo C# (Usuario.UsActivo) ya asume "true" como
            // default de negocio ("todo usuario es Activo al crearse"). Con "false" acá,
            // aplicar esta migración contra la base real habría marcado a TODOS los
            // usuarios/alumnos ya existentes como "INACTIVO" de un día para el otro.
            migrationBuilder.AddColumn<bool>(
                name: "UsActivo",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsActivo",
                table: "Usuarios");
        }
    }
}

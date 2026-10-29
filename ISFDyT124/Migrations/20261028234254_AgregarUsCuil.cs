using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsCuil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UsCuil",
                table: "Usuarios",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsCuil",
                table: "Usuarios",
                column: "UsCuil",
                unique: true,
                filter: "[UsCuil] IS NOT NULL");

            // Siembra para pruebas: CUIL ficticio a los directivos existentes (RoId = 4, Dirección).
            // El índice es único, así que cada uno recibe uno distinto: 11111111111, 22222222222, ...
            migrationBuilder.Sql(@"
                WITH directivos AS (
                    SELECT UsId, ROW_NUMBER() OVER (ORDER BY UsId) AS n
                    FROM Usuarios
                    WHERE RoId = 4 AND UsCuil IS NULL
                )
                UPDATE u
                SET UsCuil = d.n * 11111111111
                FROM Usuarios u
                JOIN directivos d ON d.UsId = u.UsId
                WHERE d.n <= 9;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_UsCuil",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UsCuil",
                table: "Usuarios");
        }
    }
}

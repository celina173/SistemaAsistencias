using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class EliminarUsuarioRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioRoles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsRoId = table.Column<int>(type: "int", nullable: false),
                    RoId = table.Column<int>(type: "int", nullable: false),
                    UsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => x.UsRoId);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RoId",
                        column: x => x.RoId,
                        principalTable: "Roles",
                        principalColumn: "RoId");
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsId",
                        column: x => x.UsId,
                        principalTable: "Usuarios",
                        principalColumn: "UsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RoId",
                table: "UsuarioRoles",
                column: "RoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_UsId",
                table: "UsuarioRoles",
                column: "UsId");
        }
    }
}

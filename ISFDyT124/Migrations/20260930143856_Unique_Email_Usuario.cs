using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class Unique_Email_Usuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsEmail",
                table: "Usuarios",
                column: "UsEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_UsEmail",
                table: "Usuarios");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class PermitirEmailNulo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_UsEmail",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "UsEmail",
                table: "Usuarios",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsEmail",
                table: "Usuarios",
                column: "UsEmail",
                unique: true,
                filter: "[UsEmail] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_UsEmail",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "UsEmail",
                table: "Usuarios",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsEmail",
                table: "Usuarios",
                column: "UsEmail",
                unique: true);
        }
    }
}

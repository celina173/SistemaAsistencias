using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOfflineAAsistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AsClientGuid",
                table: "Asistencias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AsFechaCarga",
                table: "Asistencias",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_AsClientGuid",
                table: "Asistencias",
                column: "AsClientGuid",
                unique: true,
                filter: "[AsClientGuid] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asistencias_AsClientGuid",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "AsClientGuid",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "AsFechaCarga",
                table: "Asistencias");
        }
    }
}

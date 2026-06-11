using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carreras",
                columns: table => new
                {
                    CaId = table.Column<int>(type: "int", nullable: false),
                    CaDenominacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreras", x => x.CaId);
                });

            migrationBuilder.CreateTable(
                name: "Cohortes",
                columns: table => new
                {
                    CoId = table.Column<int>(type: "int", nullable: false),
                    CoAnio = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cohortes", x => x.CoId);
                });

            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    MaId = table.Column<int>(type: "int", nullable: false),
                    MaDenominacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaModalidad = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    MaCantModulos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.MaId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoId = table.Column<int>(type: "int", nullable: false),
                    RoDenominacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoId);
                });

            migrationBuilder.CreateTable(
                name: "CarreraCohortes",
                columns: table => new
                {
                    CaCoId = table.Column<int>(type: "int", nullable: false),
                    CaId = table.Column<int>(type: "int", nullable: false),
                    CoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarreraCohortes", x => x.CaCoId);
                    table.ForeignKey(
                        name: "FK_CarreraCohortes_Carreras_CaId",
                        column: x => x.CaId,
                        principalTable: "Carreras",
                        principalColumn: "CaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarreraCohortes_Cohortes_CoId",
                        column: x => x.CoId,
                        principalTable: "Cohortes",
                        principalColumn: "CoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarreraMaterias",
                columns: table => new
                {
                    CaMaId = table.Column<int>(type: "int", nullable: false),
                    CaId = table.Column<int>(type: "int", nullable: false),
                    MaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarreraMaterias", x => x.CaMaId);
                    table.ForeignKey(
                        name: "FK_CarreraMaterias_Carreras_CaId",
                        column: x => x.CaId,
                        principalTable: "Carreras",
                        principalColumn: "CaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarreraMaterias_Materias_MaId",
                        column: x => x.MaId,
                        principalTable: "Materias",
                        principalColumn: "MaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsId = table.Column<int>(type: "int", nullable: false),
                    UsApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsDni = table.Column<int>(type: "int", nullable: false),
                    UsEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsContrasena = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsId);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_RoId",
                        column: x => x.RoId,
                        principalTable: "Roles",
                        principalColumn: "RoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asistencias",
                columns: table => new
                {
                    AsId = table.Column<int>(type: "int", nullable: false),
                    AsFecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AsPresente = table.Column<bool>(type: "bit", nullable: false),
                    AsJustificacion = table.Column<bool>(type: "bit", nullable: false),
                    UsId = table.Column<int>(type: "int", nullable: true),
                    MaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencias", x => x.AsId);
                    table.ForeignKey(
                        name: "FK_Asistencias_Materias_MaId",
                        column: x => x.MaId,
                        principalTable: "Materias",
                        principalColumn: "MaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asistencias_Usuarios_UsId",
                        column: x => x.UsId,
                        principalTable: "Usuarios",
                        principalColumn: "UsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsRoId = table.Column<int>(type: "int", nullable: false),
                    UsId = table.Column<int>(type: "int", nullable: false),
                    RoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => x.UsRoId);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RoId",
                        column: x => x.RoId,
                        principalTable: "Roles",
                        principalColumn: "RoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsId",
                        column: x => x.UsId,
                        principalTable: "Usuarios",
                        principalColumn: "UsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_MaId",
                table: "Asistencias",
                column: "MaId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_UsId",
                table: "Asistencias",
                column: "UsId");

            migrationBuilder.CreateIndex(
                name: "IX_CarreraCohortes_CaId",
                table: "CarreraCohortes",
                column: "CaId");

            migrationBuilder.CreateIndex(
                name: "IX_CarreraCohortes_CoId",
                table: "CarreraCohortes",
                column: "CoId");

            migrationBuilder.CreateIndex(
                name: "IX_CarreraMaterias_CaId",
                table: "CarreraMaterias",
                column: "CaId");

            migrationBuilder.CreateIndex(
                name: "IX_CarreraMaterias_MaId",
                table: "CarreraMaterias",
                column: "MaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RoId",
                table: "UsuarioRoles",
                column: "RoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_UsId",
                table: "UsuarioRoles",
                column: "UsId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RoId",
                table: "Usuarios",
                column: "RoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsDni",
                table: "Usuarios",
                column: "UsDni",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asistencias");

            migrationBuilder.DropTable(
                name: "CarreraCohortes");

            migrationBuilder.DropTable(
                name: "CarreraMaterias");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "Cohortes");

            migrationBuilder.DropTable(
                name: "Carreras");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}

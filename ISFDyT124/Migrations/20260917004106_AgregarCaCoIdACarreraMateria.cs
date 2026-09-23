using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCaCoIdACarreraMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Agregar la columna nueva (nullable) ANTES de tocar CaId, para poder
            //    backfillear con el valor viejo antes de perderlo.
            migrationBuilder.AddColumn<int>(
                name: "CaCoId",
                table: "CarreraMateria",
                type: "int",
                nullable: true);

            // 2) Completar las CarreraCohorte que falten para Carreras que ya tienen
            //    cátedras cargadas (antes CarreraMateria no sabía de cohortes). Se usa
            //    la única cohorte existente como default (hoy: 2026). En una base nueva
            //    o vacía esto no inserta nada, porque tampoco hay CarreraMateria previas.
            //    No se especifica CaCoId en el INSERT: la migración
            //    ConvertirCarreraCohorteCaCoIdAIdentity (corre antes que esta) ya convirtió
            //    esa columna a IDENTITY, así que la base lo asigna sola.
            migrationBuilder.Sql(@"
                DECLARE @DefaultCoId INT = (SELECT TOP 1 CoId FROM Cohortes ORDER BY CoId);
                IF @DefaultCoId IS NOT NULL
                BEGIN
                    INSERT INTO CarreraCohortes (CaId, CoId)
                    SELECT DISTINCT cm.CaId, @DefaultCoId
                    FROM CarreraMateria cm
                    WHERE NOT EXISTS (
                        SELECT 1 FROM CarreraCohortes cc WHERE cc.CaId = cm.CaId
                    );
                END
            ");

            // 3) Backfill: cada CarreraMateria existente apunta a la CarreraCohorte de
            //    su Carrera (garantizada por el paso anterior). Si una Carrera tuviera
            //    más de una CarreraCohorte se toma la de menor CaCoId (hoy no pasa).
            migrationBuilder.Sql(@"
                UPDATE cm
                SET cm.CaCoId = cc.CaCoId
                FROM CarreraMateria cm
                CROSS APPLY (
                    SELECT TOP 1 CaCoId
                    FROM CarreraCohortes
                    WHERE CaId = cm.CaId
                    ORDER BY CaCoId
                ) cc
            ");

            // 4) Recién ahora se puede soltar la columna vieja sin perder el dato.
            // El nombre de esta FK no es confiable: Railway todavía tiene el nombre
            // viejo con drift ("FK_CarrerasMaterias_Carreras_CaId", con "s") de antes
            // del fix del ticket 6.2, que solo corrigió el archivo de migración para
            // bases nuevas — nunca renombró la restricción real ya aplicada en Railway.
            // Se busca dinámicamente por relación (tabla origen -> tabla destino) en vez
            // de asumir un nombre fijo.
            migrationBuilder.Sql(@"
                DECLARE @fk NVARCHAR(200);
                SELECT @fk = fk.name
                FROM sys.foreign_keys fk
                WHERE fk.parent_object_id = OBJECT_ID('CarreraMateria')
                    AND fk.referenced_object_id = OBJECT_ID('Carreras');
                IF @fk IS NOT NULL
                    EXEC('ALTER TABLE CarreraMateria DROP CONSTRAINT [' + @fk + ']');
            ");

            migrationBuilder.DropIndex(
                name: "IX_CarreraMateria_CaId_MaId",
                table: "CarreraMateria");

            migrationBuilder.DropColumn(
                name: "CaId",
                table: "CarreraMateria");

            migrationBuilder.CreateIndex(
                name: "IX_CarreraMateria_CaCoId_MaId",
                table: "CarreraMateria",
                columns: new[] { "CaCoId", "MaId" },
                unique: true,
                filter: "[CaCoId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMateria_CarreraCohortes_CaCoId",
                table: "CarreraMateria",
                column: "CaCoId",
                principalTable: "CarreraCohortes",
                principalColumn: "CaCoId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarreraMateria_CarreraCohortes_CaCoId",
                table: "CarreraMateria");

            migrationBuilder.DropIndex(
                name: "IX_CarreraMateria_CaCoId_MaId",
                table: "CarreraMateria");

            migrationBuilder.AddColumn<int>(
                name: "CaId",
                table: "CarreraMateria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Reconstruir CaId a partir de la CarreraCohorte asignada (best-effort: una
            // fila que hubiera quedado con CaCoId NULL conserva el 0 por defecto).
            migrationBuilder.Sql(@"
                UPDATE cm
                SET cm.CaId = cc.CaId
                FROM CarreraMateria cm
                JOIN CarreraCohortes cc ON cc.CaCoId = cm.CaCoId
                WHERE cm.CaCoId IS NOT NULL
            ");

            migrationBuilder.DropColumn(
                name: "CaCoId",
                table: "CarreraMateria");

            migrationBuilder.CreateIndex(
                name: "IX_CarreraMateria_CaId_MaId",
                table: "CarreraMateria",
                columns: new[] { "CaId", "MaId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CarreraMateria_Carreras_CaId",
                table: "CarreraMateria",
                column: "CaId",
                principalTable: "Carreras",
                principalColumn: "CaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

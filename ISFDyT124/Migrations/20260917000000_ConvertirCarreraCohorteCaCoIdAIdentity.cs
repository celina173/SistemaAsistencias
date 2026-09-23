using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class ConvertirCarreraCohorteCaCoIdAIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server no permite ALTER COLUMN para agregar IDENTITY a una columna
            // existente: hay que recrear la tabla preservando los datos y las FKs que
            // apuntan a CaCoId (mismo patrón ya usado en MateriaCarreras para CaMaId).
            // CarreraCohorte quedaba como la única PK manual de esa familia (Carrera,
            // Materia, Asistencia y CarreraMateria ya son IDENTITY) — eso obligaba a
            // calcular el próximo ID a mano en cualquier alta, que fue la causa real de
            // un bug en el backfill de CarreraMateria.CaCoId de la migración siguiente.
            migrationBuilder.Sql(@"
                ALTER TABLE Usuarios DROP CONSTRAINT FK_Usuarios_CarreraCohortes_CaCoId;
                ALTER TABLE CarreraCohortes DROP CONSTRAINT FK_CarreraCohortes_Carreras_CaId;
                ALTER TABLE CarreraCohortes DROP CONSTRAINT FK_CarreraCohortes_Cohortes_CoId;
                DROP INDEX IX_CarreraCohortes_CaId_CoId ON CarreraCohortes;

                EXEC sp_rename 'CarreraCohortes', 'CarreraCohortes_old';
                EXEC sp_rename 'PK_CarreraCohortes', 'PK_CarreraCohortes_old';

                CREATE TABLE CarreraCohortes (
                    CaCoId INT IDENTITY(1,1) NOT NULL,
                    CaId INT NOT NULL,
                    CoId INT NOT NULL,
                    CONSTRAINT PK_CarreraCohortes PRIMARY KEY (CaCoId),
                    CONSTRAINT FK_CarreraCohortes_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras (CaId) ON DELETE CASCADE,
                    CONSTRAINT FK_CarreraCohortes_Cohortes_CoId FOREIGN KEY (CoId) REFERENCES Cohortes (CoId) ON DELETE CASCADE
                );

                SET IDENTITY_INSERT CarreraCohortes ON;
                INSERT INTO CarreraCohortes (CaCoId, CaId, CoId)
                SELECT CaCoId, CaId, CoId FROM CarreraCohortes_old ORDER BY CaCoId;
                SET IDENTITY_INSERT CarreraCohortes OFF;

                DECLARE @maxId INT = (SELECT MAX(CaCoId) FROM CarreraCohortes);
                IF @maxId IS NOT NULL
                    DBCC CHECKIDENT ('CarreraCohortes', RESEED, @maxId);

                CREATE UNIQUE INDEX IX_CarreraCohortes_CaId_CoId ON CarreraCohortes (CaId, CoId);

                ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_CarreraCohortes_CaCoId
                    FOREIGN KEY (CaCoId) REFERENCES CarreraCohortes (CaCoId);

                DROP TABLE CarreraCohortes_old;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE Usuarios DROP CONSTRAINT FK_Usuarios_CarreraCohortes_CaCoId;
                ALTER TABLE CarreraCohortes DROP CONSTRAINT FK_CarreraCohortes_Carreras_CaId;
                ALTER TABLE CarreraCohortes DROP CONSTRAINT FK_CarreraCohortes_Cohortes_CoId;
                DROP INDEX IX_CarreraCohortes_CaId_CoId ON CarreraCohortes;

                EXEC sp_rename 'CarreraCohortes', 'CarreraCohortes_old';
                EXEC sp_rename 'PK_CarreraCohortes', 'PK_CarreraCohortes_old';

                CREATE TABLE CarreraCohortes (
                    CaCoId INT NOT NULL,
                    CaId INT NOT NULL,
                    CoId INT NOT NULL,
                    CONSTRAINT PK_CarreraCohortes PRIMARY KEY (CaCoId),
                    CONSTRAINT FK_CarreraCohortes_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras (CaId) ON DELETE CASCADE,
                    CONSTRAINT FK_CarreraCohortes_Cohortes_CoId FOREIGN KEY (CoId) REFERENCES Cohortes (CoId) ON DELETE CASCADE
                );

                INSERT INTO CarreraCohortes (CaCoId, CaId, CoId)
                SELECT CaCoId, CaId, CoId FROM CarreraCohortes_old ORDER BY CaCoId;

                CREATE UNIQUE INDEX IX_CarreraCohortes_CaId_CoId ON CarreraCohortes (CaId, CoId);

                ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_CarreraCohortes_CaCoId
                    FOREIGN KEY (CaCoId) REFERENCES CarreraCohortes (CaCoId);

                DROP TABLE CarreraCohortes_old;
            ");
        }
    }
}

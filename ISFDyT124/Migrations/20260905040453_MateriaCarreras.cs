using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class MateriaCarreras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server no permite convertir una columna existente a IDENTITY con ALTER COLUMN.
            // Hay que recrear la tabla preservando los datos y las FKs que apuntan a CaMaId.
            migrationBuilder.Sql(@"
                ALTER TABLE UsuarioCarreraMateria DROP CONSTRAINT FK_UsuarioCarreraMateria_CarreraMateria_CarreraMateriasCaMaId;
                ALTER TABLE Asistencias DROP CONSTRAINT FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId;
                ALTER TABLE Inscripciones DROP CONSTRAINT FK_Inscripciones_CarreraMateria_CarreraMateriaCaMaId;

                EXEC sp_rename 'CarreraMateria', 'CarreraMateria_old';
                EXEC sp_rename 'PK_CarrerasMaterias', 'PK_CarrerasMaterias_old';
                EXEC sp_rename 'FK_CarrerasMaterias_Carreras_CaId', 'FK_CarrerasMaterias_Carreras_CaId_old';
                EXEC sp_rename 'FK_CarrerasMaterias_Materias_MaId', 'FK_CarrerasMaterias_Materias_MaId_old';

                CREATE TABLE CarreraMateria (
                    CaMaId INT IDENTITY(1,1) NOT NULL,
                    CaId INT NOT NULL,
                    MaId INT NOT NULL,
                    CONSTRAINT PK_CarrerasMaterias PRIMARY KEY (CaMaId),
                    CONSTRAINT FK_CarrerasMaterias_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras (CaId) ON DELETE CASCADE,
                    CONSTRAINT FK_CarrerasMaterias_Materias_MaId FOREIGN KEY (MaId) REFERENCES Materias (MaId) ON DELETE CASCADE
                );

                SET IDENTITY_INSERT CarreraMateria ON;
                INSERT INTO CarreraMateria (CaMaId, CaId, MaId)
                SELECT CaMaId, CaId, MaId FROM CarreraMateria_old ORDER BY CaMaId;
                SET IDENTITY_INSERT CarreraMateria OFF;

                DECLARE @maxId INT = (SELECT MAX(CaMaId) FROM CarreraMateria);
                IF @maxId IS NOT NULL
                    DBCC CHECKIDENT ('CarreraMateria', RESEED, @maxId);

                ALTER TABLE UsuarioCarreraMateria ADD CONSTRAINT FK_UsuarioCarreraMateria_CarreraMateria_CarreraMateriasCaMaId
                    FOREIGN KEY (CarreraMateriasCaMaId) REFERENCES CarreraMateria (CaMaId) ON DELETE CASCADE;
                ALTER TABLE Asistencias ADD CONSTRAINT FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId
                    FOREIGN KEY (CarreraMateriaCaMaId) REFERENCES CarreraMateria (CaMaId);
                ALTER TABLE Inscripciones ADD CONSTRAINT FK_Inscripciones_CarreraMateria_CarreraMateriaCaMaId
                    FOREIGN KEY (CarreraMateriaCaMaId) REFERENCES CarreraMateria (CaMaId);

                DROP TABLE CarreraMateria_old;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE UsuarioCarreraMateria DROP CONSTRAINT FK_UsuarioCarreraMateria_CarreraMateria_CarreraMateriasCaMaId;
                ALTER TABLE Asistencias DROP CONSTRAINT FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId;
                ALTER TABLE Inscripciones DROP CONSTRAINT FK_Inscripciones_CarreraMateria_CarreraMateriaCaMaId;

                EXEC sp_rename 'CarreraMateria', 'CarreraMateria_old';
                EXEC sp_rename 'PK_CarrerasMaterias', 'PK_CarrerasMaterias_old';
                EXEC sp_rename 'FK_CarrerasMaterias_Carreras_CaId', 'FK_CarrerasMaterias_Carreras_CaId_old';
                EXEC sp_rename 'FK_CarrerasMaterias_Materias_MaId', 'FK_CarrerasMaterias_Materias_MaId_old';

                CREATE TABLE CarreraMateria (
                    CaMaId INT NOT NULL,
                    CaId INT NOT NULL,
                    MaId INT NOT NULL,
                    CONSTRAINT PK_CarrerasMaterias PRIMARY KEY (CaMaId),
                    CONSTRAINT FK_CarrerasMaterias_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras (CaId) ON DELETE CASCADE,
                    CONSTRAINT FK_CarrerasMaterias_Materias_MaId FOREIGN KEY (MaId) REFERENCES Materias (MaId) ON DELETE CASCADE
                );

                INSERT INTO CarreraMateria (CaMaId, CaId, MaId)
                SELECT CaMaId, CaId, MaId FROM CarreraMateria_old ORDER BY CaMaId;

                ALTER TABLE UsuarioCarreraMateria ADD CONSTRAINT FK_UsuarioCarreraMateria_CarreraMateria_CarreraMateriasCaMaId
                    FOREIGN KEY (CarreraMateriasCaMaId) REFERENCES CarreraMateria (CaMaId) ON DELETE CASCADE;
                ALTER TABLE Asistencias ADD CONSTRAINT FK_Asistencias_CarreraMateria_CarreraMateriaCaMaId
                    FOREIGN KEY (CarreraMateriaCaMaId) REFERENCES CarreraMateria (CaMaId);
                ALTER TABLE Inscripciones ADD CONSTRAINT FK_Inscripciones_CarreraMateria_CarreraMateriaCaMaId
                    FOREIGN KEY (CarreraMateriaCaMaId) REFERENCES CarreraMateria (CaMaId);

                DROP TABLE CarreraMateria_old;
            ");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISFDyT124.Migrations
{
    /// <inheritdoc />
    public partial class ConvertirCarrerasMateriasAsistenciasAIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server no permite ALTER COLUMN para agregar IDENTITY a una columna existente:
            // hay que soltar las FK/PK que dependen de ella, recrear la columna, y volver a crear
            // las constraints. Nombres de FK verificados contra la base real (algunos conservan el
            // prefijo histórico "CarrerasMaterias" de cuando la tabla se llamaba distinto).
            migrationBuilder.Sql(@"
                ALTER TABLE Asistencias DROP CONSTRAINT FK_Asistencias_Materias_MaId;
                ALTER TABLE CarreraMateria DROP CONSTRAINT FK_CarrerasMaterias_Materias_MaId;
                ALTER TABLE Materias DROP CONSTRAINT PK_Materias;
                ALTER TABLE Materias DROP COLUMN MaId;
                ALTER TABLE Materias ADD MaId INT IDENTITY(1,1) NOT NULL;
                ALTER TABLE Materias ADD CONSTRAINT PK_Materias PRIMARY KEY (MaId);
                ALTER TABLE Asistencias ADD CONSTRAINT FK_Asistencias_Materias_MaId FOREIGN KEY (MaId) REFERENCES Materias(MaId);
                ALTER TABLE CarreraMateria ADD CONSTRAINT FK_CarrerasMaterias_Materias_MaId FOREIGN KEY (MaId) REFERENCES Materias(MaId) ON DELETE CASCADE;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE CarreraCohortes DROP CONSTRAINT FK_CarreraCohortes_Carreras_CaId;
                ALTER TABLE CarreraMateria DROP CONSTRAINT FK_CarrerasMaterias_Carreras_CaId;
                ALTER TABLE Carreras DROP CONSTRAINT PK_Carreras;
                ALTER TABLE Carreras DROP COLUMN CaId;
                ALTER TABLE Carreras ADD CaId INT IDENTITY(1,1) NOT NULL;
                ALTER TABLE Carreras ADD CONSTRAINT PK_Carreras PRIMARY KEY (CaId);
                ALTER TABLE CarreraCohortes ADD CONSTRAINT FK_CarreraCohortes_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras(CaId) ON DELETE CASCADE;
                ALTER TABLE CarreraMateria ADD CONSTRAINT FK_CarrerasMaterias_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras(CaId) ON DELETE CASCADE;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE Asistencias DROP CONSTRAINT PK_Asistencias;
                ALTER TABLE Asistencias DROP COLUMN AsId;
                ALTER TABLE Asistencias ADD AsId INT IDENTITY(1,1) NOT NULL;
                ALTER TABLE Asistencias ADD CONSTRAINT PK_Asistencias PRIMARY KEY (AsId);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE Asistencias DROP CONSTRAINT PK_Asistencias;
                ALTER TABLE Asistencias DROP COLUMN AsId;
                ALTER TABLE Asistencias ADD AsId INT NOT NULL DEFAULT 0;
                ALTER TABLE Asistencias ADD CONSTRAINT PK_Asistencias PRIMARY KEY (AsId);
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE CarreraCohortes DROP CONSTRAINT FK_CarreraCohortes_Carreras_CaId;
                ALTER TABLE CarreraMateria DROP CONSTRAINT FK_CarrerasMaterias_Carreras_CaId;
                ALTER TABLE Carreras DROP CONSTRAINT PK_Carreras;
                ALTER TABLE Carreras DROP COLUMN CaId;
                ALTER TABLE Carreras ADD CaId INT NOT NULL DEFAULT 0;
                ALTER TABLE Carreras ADD CONSTRAINT PK_Carreras PRIMARY KEY (CaId);
                ALTER TABLE CarreraCohortes ADD CONSTRAINT FK_CarreraCohortes_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras(CaId) ON DELETE CASCADE;
                ALTER TABLE CarreraMateria ADD CONSTRAINT FK_CarrerasMaterias_Carreras_CaId FOREIGN KEY (CaId) REFERENCES Carreras(CaId) ON DELETE CASCADE;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE Asistencias DROP CONSTRAINT FK_Asistencias_Materias_MaId;
                ALTER TABLE CarreraMateria DROP CONSTRAINT FK_CarrerasMaterias_Materias_MaId;
                ALTER TABLE Materias DROP CONSTRAINT PK_Materias;
                ALTER TABLE Materias DROP COLUMN MaId;
                ALTER TABLE Materias ADD MaId INT NOT NULL DEFAULT 0;
                ALTER TABLE Materias ADD CONSTRAINT PK_Materias PRIMARY KEY (MaId);
                ALTER TABLE Asistencias ADD CONSTRAINT FK_Asistencias_Materias_MaId FOREIGN KEY (MaId) REFERENCES Materias(MaId);
                ALTER TABLE CarreraMateria ADD CONSTRAINT FK_CarrerasMaterias_Materias_MaId FOREIGN KEY (MaId) REFERENCES Materias(MaId) ON DELETE CASCADE;
            ");
        }
    }
}

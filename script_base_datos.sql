/*
    Script de creación de la base de datos - Sistema de Asistencias ISFDyT124
    ---------------------------------------------------------------------
    Generado a partir del esquema real de la base "Instituto" (SQL Server en Docker).
    Pensado para ejecutarse en SQL Server on Windows (SSMS o sqlcmd) sobre una
    instancia nueva / vacía.

    Uso:
      1. Abrir este archivo en SQL Server Management Studio, o ejecutar:
             sqlcmd -S <servidor> -U <usuario> -P <password> -i script_base_datos.sql
      2. Ajustar la cadena de conexión en appsettings.json (clave "DBSI") para que
         apunte a esta base.
      3. Los roles (Admin/Profesor/Alumno) y el usuario admin se crean solos al
         levantar la aplicación (seed en Program.cs) - no hace falta cargarlos acá.

    Nota: la tabla __EFMigrationsHistory se crea y se completa al final para que,
    si alguien corre después "dotnet ef database update", Entity Framework sepa
    que estas migraciones ya están aplicadas y no intente volver a crearlas.
*/

IF DB_ID(N'Instituto') IS NULL
BEGIN
    CREATE DATABASE Instituto;
END
GO

USE Instituto;
GO

-- =====================================================================
-- Tablas sin dependencias
-- =====================================================================

CREATE TABLE Roles (
    RoId            INT             NOT NULL,
    RoDenominacion  NVARCHAR(50)    NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoId)
);
GO

CREATE TABLE Cohortes (
    CoId        INT     NOT NULL,
    CoAnio      INT     NOT NULL,
    CoEstado    BIT     NOT NULL DEFAULT (CONVERT([bit], (0))),
    CONSTRAINT PK_Cohortes PRIMARY KEY (CoId)
);
GO

CREATE TABLE Carreras (
    CaId            INT             IDENTITY(1,1) NOT NULL,
    CaDenominacion  NVARCHAR(100)   NOT NULL,
    CONSTRAINT PK_Carreras PRIMARY KEY (CaId)
);
GO

CREATE TABLE Materias (
    MaId            INT             IDENTITY(1,1) NOT NULL,
    MaDenominacion  NVARCHAR(30)    NOT NULL,
    MaModalidad     NVARCHAR(25)    NOT NULL,
    MaCantModulos   INT             NOT NULL,
    CONSTRAINT PK_Materias PRIMARY KEY (MaId)
);
GO

-- =====================================================================
-- Tablas puente Carrera <-> Cohorte / Materia
-- =====================================================================

CREATE TABLE CarreraCohortes (
    CaCoId  INT NOT NULL,
    CaId    INT NOT NULL,
    CoId    INT NOT NULL,
    CONSTRAINT PK_CarreraCohortes PRIMARY KEY (CaCoId),
    CONSTRAINT FK_CarreraCohortes_Carreras_CaId FOREIGN KEY (CaId)
        REFERENCES Carreras (CaId) ON DELETE CASCADE,
    CONSTRAINT FK_CarreraCohortes_Cohortes_CoId FOREIGN KEY (CoId)
        REFERENCES Cohortes (CoId) ON DELETE CASCADE
);
GO

CREATE TABLE CarreraMateria (
    CaMaId  INT NOT NULL,
    CaId    INT NOT NULL,
    MaId    INT NOT NULL,
    CONSTRAINT PK_CarrerasMaterias PRIMARY KEY (CaMaId),
    CONSTRAINT FK_CarrerasMaterias_Carreras_CaId FOREIGN KEY (CaId)
        REFERENCES Carreras (CaId) ON DELETE CASCADE,
    CONSTRAINT FK_CarrerasMaterias_Materias_MaId FOREIGN KEY (MaId)
        REFERENCES Materias (MaId) ON DELETE CASCADE
);
GO

-- =====================================================================
-- Usuarios
-- =====================================================================

CREATE TABLE Usuarios (
    UsId            INT             NOT NULL,
    UsApellido      NVARCHAR(100)   NOT NULL,
    UsNombre        NVARCHAR(100)   NOT NULL,
    UsDni           INT             NOT NULL,
    UsEmail         NVARCHAR(MAX)   NOT NULL,
    UsContrasena    NVARCHAR(MAX)   NOT NULL,
    RoId            INT             NOT NULL,
    CaCoId          INT             NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY (UsId),
    CONSTRAINT FK_Usuarios_Roles_RoId FOREIGN KEY (RoId)
        REFERENCES Roles (RoId) ON DELETE CASCADE,
    CONSTRAINT FK_Usuarios_CarreraCohortes_CaCoId FOREIGN KEY (CaCoId)
        REFERENCES CarreraCohortes (CaCoId)
);
GO

CREATE UNIQUE INDEX IX_Usuarios_UsDni ON Usuarios (UsDni);
GO

-- =====================================================================
-- Asistencias / Inscripciones
-- =====================================================================

CREATE TABLE Asistencias (
    AsId                    INT             IDENTITY(1,1) NOT NULL,
    AsFecha                 DATETIME2(7)    NOT NULL,
    AsPresente              BIT             NOT NULL,
    AsJustificacion         BIT             NOT NULL,
    UsId                    INT             NULL,
    MaId                    INT             NULL,
    CaMaId                  INT             NULL,
    CarreraMateriaCaMaId    INT             NULL,
    CONSTRAINT PK_Asistencias PRIMARY KEY (AsId),
    CONSTRAINT FK_Asistencias_Materias_MaId FOREIGN KEY (MaId)
        REFERENCES Materias (MaId),
    CONSTRAINT FK_Asistencias_Usuarios_UsId FOREIGN KEY (UsId)
        REFERENCES Usuarios (UsId) ON DELETE CASCADE
);
GO

CREATE TABLE Inscripciones (
    InId                    INT NOT NULL IDENTITY(1,1),
    UsId                    INT NOT NULL,
    CaMaId                  INT NOT NULL,
    UsuariosUsId            INT NULL,
    CarreraMateriaCaMaId    INT NULL,
    CONSTRAINT PK_Inscripciones PRIMARY KEY (InId),
    CONSTRAINT FK_Inscripciones_Usuarios_UsuariosUsId FOREIGN KEY (UsuariosUsId)
        REFERENCES Usuarios (UsId)
);
GO

-- =====================================================================
-- Tablas puente Usuario <-> CarreraMateria / Rol
-- =====================================================================

CREATE TABLE UsuarioCarreraMateria (
    CarreraMateriasCaMaId  INT NOT NULL,
    UsuariosUsId            INT NOT NULL,
    CONSTRAINT PK_UsuarioCarreraMateria PRIMARY KEY (CarreraMateriasCaMaId, UsuariosUsId),
    CONSTRAINT FK_UsuarioCarreraMateria_Usuarios_UsuariosUsId FOREIGN KEY (UsuariosUsId)
        REFERENCES Usuarios (UsId) ON DELETE CASCADE
);
GO

CREATE TABLE UsuarioRoles (
    UsRoId  INT NOT NULL,
    UsId    INT NOT NULL,
    RoId    INT NOT NULL,
    CONSTRAINT PK_UsuarioRoles PRIMARY KEY (UsRoId),
    CONSTRAINT FK_UsuarioRoles_Usuarios_UsId FOREIGN KEY (UsId)
        REFERENCES Usuarios (UsId) ON DELETE CASCADE,
    CONSTRAINT FK_UsuarioRoles_Roles_RoId FOREIGN KEY (RoId)
        REFERENCES Roles (RoId)
);
GO

-- =====================================================================
-- Historial de migraciones de Entity Framework
-- =====================================================================

CREATE TABLE __EFMigrationsHistory (
    MigrationId     NVARCHAR(150)   NOT NULL,
    ProductVersion  NVARCHAR(32)    NOT NULL,
    CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
);
GO

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES
(N'20260605133142_InitialCreate', N'8.0.27'),
(N'20260605144324_UsuarioRelationships', N'8.0.27'),
(N'20260819165114_AgregarInscripcionesYCarrerasMaterias', N'8.0.27'),
(N'20260820000148_ConvertirCarrerasMateriasAsistenciasAIdentity', N'8.0.27');
GO

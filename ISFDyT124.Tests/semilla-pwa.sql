-- Semilla de la prueba de punta a punta de la PWA (contraseña de los usuarios: Prueba123). Los roles los crea la app al arrancar.
SET QUOTED_IDENTIFIER ON;
INSERT INTO Usuarios (UsId, UsApellido, UsNombre, UsDni, UsEmail, UsContrasena, UsActivo, RoId, UsTokenRecovery) VALUES
 (1,'Docente','Prueba',30000001,'docente@prueba.local','AQAAAAIAAYagAAAAENonQi9F6awVDE4oDSLy+mWviJoxP2Ihta0Fv9QIXUaOH+spciipNM8SbdEQx5piSA==',1,2,'tokenbloqueado'),
 (2,'Alumno','Uno',40000001,'a1@prueba.local','x',1,3,'t'),
 (3,'Alumno','Dos',40000002,'a2@prueba.local','x',1,3,'t');
INSERT INTO Carreras (CaDenominacion) VALUES ('Carrera E2E');
INSERT INTO Cohortes (CoId, CoAnio, CoEstado) VALUES (1, 2026, 1);
INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos, MaPorcentajeAsistenciaMinima) VALUES ('Materia E2E','Presencial',2,75);
INSERT INTO CarreraCohortes (CaId, CoId) SELECT TOP 1 CaId, 1 FROM Carreras;
INSERT INTO CarreraMateria (CaCoId, MaId) SELECT TOP 1 (SELECT TOP 1 CaCoId FROM CarreraCohortes), MaId FROM Materias;
INSERT INTO Inscripciones (UsId, CaMaId, InFechaInscripcion) SELECT u, (SELECT TOP 1 CaMaId FROM CarreraMateria), DATEADD(day,-30,GETDATE()) FROM (VALUES (2),(3)) v(u);
INSERT INTO UsuarioCarreraMateria (CarreraMateriasCaMaId, UsuariosUsId) SELECT TOP 1 CaMaId, 1 FROM CarreraMateria;

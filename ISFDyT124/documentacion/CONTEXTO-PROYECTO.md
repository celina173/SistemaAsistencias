# Contexto del proyecto — Sistema de Asistencias (ISFDyT124)

Documento de referencia para cualquiera que se sume al proyecto o retome
trabajo después de un tiempo. No reemplaza al `CLAUDE.md` de la raíz (reglas
de trabajo del repo) — lo complementa con el estado real del código: qué hay,
cómo está armado, y qué inconsistencias/bugs conocidos hay que tener en
cuenta antes de tocar algo.

## Qué es

Aplicación web ASP.NET Core MVC (.NET 8) para gestionar asistencias de un
instituto: usuarios con roles (Admin/Profesor/Alumno), carreras, materias,
cátedras (Carrera+Materia), cohortes, e inscripciones de alumnos a cátedras.
Autenticación por cookie, sin ASP.NET Identity (todo es manual: login por
DNI+contraseña, contraseña en texto plano — ver sección de deuda técnica).

## Cómo correrlo

Ver `CLAUDE.md` en la raíz del repo para comandos (`dotnet run`, migraciones,
cadena de conexión por rama). Resumen rápido: `appsettings.json` no está en
git (contiene la cadena de conexión real), cada rama de grupo usa su propio
servidor SQL Server — la de esta rama apunta a una base en Railway.

## Arquitectura

### Controllers (`Controllers/`)

| Controller | `[Authorize]` | Qué hace |
| --- | --- | --- |
| `AccountController` | solo `CambiarContrasena` | Login por DNI+contraseña (texto plano), arma la cookie con claims (Name, Email, Role), fuerza cambio de contraseña si contraseña==DNI, redirige según rol. |
| `AdminController` | `Roles="Admin"` (toda la clase) | Dashboard, alta/edición/baja de usuarios, ABM de usuarios, auditoría de actividad de docentes. |
| `AsistenciasController` | **ninguno** | Toma de asistencia y reporte global — filtra alumnos por rol `"estudiante"` (ver bug de nombres de rol más abajo). |
| `CarrerasController` | `Roles="Admin"` | CRUD estándar de Carrera. |
| `HomeController` | `[Authorize]` genérico | Home + vistas `Asistencia`/`AsistenciaGlobal` que parecen duplicados sin lógica de `AsistenciasController` (posible resto de una refactorización). |
| `InscripcionesController` | **ninguno** | Alta/edición de inscripciones alumno↔cátedra — también filtra por rol `"estudiante"`/`"Estudiante"` (mismo bug de nombres). |
| `MateriasController` | **ninguno** | CRUD estándar de Materia. |
| `ProfesorController` | `Roles="Profesor"` (toda la clase) | El docente ve sus cátedras y toma/edita asistencia de sus alumnos — filtra por rol `"Alumno"` (exacto, este sí matchea). |

**Ojo:** `MateriasController`, `InscripcionesController` y `AsistenciasController`
no tienen `[Authorize]` en ningún lado y no hay filtro global de autorización
en `Program.cs` — son accesibles sin login.

### Modelos (`Models/`)

- **Usuario** (`Us*`): `UsId` (PK **manual, no IDENTITY** — ver deuda técnica),
  `UsDni` (índice único), `UsEmail`, `UsContrasena` (texto plano), `RoId` (FK
  a Rol), `CaCoId` (nullable, solo para Alumnos). Relación muchos-a-muchos con
  `CarreraMateria` (cátedras que dicta, si es docente).
- **Rol** (`Ro*`): `RoId`, `RoDenominacion`. Sembrados por `Program.cs`:
  Admin(1), Profesor(2), Alumno(3).
- **UsuarioRol**: tabla puente Usuario↔Rol que **existe pero no se usa** —
  `Usuario.RoId` es la única fuente real de verdad del rol; `UsuarioRoles`
  solo se toca al eliminar un usuario (limpieza), nunca se popula al crear/editar.
- **Carrera** / **Materia** / **CarreraMateria** (la "cátedra", join
  Carrera+Materia) / **Cohorte** / **CarreraCohorte** (join Carrera+Cohorte,
  usado para ubicar a un Alumno en carrera+cohorte vía `Usuario.CaCoId`).
- **Asistencia** (`As*`): `AsFecha`, `AsPresente`, `AsJustificacion`, `UsId`,
  `MaId`, `CaMaId` (los últimos dos nullable). **`AsistenciasController` graba
  `CaMaId` y deja `MaId` null; `ProfesorController` graba `MaId` y deja
  `CaMaId` null** — dos caminos de código escriben columnas distintas de la
  misma fila (ya documentado como comentario en `AuditoriaDocentes`).
- **Inscripciones**: `InId`, `UsId`, `CaMaId` — la inscripción real de un
  alumno a una cátedra. No confundir con la relación muchos-a-muchos
  `Usuario.CarreraMaterias` (esa es para **docentes** asignados a cátedras,
  mapeada a una tabla sombra `UsuarioCarreraMateria` que no tiene clase C#).

### DbContext

Solo existe **`InstitutoDbContext`** (`Data/InstitutoDbContext.cs`). No hay
ningún `AsistenciaContext` en el código — si en algún momento se mencionó
como objetivo de convergencia, todavía no se creó ni está cableado.

Varias PKs están configuradas `ValueGeneratedNever()` (manuales, no IDENTITY):
`Rol`, `Usuario`, `CarreraMateria`, `CarreraCohorte`, `Cohorte`, `UsuarioRol`.
`Materia`, `Carrera` y `Asistencia` sí son IDENTITY (migradas más adelante).
**Importante:** al insertar un `Usuario` a mano hay que calcular el próximo
`UsId` libre (`MAX(UsId)+1`, patrón ya usado en `AdminController.UsuarioAgregar`
y en el seed de `Program.cs`) — no asumir que la base lo autogenera.

`Usuario.UsDni` tiene índice único — no se puede repetir DNI entre usuarios.

### DTOs (`DTO/`)

Un par Crear/Detalle por entidad (`UsuarioCrearDto`/`UsuarioDetalleDto`, etc.)
más `AuditoriaDocenteDto`, `HomeIndexDto`, `UsuarioLoginDto`.

### Vistas (`Views/`)

Carpetas: `Account`, `Admin`, `Asistencias`, `Carreras`, `Home`, `Inscripciones`,
`Materias`, `Profesor`, `Shared`. Las de `Home` (`Asistencia`,
`AsistenciaGlobal`) parecen duplicados sin lógica de las de `Asistencias` —
no se borraron porque sus acciones en `HomeController` siguen existiendo
(aunque nada las enlaza en la navegación).

## Autenticación y roles — inconsistencia conocida

El nombre de rol usado para "alumno" **no es consistente** en todo el código:

| String usado | Dónde | ¿Matchea contra el rol sembrado ("Alumno")? |
| --- | --- | --- |
| `"Alumno"` (exacto) | `ProfesorController.Asistencia` | ✅ Sí |
| `"estudiante"` (minúscula) | `AsistenciasController` (2 lugares), `InscripcionesController.AgregarInscripcionMateria` | ❌ No — nunca existe un rol con ese nombre |
| `"Estudiante"` (mayúscula inicial) | `InscripcionesController` (4 lugares más) | ❌ No |

**Consecuencia real:** las funciones de `AsistenciasController` e
`InscripcionesController` que buscan "estudiantes" por rol **no encuentran a
nadie**, porque el rol sembrado se llama "Alumno". Es la razón de fondo de
varios bugs ya diagnosticados (incluida la decisión explícita de dejar el
módulo Inscripciones sin arreglar por ahora — ver `documentacion/` y memoria
del proyecto). Cualquier corrección de este tipo de bug tiene que decidir
**a cuál de los dos nombres converger** (lo más simple: todo a `"Alumno"`,
que es el que ya está sembrado) y tocar todos los lugares de la tabla de
arriba a la vez, no uno solo.

Además hay una **tercera forma paralela** de identificar roles: número mágico
de `RoId` (`RoId==2` para docente, `RoId==3` para alumno) usada directamente
en `AdminController` — si el día de mañana cambia el orden de siembra de
roles, estos hardcodes se rompen sin avisar.

## Deuda técnica / decisiones conocidas (no arreglar sin que se pida)

- **Contraseñas en texto plano** — `Usuario.UsContrasena` se guarda y compara
  sin hash. Ticket pendiente: "Encriptar contraseñas" (crítico, EDT 2.3).
- **Módulo Inscripciones incompleto a propósito** — decisión explícita del
  equipo, queda para una iteración futura.
- **`AccessDeniedPath = "/Home/Privacy"`** en `Program.cs` apunta a una
  acción/vista que no existe → 404 si alguien sin rol intenta entrar a algo
  protegido.
- **`UsuarioRol` (tabla puente) no se usa** para nada más que limpieza al
  borrar un usuario — el rol real vive solo en `Usuario.RoId`.
- **`Asistencia.CaMaId` vs `Asistencia.MaId`** — dos controllers graban
  columnas distintas de la misma tabla según por dónde entrás a cargar
  asistencia (`AsistenciasController` vs `ProfesorController`).

## Git / ramas

Ver `CLAUDE.md` (reglas vigentes): `master` no se toca sin orden explícita,
todo el trabajo va a `Development`/ramas de grupo, la cadena de conexión de
`appsettings.json` no se versiona.

## Gestión del proyecto

- **EDT y cronograma de correcciones**: `documentacion/EDT-Cronograma-Correcciones.html`
  (también exportado a `.pdf`) — 6 frentes de trabajo repartidos entre los 10
  integrantes, hitos: **MVP 16/09**, **pre-entrega final 30/09**.
- **Diccionario de las 20 tareas** (con alcance de cada una) está en el mismo
  EDT.
- **Seguimiento de tickets**: tablero Trello vía la plataforma collabkit,
  isla "Sistema de Asistencias" (columnas: Bloqueado / Lista de tareas / En
  proceso / Hecho) — refleja el mismo EDT, con asignaciones ajustadas para
  minimizar dependencias entre personas.

## Dónde mirar para más detalle

- `documentacion/DOCUMENTACIÓN - PROGRAMACIÓN.md` — pedido original de
  correcciones (reunión con Magali), fuente del EDT.
- `documentacion/auditoria-docentes.md` — decisiones de diseño de la
  pantalla de Auditoría de Docentes.
- `documentacion/Gustavo vistas documentacion.md` — investigación de bugs de
  diseño en las vistas de Materias/Usuarios/Carreras (clases CSS inexistentes,
  Bootstrap no cargado).

================================================================================
  READMEGUSTAVO — Módulo Profesor y trabajo sobre la rama
  Rama de trabajo: grupo2-010726-Gustavo (basada en origin/master)
================================================================================

--------------------------------------------------------------------------------
1. CONTEXTO DE RAMAS
--------------------------------------------------------------------------------
- Se creó la rama grupo2-010726-Gustavo a partir del master remoto (origin/master),
  que es la única rama tomada como fuente de verdad.
- El master local estaba desactualizado (33 commits atrás); se sincronizó con
  origin/master por fast-forward.
- Luego se trajeron a esta rama los últimos cambios de master.

Cadena de conexión: se usa siempre la de Grupo-2 (servidor MUCHI) en appsettings.json,
dejando las de master (Santiago / localhost) COMENTADAS (no se borran). El archivo
appsettings.json está en .gitignore (config local).

    Server=MUCHI;Database=Instituto;Trusted_Connection=True;TrustServerCertificate=True;

--------------------------------------------------------------------------------
2. OBJETIVO: UNIFICAR/ORDENAR EL MÓDULO PROFESOR
--------------------------------------------------------------------------------
El módulo docente venía desalineado. Antes -> Ahora:
  - Archivo:               ProfesorController.cs (igual)
  - Clase:                 DocenteController   ->  ProfesorController
  - Rol exigido:           "Docente" (no existe) -> "Profesor" (rol real sembrado)
  - Redirección de login:  case "DOCENTE" (nunca matcheaba) -> case "PROFESOR"
  - Vistas:                no existían         ->  Views/Profesor/ creadas
  - Datos hacia la vista:  entidades + ViewBag ->  DTOs

Se eligió la Opción B: un ProfesorController dedicado (convención MVC), en vez de
mezclarlo en un UsuarioController.

--------------------------------------------------------------------------------
3. CAMBIOS REALIZADOS
--------------------------------------------------------------------------------
3.1. ProfesorController (refactor a DTOs) — ISFDyT124/Controllers/ProfesorController.cs
     - Clase renombrada a ProfesorController con [Authorize(Roles = "Profesor")].
     - Tres secciones, todas usando los DTOs de la carpeta /DTO:
         Sección 1 - Dashboard de cátedras    -> Index()                  -> CarreraMateriaDetalleDto
         Sección 2 - Tomar/editar asistencia  -> Asistencia() GET/POST    -> UsuarioDetalleDto / AsistenciaCrearDto
         Sección 3 - Historial                -> HistorialAsistencias()   -> AsistenciaDetalleDto
     - Alcance del profesor: solo toma asistencia de SUS materias y consulta el historial.
     - La toma de asistencia hace upsert en lote (inserta o actualiza el registro del día) con PK manual.

3.2. Vistas nuevas (Views/Profesor/) — replican el estilo de las maquetas de Views/Home/,
     integradas al _Layout.cshtml y dinámicas (con @model DTO):
         Index.cshtml                 <- estilo de Admin/UsuariosABM.cshtml (main-container-gestion, crud-table)
         Asistencia.cshtml            <- estilo de Home/Asistencia.cshtml   (attendance-table, custom-checkbox, btn-guardar)
         HistorialAsistencias.cshtml  <- estilo de Home/AsistenciaGlobal    (global-table, title-cell)
     No se agregó CSS nuevo: se reutilizaron las clases existentes.

3.3. Login — ISFDyT124/Controllers/AccountController.cs
     - El switch de redirección post-login ahora usa case "PROFESOR" -> ProfesorController.

3.4. Documentación para frontend — REVISION-FRONTEND-vistas-profesor.txt
     - Pide revisar las 3 vistas nuevas y define puntos abiertos (principalmente: la maqueta
       mostraba 4 "módulos" por alumno, pero el modelo guarda un solo Presente/día).

--------------------------------------------------------------------------------
4. INTEGRACIÓN DEL ARREGLO DE MASTER (BUILD)
--------------------------------------------------------------------------------
- Master había quedado roto por un rename incompleto UsDni -> UsDNI + DbSets en singular
  (17 errores de compilación).
- Otro alumno lo arregló en origin/master con 2 commits: d2ffc7c "Revertir a ba762f0"
  (revierte el rename) y f8d0d79 (limpia .gitignore y borra un archivo vacío).
- Se mergeó origin/master en esta rama (commit merge 5bfa970), SIN conflictos.
- Resultado: el nombre canónico vuelve a ser UsDni y los DbSets quedan en plural
  (CarreraCohortes / CarreraMaterias), consistentes con el InstitutoDbContext.

Estado del build: OK
  - El proyecto compila: 0 errores.
  - La app arranca y conecta a la base MUCHI (Now listening on http://localhost:5189).
  - Rutas verificadas:
      GET /Account/Login          -> 200 OK
      GET /Profesor (sin auth)    -> 302 (redirige al login) => el ProfesorController existe,
                                     está ruteado y protegido por rol. (Antes daba 404.)

--------------------------------------------------------------------------------
5. REVISIÓN DE ESTILOS (CSS DE ROSELEN)
--------------------------------------------------------------------------------
- Los estilos de Roselen (commit 31b0022 "VISTAS") YA están incorporados en esta rama y en
  master (el style.css es idéntico, 1885 líneas).
- De las 21 clases que usan las vistas Profesor, 16 están definidas por el CSS de Roselen
  (tablas, checkboxes, botones, banners) => Asistencia e HistorialAsistencias se ven bien.
- 5 clases NO están definidas en el CSS (por nadie), y también las usan vistas existentes
  (Admin/UsuariosABM, Home/Asistencia), o sea es un hueco preexistente:
      .main-container-gestion, .crud-table-container, .action-buttons,
      .modules-cell, .justification-cell
- Recomendación: que el equipo de frontend (Roselen) las agregue al style.css
  (no se tocó el CSS compartido desde este módulo).

--------------------------------------------------------------------------------
6. ARREGLO DEL CSS DEL LOGIN
--------------------------------------------------------------------------------
Archivo: ISFDyT124/Views/Account/Login.cshtml
- Problema: la vista (Layout = null) referenciaba los recursos con rutas RELATIVAS, que al
  servirse en /Account/Login resolvían a /Account/... -> 404 -> login sin estilos y logo roto.
- Fix aplicado (a rutas root-relative ~/, igual que el _Layout):
      href="style.css"     (pedía /Account/style.css)      -> href="~/css/style.css"
      src="img/logo.png"   (pedía /Account/img/logo.png)   -> src="~/images/logo.png"
      src="script.js"      (pedía /Account/script.js)       -> src="~/js/script.js"
- Nota: el link "Olvidé mi contraseña" apunta a recupero-contraseña.html (ruta rota), pero es
  tema de navegación, no de CSS. Se dejó como está.

--------------------------------------------------------------------------------
7. PENDIENTES
--------------------------------------------------------------------------------
[ ] Definir con frontend "módulos vs presente" y el formato del historial.
[ ] Agregar al style.css las 5 clases de layout faltantes (frontend/Roselen).
[ ] Agregar el acceso "Profesor" en el menú lateral de _Layout.cshtml.
[ ] Sembrar datos de prueba (docente + cátedras + inscripciones) para ver el flujo con datos.
[ ] (Opcional) Arreglar el link "Olvidé mi contraseña" del Login.
================================================================================

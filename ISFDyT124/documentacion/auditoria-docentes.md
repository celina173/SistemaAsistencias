# Auditoría de Docentes — decisiones de diseño

## El problema que la originó

La tarjeta "Docentes" del panel Admin enlazaba a `Profesor/Index`, que es la
pantalla personal del propio docente logueado (`[Authorize(Roles = "Profesor")]`).
Como el Admin no tiene ese rol, la app lo redirigía a la página de acceso
denegado configurada en `Program.cs` (`/Home/Privacy`) — que ni siquiera existe
como acción — y terminaba en un 404.

Al revisar qué debería mostrar esa tarjeta en su lugar, se definió que el
Admin no necesita "entrar como si fuera un docente" a ver sus propias
cátedras: necesita **supervisar** a todos los docentes. De ahí nace la
Auditoría de Docentes.

## Qué muestra

Una tabla con una fila por cada combinación **(docente, cátedra que tiene
asignada)**:

| Columna | Qué informa |
|---|---|
| Docente | Apellido y nombre |
| Carrera / Materia | La cátedra puntual |
| Alumnos | Cuántos alumnos están inscriptos en esa cátedra |
| Fechas cargadas | Cuántas fechas distintas tienen asistencia registrada |
| Última carga | La fecha más reciente con asistencia cargada |
| Estado | "Sin actividad" (resaltado en rojo) si nunca cargó nada |

**Orden:** las cátedras sin ninguna carga aparecen primero, y después el
resto ordenado por última carga ascendente — lo más urgente de revisar queda
arriba, sin que el Admin tenga que buscarlo.

## Por qué sirve

Antes de esto, no existía ninguna pantalla donde un Admin pudiera responder
rápido a "¿qué docentes no están usando el sistema?" o "¿hace cuánto que tal
cátedra no tiene asistencia cargada?". Esa información estaba dispersa:

- La lista de qué cátedras tiene cada docente ya se veía en `Usuarios` (panel
  Admin), pero mezclada en una sola celda de texto y sin ningún dato de
  actividad.
- El detalle de fechas de asistencia solo lo puede ver el propio docente
  (`Profesor/HistorialAsistencias`), materia por materia — el Admin no tiene
  acceso, y no hay forma de verlas todas juntas.
- `Asistencias/AsistenciaGlobal` sí es una pantalla para el Admin, pero está
  armada para auditar **alumnos** (% de asistencia por alumno), no para
  detectar qué **docente** dejó de cargar.

La Auditoría de Docentes es la primera pantalla que junta "quién" (docente),
"dónde" (cátedra) y "cuándo fue la última vez que hizo algo" en un solo
lugar, ordenada para que lo más preocupante aparezca primero.

## Decisiones de diseño y por qué se tomaron

### 1. Fase 1: sin tocar el esquema de la base

La tabla `Asistencias` no guarda ni la fecha en la que se cargó el registro
(`AsFecha` es la fecha de la clase, no de carga) ni quién lo cargó. Tampoco
existe una tabla de "calendario de clases esperado" contra la cual comparar.

Para tener trazabilidad real (fecha de carga exacta, usuario que cargó, y
compatibilidad correcta cuando una cátedra tiene más de un docente asignado)
habría que agregar columnas nuevas a `Asistencias` — es decir, otra migración
sobre la base compartida en Railway.

Se decidió explícitamente **no hacer eso en esta primera versión**: se
construyó con los datos que ya existen, aceptando que es una auditoría de
*actividad* (¿cargó, cuánto, cuándo fue la última vez que hay un registro?)
y no de *cumplimiento* estricto contra un calendario. Si más adelante hace
falta más precisión, la fase 2 (agregar esas columnas) queda planteada pero
no implementada.

### 2. Unidad de auditoría: (docente, cátedra), no "una fila por cátedra"

Se confirmó revisando el modelo que `UsuarioCarreraMateria` es una relación
muchos a muchos real — no hay ninguna restricción que impida que una cátedra
tenga más de un docente asignado. Por eso la tabla no agrupa por cátedra
sola: agrupa por el par docente+cátedra, para no ocultar información si en
algún momento se usa esa flexibilidad.

### 3. Se matchea la asistencia por `MaId`, no por `CaMaId`

Al arreglar antes el bug del guardado de asistencia (`AsId` como columna
IDENTITY) se había detectado que `ProfesorController` guarda cada asistencia
con `MaId` pero deja `CaMaId` en `NULL`. La auditoría respeta ese mismo
criterio para contar "fechas cargadas" — matchea por `MaId`, igual que ya
hace el propio `ProfesorController` al buscar si una asistencia "ya existe".
Si se hubiera matcheado por `CaMaId`, la auditoría mostraría 0 actividad en
casi todas las cátedras, porque ese campo casi nunca se completa hoy.

### 4. No se reutilizó el mockup `gestion-docentes.cshtml`

Ya existía un archivo `Views/Admin/gestion-docentes.cshtml` con una tabla de
"docentes", pero es un prototipo estático: HTML completo standalone (no usa
`_Layout`), con datos de ejemplo hardcodeados ("Pérez Juan Carlos") y links a
páginas `.html` que no existen en el proyecto MVC. Ningún controlador lo
servía. Se optó por no partir de ahí para no duplicar la lógica de usuarios
que ya existe y funciona (`AdminController.UsuariosABM` y compañía) — ese
archivo queda como código muerto, sin usar.

### 5. Estilo visual: se clonó `AsistenciaGlobal`, no `UsuariosABM`

La auditoría es un reporte de solo lectura (sin agregar/editar/borrar), así
que se copió la estructura de `Views/Asistencias/AsistenciaGlobal.cshtml`
(otro reporte de solo lectura ya existente) en vez de la de `UsuariosABM`
(que es un ABM con buscador y botón "+"). Se reutilizan las clases CSS que
ya existían — `.global-table`, `.title-cell`, `.total-col` — y para resaltar
"Sin actividad" se usa el mismo rojo que ya usa el resto de la app en los
botones de eliminar (`#ef4444`), en vez de inventar un color o componente
nuevo.

## Archivos

- `ISFDyT124/DTO/AuditoriaDocenteDto.cs` (nuevo)
- `ISFDyT124/Controllers/AdminController.cs` — acción `AuditoriaDocentes()`
- `ISFDyT124/Views/Admin/AuditoriaDocentes.cshtml` (nuevo)
- `ISFDyT124/Views/Admin/Index.cshtml` — la tarjeta "Docentes" apunta acá en
  vez de a `Profesor/Index`

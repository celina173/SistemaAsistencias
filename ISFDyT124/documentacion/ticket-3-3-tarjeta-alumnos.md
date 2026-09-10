# Tarjeta "Alumnos" (ex-Docentes) — descripción y decisiones de diseño (ticket 3.3)

> **Nomenclatura — regla:**
> - **En pantalla, siempre "Estudiantes"**: etiqueta de la tarjeta, títulos de
>   pantalla, encabezados de tabla, botones, mensajes de validación y de
>   confirmación. Nada visible dice "Alumno".
> - **En el código se mantiene "alumno/alumnos"** para nombres de controlador,
>   acciones, DTOs, modelos, archivos de vista y variables. Renombrarlos a esta
>   altura es caro y no aporta valor. Ya hay código que usa esa palabra
>   (`ProfesorController`, variables `alumnos`, etc.) — se sigue esa convención.
> - El vínculo real con la persona es el rol `Estudiante` (`RoId = 3`), definido en
>   el ticket 3.2.

## El problema que lo originó

Texto del EDT (`EDT-Cronograma-Correcciones.html`, fila 3.3):

> Renombrar la tarjeta del panel Admin y reorientar su función: pasa a
> listar/gestionar alumnos asignados a cada carrera, no docentes.

El ticket es anterior al renombrado de roles del ticket 3.2 (viene del pedido de
Magali del 02/09, antes de que el equipo definiera "Estudiante"). Por eso el EDT
habla de "alumnos" — es desactualización, no una contradicción. Se implementa con
la nomenclatura actual: **Estudiante**.

## Alcance acordado (definiciones del 09/09/2026)

1. **El estudiante es un DATO, no un usuario que se loguea.** No tiene portal ni
   login propio. Lo gestionan Admin, Dirección y Docentes.
2. **Por ahora el estudiante sigue viviendo en la tabla `Usuarios`** (con
   `RoId = 3`, Estudiante). No se crea tabla/modelo `Estudiante` propio en esta
   etapa. A futuro la profesora contempla mover la lista de estudiantes a un
   almacén aparte (SQL Server Compact / `.sdf`); eso queda **fuera del alcance del
   MVP** y debería ser su propio ticket.
3. **La tarjeta lleva a una lista filtrada, no a una pantalla nueva agrupada por
   carrera** (se descartó construir una vista nueva desde cero — "Opción B").
4. **Un Docente puede dar de alta, editar y borrar estudiantes.** Un Admin (y
   Dirección) también.
5. **Un Docente solo ve los estudiantes que le corresponden**, no toda la matrícula
   del instituto. Ver "Alcance del docente" abajo.
6. **Etiqueta visible: "Estudiantes"** en todas las vistas.

## Resumen de los cambios (en criollo)

Qué va a ver y hacer el usuario cuando esto esté listo:

- En el **panel Admin**, la tarjeta que hoy dice "Docentes" va a decir
  **"Estudiantes"** y va a abrir una **lista de estudiantes**. Al lado aparece una
  tarjeta nueva **"Auditoría docentes"** con el reporte que antes abría la tarjeta
  "Docentes" (ese reporte no cambia, solo se mueve de lugar). El panel pasa de 4 a
  5 tarjetas.
- La **lista de estudiantes** es la misma pantalla de usuarios que ya existe, pero
  filtrada para mostrar solo estudiantes. Tiene los botones **Agregar / Editar /
  Eliminar** que ya funcionan.
- Un **docente** también entra a esa lista (desde su propio panel) y puede
  **agregar, editar y borrar estudiantes**. Pero solo ve los estudiantes de **sus
  carreras** (las carreras de las materias que dicta), no todos los del instituto.
  Un admin sí ve todos.
- Cuando un docente o admin **da de alta un estudiante**, el formulario le pide
  nombre, apellido, DNI, email y carrera/cohorte. No le pide rol ni contraseña (el
  estudiante no se loguea).

Qué NO entra en este ticket (queda para tickets futuros):

- Que el docente vea estudiantes **por materia puntual** en vez de por carrera. Hoy
  el sistema no sabe qué materias cursa cada estudiante. Ver más abajo.
- Alta masiva de estudiantes por Excel (ya es el ticket 3.4).

## Diseño para el MVP

### Estudiantes en `Usuarios`

Un estudiante es una fila en `Usuarios` con `RoId = 3`. La columna `UsContrasena`
es `NOT NULL`: al crear un estudiante se le guarda el hash de su DNI, igual que hoy
en `UsuarioAgregar`. Es inofensivo — nunca inicia sesión. **No se toca el esquema.**

### La tarjeta y la Auditoría de Docentes

Hoy el panel (`Views/Admin/Index.cshtml`) tiene 4 tarjetas: **Usuarios · Materias ·
Carreras · Docentes**. La tarjeta "Docentes" (`card-docentes`, ícono
`bi-person-workspace`) **no gestiona docentes**: apunta a
`AdminController.AuditoriaDocentes`, un reporte de solo lectura de actividad
docente (ver `auditoria-docentes.md`).

Cambios:

1. La tarjeta `card-docentes` se **renombra a "Estudiantes"** (texto visible) y pasa
   a apuntar a la nueva lista (`AlumnosController.Index`). Se le cambia el ícono a
   uno de personas (p. ej. `bi-people` o `bi-person-badge`).
2. Se agrega una **5ª tarjeta "Auditoría docentes"** que apunta a
   `AdminController.AuditoriaDocentes` (la acción y su vista **no se tocan**, solo
   cambia desde dónde se entra). Ícono: se reusa `bi-person-workspace`.

Por qué una tarjeta nueva y no esconderla dentro de "Usuarios": la auditoría junta
información que no está en ninguna otra pantalla ("¿qué docente dejó de cargar
asistencia?"). Enterrarla en un submenú equivale a que nadie la use.

El panel queda con 5 tarjetas. **No hace falta tocar el CSS:** `.cards-grid` es
`display: flex; flex-wrap: wrap; justify-content: center`, así que las 5 tarjetas
se reacomodan solas (3 + 2).

### Controller nuevo `AlumnosController` (no se abre el CRUD de usuarios)

El CRUD de usuarios vive hoy en `AdminController`
(`[Authorize(Roles = "Admin,Dirección")]`). **No se le puede agregar "Docente" a
ese `[Authorize]`:** `UsuarioAgregar` / `UsuarioEditar` / `UsuarioEliminar` son
CRUD genérico y pueden crear o editar *cualquier* rol. Un docente con ese acceso
podría promoverse a Admin o borrar al admin — escalada de privilegios.

Solución: **`AlumnosController` separado** (nombre de código; en pantalla dice
"Estudiantes"), con `[Authorize(Roles = "Admin,Dirección,Docente")]` y acciones
acotadas:

| Acción | Regla |
| --- | --- |
| `Index` | Lista solo filas con rol `Estudiante`, acotada según el rol de quien mira (ver abajo). |
| `Agregar` | Fuerza `RoId = 3`. El formulario **no** muestra selector de rol ni de contraseña ni de materias. Solo datos de la persona + carrera/cohorte (`CaCoId`). |
| `Editar` | Rechaza (404 / Forbid) si la fila objetivo no es `Estudiante`. Un docente no puede editar a otro docente ni al admin por esta vía. |
| `Eliminar` | Misma validación que `Editar`. |

Reusa los DTOs y las listas de carrera/cohorte que ya existen en
`AdminController.CargarListasFormularioUsuarioAsync`.

### Alcance del docente — Lectura A (la que ya usa el sistema)

**Para el MVP, "los estudiantes del docente" = los estudiantes de las carreras de
sus cátedras.**

Es exactamente lo que ya hace la planilla de asistencia
(`ProfesorController.Asistencia`, `ProfesorController.cs:106`): toma los estudiantes
cuya `CaCoId` pertenece a la carrera de la cátedra. No agrega tablas ni datos
nuevos.

- Admin y Dirección: ven **todos** los estudiantes.
- Docente: ve la unión de estudiantes de las carreras en las que tiene al menos una
  cátedra asignada (`Usuario.CarreraMaterias` → `Carrera` → `CarreraCohortes` →
  `Usuarios.CaCoId`).
- Punto de entrada para el docente: agregar el acceso en `ProfesorController.Index`
  (panel del docente), además de la tarjeta del panel Admin.

## El problema NO resuelto en el MVP (para la asignación de tickets)

**Lo que Pablo pidió idealmente:** que el docente vea *solo los estudiantes de la
materia puntual que está dictando*, no los de toda la carrera.

**Por qué no entra en el MVP:** el modelo de datos actual no representa la
inscripción de un estudiante a una materia.

- La tabla `Inscripciones` (`UsId`, `CaMaId`) existe para eso, pero **ningún flujo
  la llena** para estudiantes. Está vacía en la práctica.
- Todo el sistema (asistencia incluida) asume hoy "estudiante pertenece a una
  carrera" vía `Usuarios.CaCoId`, no "estudiante cursa estas materias".
- Si un docente dicta dos materias de la misma carrera, con el modelo actual ve la
  misma lista de estudiantes en ambas.

**Lo que haría falta para la "Lectura B" (inscripción real por materia):**

1. Un flujo de **inscripción de estudiante a materias**: UI para asignar a cada
   estudiante las cátedras (`CarreraMateria`) que cursa, y persistirlo en
   `Inscripciones`.
2. Definir **quién inscribe**: ¿el Admin/Dirección al dar de alta? ¿el propio
   docente sobre su cátedra? ¿carga masiva?
3. Migrar las consultas que hoy resuelven "estudiantes de la cátedra" por `CaCoId`
   (empezando por `ProfesorController.Asistencia`) para que usen `Inscripciones`.
4. Decidir qué pasa con `UsuarioCarreraMateria` (join implícito que generó EF,
   redundante con `Inscripciones`) — unificar en una sola relación.

**Relación con otros tickets:**

- **5.1** ("rol estudiante inexistente") ya empuja hacia usar `Inscripciones` en
  vez de filtrar por rol. La Lectura B es la continuación natural.
- **3.4** ("carga masiva de alumnos vía Excel", Post-MVP) probablemente sea el
  vehículo para poblar `Inscripciones` a escala.

**Tickets nuevos sugeridos (a numerar por el equipo):**

- *Inscripción de estudiantes a materias*: modelo de datos + UI + define quién
  inscribe. Habilita que el docente vea estudiantes por materia y no por carrera.
  Bloquea la "Lectura B" de 3.3. (MVP+1 / Post-MVP según prioridad.)
- *Migrar "estudiantes de la cátedra" a `Inscripciones`*: reemplaza el filtro por
  `CaCoId` en `ProfesorController.Asistencia` y donde aplique. Depende del
  anterior.
- *Almacén separado de estudiantes (SQL Server Compact)*: si se confirma sacar la
  matrícula de `Usuarios`. Impacta el diseño de este ticket — conviene definirlo
  antes de invertir en UI de gestión masiva sobre `Usuarios`.

## Pendiente de definición

- Alcance de "gestionar": el MVP cubre alta / edición / baja individual. Edición
  en lote (reasignar carrera de varios estudiantes, baja masiva) **no** está en
  ningún ticket — evaluar si hace falta.

## Archivos que tocaría el MVP (estimado)

- `Controllers/AlumnosController.cs` — **nuevo**
- `Views/Alumnos/` — **nuevo** (`Index`, `Agregar`, `Editar`; versiones recortadas
  de las de `Admin/Usuario*`). Todo el texto visible dice "Estudiantes".
- `Views/Admin/Index.cshtml` — renombrar tarjeta (a "Estudiantes"), redirigir,
  agregar 5ª tarjeta "Auditoría docentes"
- `Controllers/ProfesorController.cs` / `Views/Profesor/Index.cshtml` — acceso a la
  lista desde el panel del docente
- DTOs — reusar `UsuarioCrearDto` / `UsuarioDetalleDto` o derivar unos propios
  (nombre de código con "alumno")

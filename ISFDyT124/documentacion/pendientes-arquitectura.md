# Reporte de pendientes — lo que todavía no está resuelto

> Consolidado al 10/09/2026. Junta la deuda y los pendientes anotados en
> [[revision-usuario-agregar]] (3.1), [[correccion-roles-sembrados]] (3.2) y
> [[ticket-3-3-tarjeta-alumnos]] (3.3), verificados contra el código actual de
> `Development`. Ordenado por severidad.

---

## A. Bugs conocidos sin corregir

### A.1 — `CambiarContrasena` no respeta el rol al redirigir
`AccountController.CambiarContrasena` (POST, `AccountController.cs:168`) termina
siempre en `RedirectToAction("Index", "Home")`, a diferencia de `Login`, que sí
hace `switch` por rol.

Todo usuario nuevo se crea con contraseña = DNI y `Login` lo fuerza a pasar por
este flujo en su primer ingreso (`AccountController.cs:111`). Consecuencia:
**cualquier alta reciente — Admin, Docente o Dirección — cae en Home la primera
vez** en lugar de su panel. Recién en el segundo login llega a donde corresponde.

Es un bug preexistente (no lo introdujeron 3.1/3.2), detectado probando el alta
de usuarios. Sin ticket propio.

**Arreglo:** replicar el `switch` de `Login` (`AccountController.cs:115-125`) en el
`return` de `CambiarContrasena`, o extraer ese `switch` a un helper y llamarlo
desde los dos lados.

### A.2 — Dirección redirige al panel de Admin "por ahora"
`Login` manda el rol Dirección a `RedirectToAction("Index", "Admin")` con el
comentario *"Por ahora; hasta que definamos si tendrá una vista aparte, o
permisos especiales"* (`AccountController.cs:122`). Falta la definición del
equipo: ¿Dirección usa el mismo panel que Admin o tiene el suyo?

---

## B. Deuda técnica (funciona, pero está frágil o inconsistente)

### B.1 — El campo "Contraseña" del alta de usuario es código muerto
`UsuarioAgregar.cshtml` muestra un input de contraseña, pero el POST lo ignora y
siempre guarda `PasswordService.HashPassword(model.UsDni.ToString())`
(`AdminController.cs:232`). Lo tipeado no se bindea ni se usa. Confunde a quien
carga usuarios. O se saca el campo, o se lo respeta.

### B.2 — `RoId` en `UsuarioCrearDto` tiene annotations que no hacen nada
El DTO trae `[Required]` / `[ForeignKey]` sobre `RoId`, pero el rol real se
bindea desde el parámetro suelto `selectedRoleId` del método de acción, no desde
esa propiedad. Limpieza pendiente.

### B.3 — Números mágicos de rol (`2` Docente, `3` Estudiante) hardcodeados
Aparecen sueltos en:
- `Views/Admin/UsuarioAgregar.cshtml:100-101`, `UsuarioEditar.cshtml:100-101` (JS
  de campos condicionales).
- `AdminController.cs` líneas 52, 54, 77, 322, 339, 596, 697.
- `AlumnosController.cs` (filtro `RoId = 3`).

3.2 mantuvo deliberadamente esos números para no romper este código, pero sigue
siendo frágil ante cualquier renumeración futura de roles. Falta una constante o
un enum único.

### B.4 — `AdminController.UsuarioEditar` valida poco
El POST de edición usa `UsuarioDetalleDto` (`AdminController.cs:291`), que **no
tiene DataAnnotations**. El alta (`UsuarioCrearDto`) sí valida. En
`AlumnosController` este hueco ya se tapó con `AlumnoFormDto`, pero el CRUD
genérico de admin quedó igual: se puede editar un usuario con datos inválidos.

### B.5 — Dos relaciones para lo mismo: `UsuarioCarreraMateria` vs `Inscripciones`
`UsuarioCarreraMateria` es un join implícito que generó EF (tabla real, columnas
`CarreraMateriasCaMaId` / `UsuariosUsId`, ver `Data/InstitutoDbContext.cs:124`),
redundante con `Inscripciones` (`UsId`, `CaMaId`). Falta decidir cuál queda y
unificar.

### B.6 — Mapeo inconsistente de `Inscripciones`
Las FK "reales" del modelo son columnas *shadow* nullables sin cascade
(`UsuariosUsId` / `CarreraMateriaCaMaId`); las columnas planas `UsId` / `CaMaId`
son las que efectivamente se escriben y por las que joinean el código de 5.1 y el
de 3.3. Por eso el borrado de un estudiante necesita `RemoveRange` explícito
sobre sus inscripciones (no hay cascade). Conviene alinear el mapeo.

### B.7 — Dos caminos distintos para armar la planilla de asistencia
- `AsistenciasController` (ticket 5.1, `AsistenciasController.cs:130` y `:271`) ya
  arma la lista de estudiantes desde **`Inscripciones`**.
- `ProfesorController.Asistencia` (`ProfesorController.cs:106-113`) sigue
  armándola por **`Usuarios.CaCoId` + `RoId Estudiante`**.

Los dos renderizan planillas de asistencia con criterios divergentes. Falta
migrar `ProfesorController` a `Inscripciones` para que coincidan (ver C.1).

---

## C. Funcionalidad no implementada (candidata a tickets nuevos)

### C.1 — El docente ve estudiantes por carrera, no por materia
Lo ideal (pedido de Pablo): que el docente vea sólo los estudiantes de la materia
puntual que dicta. Hoy `AlumnosController` y `ProfesorController.Asistencia` los
filtran por carrera (`Usuarios.CaCoId`). Para la "Lectura B" (por materia) hace
falta:
1. Un flujo de inscripción estudiante ↔ cátedra que persista en `Inscripciones`.
2. Definir **quién inscribe** (Admin al dar de alta / el docente sobre su cátedra
   / carga masiva).
3. Migrar `ProfesorController.Asistencia` de `CaCoId` a `Inscripciones`.
4. Resolver B.5 (unificar con `UsuarioCarreraMateria`).

La carga masiva de 3.4 y el alta individual de 3.3 ya empezaron a poblar
`Inscripciones`, así que el flujo existe parcialmente pero no está completo ni es
la fuente de verdad en todos lados.

### C.2 — Almacén separado de estudiantes (SQL Server Compact / `.sdf`)
La profesora contempla sacar la matrícula de `Usuarios` a un almacén aparte.
Fuera del alcance del MVP, pero **impacta el diseño de la gestión de estudiantes**
(3.3): conviene decidirlo antes de invertir más en UI sobre `Usuarios`.

### C.3 — Edición en lote de estudiantes
El MVP de 3.3 cubre alta / edición / baja individual. Reasignar carrera a varios
estudiantes o baja masiva no está en ningún ticket. Evaluar si hace falta.

### C.4 — CRUD de `Rol` inexistente
No hay ninguna pantalla para gestionar la entidad `Rol` (`RolCrearDto` /
`RolDetalleDto` existen en `DTO/` pero no los usa nadie). El día que se
construya, hay que restringirla a `Admin` **excluyendo** a `Dirección` (decisión
de 3.2).

---

## D. Pruebas pendientes

### D.1 — Ticket 3.3: flujos autenticados end-to-end
Falta probar `AlumnosController` logueado como Admin y como Docente contra la base
de Railway: que el docente sólo vea/gestione los estudiantes de sus carreras, que
el alta cree bien las `Inscripciones`, que la baja las borre. **Bloqueado:** no
hay credencial de admin válida contra Railway (ninguna de las conocidas funciona;
la tiene que pasar Gustavo).

### D.2 — Ticket 3.2: seed contra la base del servidor
El arranque se verificó contra la base real, pero conviene reconfirmar que el
seed de roles corre sin conflictos de PK en cualquier base que ya tenía el seed
viejo, y probar el login con los 4 roles.

---

## Resumen para asignar

| ID | Qué | Tipo | Prioridad sugerida |
| --- | --- | --- | --- |
| A.1 | `CambiarContrasena` ignora el rol al redirigir | bug | alta — afecta a todo usuario nuevo |
| A.2 | Definir panel de Dirección | decisión | media |
| B.1 | Sacar/respetar el campo contraseña del alta | limpieza | media |
| B.3 | Constante/enum para los RoId | limpieza | media |
| B.4 | Validar `UsuarioEditar` (admin) | bug leve | media |
| B.5–B.7 | Unificar `Inscripciones` / `UsuarioCarreraMateria` y los dos caminos de asistencia | refactor | alta si entra C.1 |
| C.1 | Inscripción real por materia | feature | MVP+1 / Post-MVP |
| C.2 | Almacén separado de estudiantes | decisión de diseño | antes de escalar 3.3 |
| C.3 | Edición en lote de estudiantes | feature | a evaluar |
| C.4 | CRUD de Rol + permiso de Dirección | feature | cuando se pida |
| D.1 | Prueba autenticada de 3.3 | test | apenas haya credencial |
| D.2 | Prueba del seed 3.2 en server | test | baja |

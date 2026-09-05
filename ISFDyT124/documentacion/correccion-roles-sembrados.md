# Corrección de roles sembrados — decisiones de diseño (ticket 3.2)

## El problema que lo originó

El sistema sembraba 3 roles en `Program.cs`: `Admin`, `Profesor` y `Alumno`. En
la reunión con Magali (16 pedidos de corrección, ver
`DOCUMENTACIÓN - PROGRAMACIÓN.md`) se pidió revisar esos roles: debían quedar
`Admin` (desarrolladores del sistema), `Dirección` (directivos del instituto)
y `Docente`/`Alumno` según lo que ya existiera.

Al revisar el código aparecieron dos problemas más, no visibles desde el
pedido original:

- Varias consultas (`AsistenciasController`, `InscripcionesController`)
  buscaban un rol llamado `"estudiante"`/`"Estudiante"` que **nunca se
  sembraba** — el rol real se llamaba `"Alumno"`. Es el bug ya anotado como
  "5.1: rol estudiante inexistente" en el EDT.
- `MateriasController` no tenía ningún `[Authorize]` — cualquier usuario
  logueado, sin importar el rol, podía crear/editar/borrar materias.

## Roles finales

| Rol | RoId | Antes |
| --- | --- | --- |
| Admin | 1 | sin cambios |
| Docente | 2 | "Profesor" |
| Estudiante | 3 | "Alumno" |
| Dirección | 4 (nuevo) | no existía |

**Permisos de Dirección:** equivalentes a Admin (accede a todo lo que hoy
requiere `[Authorize(Roles = "Admin")]`), salvo gestión de roles — esto
último no aplica todavía porque no existe ningún CRUD sobre la entidad `Rol`
en el sistema (`RolCrearDto`/`RolDetalleDto` están en `DTO/` pero no los usa
ningún controlador). Queda como restricción a aplicar el día que se
construya esa pantalla.

## Decisiones de diseño y por qué se tomaron

### 1. "Estudiante" se resuelve renombrando "Alumno", no tocando las consultas

El bug 5.1 se podía arreglar de dos formas: cambiar `"estudiante"` por
`"Alumno"` en `AsistenciasController`/`InscripcionesController`, o sembrar el
rol como `"Estudiante"` y dejar esas consultas intactas. Se eligió la
segunda porque el pedido de Magali ya pide que el rol final se llame
"Estudiante" — así una sola fuente de verdad (el seed) resuelve el nombrado
correcto y el bug 5.1 al mismo tiempo, en vez de mantener dos nombres
distintos para la misma persona (`Alumno` en el seed, `Estudiante` en medio
del código).

### 2. El seed actualiza por `RoId`, no busca por nombre nuevo

La primera versión simplemente cambió los literales (`"Profesor"` →
`"Docente"`, etc.) manteniendo la lógica "insertar si no existe ese
nombre":

```csharp
if (!await context.Roles.AnyAsync(r => r.RoDenominacion == "Docente"))
    context.Roles.Add(new Rol { RoId = 2, RoDenominacion = "Docente" });
```

En cualquier base que ya hubiera corrido el seed viejo (todas las de
desarrollo + la del servidor), esa condición da `true` — no existe una fila
`"Docente"` — e intenta insertar una fila nueva con `RoId = 2`, que ya está
ocupado por la fila `"Profesor"` existente. Resultado: `SaveChangesAsync`
tira una violación de clave primaria y la app no arranca.

Se corrigió buscando la fila por su `RoId` (que no cambia) y actualizando su
`RoDenominacion` si hace falta:

```csharp
var rolDocente = await context.Roles.FindAsync(2);
if (rolDocente == null)
    context.Roles.Add(new Rol { RoId = 2, RoDenominacion = "Docente" });
else if (rolDocente.RoDenominacion != "Docente")
    rolDocente.RoDenominacion = "Docente";
```

Mismo patrón para `Estudiante` (RoId 3). `Dirección` (RoId 4) es alta nueva,
así que ahí sí alcanza con "insertar si no existe".

### 3. Se mantuvieron los mismos `RoId` que tenían Profesor/Alumno

No hay ningún requisito de negocio sobre qué número le corresponde a cada
rol. Se optó por conservar `Docente = 2` y `Estudiante = 3` (los que ya
tenían Profesor/Alumno) para minimizar el impacto: el código que compara por
número (`AdminController`, el JS condicional de `UsuarioAgregar`/
`UsuarioEditar`) sigue funcionando sin tocarlo.

### 4. "Dirección" se escribe con tilde en el código

`RoDenominacion` cumple doble función: es el valor que compara
`[Authorize(Roles = "...")]` y el `switch` de login, y es lo que se muestra
en los `<select>` de rol. Escribirlo con tilde hace que el desplegable se
vea correctamente en español, a costa de tener que escribir la tilde en cada
literal de comparación (`Authorize`, el `switch` de `AccountController`,
`_Layout`). Se decidió priorizar la UX del desplegable; todos los literales
del código quedaron consistentes con la tilde.

### 5. `MateriasController` se cerró entero a Admin/Dirección, sin excepción para Docente

Se evaluó si el Docente necesitaba acceso de lectura al catálogo general de
materias (`MateriasController.Index`/`Details`) para "ver sus materias
asignadas y entrar a tomar asistencia". Se confirmó que esa necesidad ya
está resuelta por otra pantalla: `ProfesorController.Index()` ya lista,
filtradas por el docente logueado, solo las cátedras que tiene asignadas, y
desde ahí entra a `Asistencia(caMaId, fecha)`. `MateriasController` es el
catálogo completo sin filtrar (gestión), no la vista personal del docente —
por lo tanto no necesita abrirse a ese rol.

## Archivos

- `ISFDyT124/Program.cs` — seed de roles
- `ISFDyT124/Controllers/ProfesorController.cs` — `Authorize` a "Docente";
  filtro de alumnos por rol "Estudiante"
- `ISFDyT124/Controllers/AccountController.cs` — redirección post-login
  agrega el caso Dirección
- `ISFDyT124/Controllers/AsistenciasController.cs` — bug 5.1 resuelto sin
  tocar las consultas (ver decisión 1)
- `ISFDyT124/Controllers/AdminController.cs`,
  `ISFDyT124/Controllers/CarrerasController.cs`,
  `ISFDyT124/Controllers/MateriasController.cs` — `Authorize` a
  "Admin,Dirección" (`MateriasController` no tenía ninguno antes)
- `ISFDyT124/Views/Shared/_Layout.cshtml` — el link a Panel Admin contempla
  también Dirección

## Pendiente

- Probar en caliente (app + SQL Server levantados) el login con cada uno de
  los 4 roles y confirmar accesos y redirecciones.
- Verificar que el seed corre sin conflictos contra la base del servidor,
  no solo en local.
- El día que exista un CRUD de `Rol`, restringirlo a `Admin` excluyendo a
  `Dirección` (ver "Permisos de Dirección" arriba).

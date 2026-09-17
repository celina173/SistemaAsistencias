# Diagnóstico: qué le falta al sistema para quedar 100% funcional

> Relevamiento hecho revisando el código real (Models, DTOs, Controllers, `InstitutoDbContext`)
> en `grupo2-010726-Gustavo`, no solo los tickets del tablero. Objetivo: separar "bug puntual ya
> trackeado" de "falta algo estructural que ningún ticket individual cubre". Ordenado por
> impacto, no por dificultad.

---

## 0. Resumen ejecutivo — los 5 problemas que más importan

1. **`Asistencia` tiene dos caminos que no se hablan entre sí (`MaId` vs `CaMaId`)** — la carga de
   asistencia del docente (`ProfesorController`, el camino que más se usa) guarda `MaId` y deja
   `CaMaId` en `null`; la del admin (`AsistenciasController`) guarda `CaMaId` y deja `MaId` en
   `null`. Los reportes que filtran por `CaMaId` (Asistencia Global, Auditoría de Docentes) tienen
   que adivinar con un parche (`CaMaId == X || CaMaId == null`) que además **mezcla asistencia de
   otra materia** si el alumno está anotado a más de una cátedra. Es el bug más grave del sistema
   porque corrompe silenciosamente los números que ve la Dirección.
2. **No existe ninguna forma de dar de alta un `Cohorte` (año) ni un `CarreraCohorte` (carrera +
   año) desde la aplicación.** Todos los que hay hoy están cargados a mano en la base. El día que
   arranque un ciclo lectivo nuevo, no hay botón para crearlo — hace falta entrar directo a SQL.
   Para un sistema que se va a usar año tras año, esto no es un detalle menor: es no poder operar
   sin un desarrollador al lado.
3. **Borrar una Carrera o una Materia no avisa nada y arrastra todo en cascada.** Borrar una
   Carrera borra sus `CarreraCohorte` y `CarreraMaterias` en cascada, lo que a su vez desinscribe
   alumnos y borra `Inscripciones`; borrar una Materia con asistencia cargada borra esa asistencia
   directo. No hay ningún cartel de "esta carrera tiene 40 alumnos y 3 materias, ¿confirmás?".
4. **Faltan restricciones de unicidad a nivel de base** en las relaciones muchos-a-muchos: nada
   impide crear la misma `CarreraMateria` (carrera+materia) dos veces, la misma `Inscripciones`
   (alumno+cátedra) dos veces fuera del único formulario que sí valida a mano
   (`AgregarInscripcionMateria`), ni el mismo `CarreraCohorte` dos veces.
5. **Hay una tabla entera (`UsuarioRol`) y varios DTOs que no hace nada nadie usa** — quedaron de
   un diseño anterior (roles muchos-a-muchos) que el sistema real no usa (`Usuario.RoId` es la
   fuente de verdad, un rol por usuario). Sostenerla como si funcionara es deuda técnica que
   confunde a cualquiera que lea el modelo por primera vez.

---

## 1. Relaciones del modelo de datos

### 1.1 Mapa real (no el ideal, el que hay hoy)

```
Rol 1───* Usuario *───1 CarreraCohorte *───1 Carrera
                                              │
                                              │ 1
                                              *
                                        CarreraMateria *───1 Materia
                                          │        │
                                          │ *       │ *
                                          │         │
                              Inscripciones      UsuarioCarreraMateria
                              (alumno↔cátedra)    (docente↔cátedra)
                                          │
                                          │ (no hay FK real a Asistencia)
                                          ▼
                                     Asistencia
                                (UsId + MaId  — camino ProfesorController)
                                (UsId + CaMaId — camino AsistenciasController)
```

Puntos clave que no son obvios mirando solo los modelos:

- **`Inscripciones`** (alumno inscripto a una cátedra) y **`UsuarioCarreraMateria`** (docente
  asignado a una cátedra) **no son la misma tabla ni son redundantes entre sí** — son dos
  relaciones con significado distinto que comparten forma. `UsuarioCarreraMateria` es un
  many-to-many implícito de EF (`Usuario.CarreraMaterias`), sin DTO ni entidad propia.
- **`UsuarioRol`** es una tercera relación Usuario↔Rol, muchos-a-muchos, que **no se usa para
  nada real**: el rol efectivo de un usuario es siempre `Usuario.RoId` (uno solo). `UsuarioRol`
  solo se toca en `AdminController.UsuarioEliminar` para no dejar filas huérfanas al borrar un
  usuario — nunca se crea una fila ahí en el alta. Es researcher muerto que aparenta ser una
  feature (roles múltiples) que no existe.
- **`Asistencia.UsId`, `MaId` y `CaMaId` son todos nullable** y la entidad tiene comentarios
  ("Por el momento no se utiliza") que ya no son ciertos — `UsId` se usa activamente en ambos
  controladores. Documentación desactualizada que induce a error a cualquiera que la lea.

### 1.2 Faltan restricciones de unicidad (nada las impide hoy)

| Relación | Se puede duplicar hoy | Dónde se debería frenar |
|---|---|---|
| `CarreraMateria` (CaId + MaId) | Sí, sin aviso | Índice único en `InstitutoDbContext` + chequeo en `MateriasController`/alta de cátedra |
| `Inscripciones` (UsId + CaMaId) | Sí, salvo por el único formulario manual que lo valida (`AgregarInscripcionMateria`) — `AlumnosController` y la carga masiva no chequean | Índice único a nivel de base (defensa real, no depende de que cada controller se acuerde) |
| `CarreraCohorte` (CaId + CoId) | Sí | Índice único |
| `Usuario.UsDni` | No — esto sí está bien, ya tiene `HasIndex().IsUnique()` | ✅ ya resuelto |

### 1.3 Comportamiento de borrado sin red de seguridad

- Borrar `Carrera` → cascada a `CarreraCohorte` y `CarreraMateria` → cascada a `Inscripciones`.
  Ningún control intermedio. Un alumno con años de asistencia cargada puede perder el vínculo a
  su carrera con un solo click sin confirmación real del alcance.
- Borrar `Materia` → cascada directa a `Asistencia` (vía `MaId`) y a `CarreraMateria` (que a su
  vez cascadea a `Inscripciones`). Es decir, **borrar una materia borra asistencia histórica real**,
  sin pedir ninguna confirmación distinta a la genérica "¿Está seguro?" que no menciona qué se
  pierde.
- `Usuario.CaCoId` no tiene `OnDelete` explícito en `OnModelCreating` → EF aplica el default para
  FK opcional (`ClientSetNull`). Al borrar un `CarreraCohorte`, todos los alumnos de esa
  cohorte quedan con `CaCoId = null` en silencio, sin registro de que antes pertenecían a esa
  carrera/año.

---

## 2. Flujos de gestión que no existen (no son bugs, son ausencias)

| Entidad | Alta | Edición | Baja | Impacto de que falte |
|---|---|---|---|---|
| `Cohorte` (año) | ❌ No existe | ❌ | ❌ | No se puede arrancar un ciclo lectivo nuevo sin tocar la base a mano |
| `CarreraCohorte` (carrera+año) | ❌ No existe | ❌ | ❌ | Mismo problema — ni siquiera se puede vincular una carrera a un año nuevo |
| `Rol` | ❌ Solo el seed fijo (4 roles hardcodeados) | ❌ | ❌ | Aceptable para el alcance actual (los roles del instituto no cambian seguido), pero si el día de mañana piden un rol nuevo, hoy no hay forma sin migración |
| `UsuarioCarreraMateria` (asignar cátedra a un docente) | ✅ Existe (alta/edición de usuario) | ✅ | ✅ (se limpia al cambiar de rol) | — |
| `Inscripciones` (anotar alumno a cátedra) | ✅ Existe (3 caminos: alta/edición de alumno, carga masiva, formulario manual) | Parcial | Parcial | Ver 1.2 — los 3 caminos no validan duplicados igual |

---

## 3. Validaciones — estado por entidad

### 3.1 Ya están bien (no tocar)
- `Usuario`/`AlumnoFormDto`/`UsuarioCrearDto`: apellido/nombre (regex letras), DNI (rango
  6.000.000–99.999.999 + regex), email (`[EmailAddress]` + `MaxLength(254)` ya agregado esta
  sesión), DNI único a nivel de base.
- `Carrera`/`Materia`: denominación sin números (regex), longitud máxima, modalidad como lista
  fija, cantidad de módulos en rango 1-4, porcentaje de asistencia mínima en rango 0-100.

### 3.2 Con hueco real

| DTO/Modelo | Falta | Por qué importa |
|---|---|---|
| `CarreraMateriaCrearDto`, `CohorteCrearDto`, `CarreraCohorteCrearDto`, `RolCrearDto`, `UsuarioRolCrearDto` | No los usa ningún controller — **código muerto** | Confunde: alguien puede pensar que esos flujos de alta existen y perder tiempo buscándolos |
| `Inscripciones` (modelo) | Sin `[Required]`/`[ForeignKey]` explícitos, sin validación de "el alumno tiene que tener rol Estudiante" ni "la cátedra tiene que existir" a nivel de modelo | Se apoya 100% en que cada controller individual valide bien — ya vimos que no todos lo hacen igual |
| `CarreraMateria` (modelo) | Sin unicidad (CaId+MaId) | Ver 1.2 |
| `Asistencia` (modelo) | `AsFecha` es `DateTime?` pero `[Required]` — inconsistente (si es obligatorio no debería ser nullable); no valida que la fecha no sea futura a nivel de modelo (sí lo hace `ProfesorController` en código, pero `AsistenciasController` no) | Un Admin puede cargar asistencia con fecha futura sin que nada lo frene |

---

## 4. Seguridad y permisos — lo que ya se sabía, para que quede en un solo lugar

Esto ya está documentado por Pablo en `pendientes-arquitectura.md`, lo repito acá solo para que
el diagnóstico esté completo en un solo documento:

- `AccountController.CambiarContrasena` no redirige por rol (a diferencia de `Login`, que sí) —
  todo usuario nuevo cae en Home la primera vez en lugar de su panel real.
- `Dirección` usa el panel de `Admin` "por ahora" (comentario textual en el código) — no hay
  definición de si eso queda así para siempre o necesita su propia vista.
- Números mágicos de rol (`2`=Docente, `3`=Estudiante) repetidos en JS y controllers en vez de un
  enum/constante único — frágil ante cualquier renumeración futura.

---

## 5. Priorización sugerida

| # | Ítem | Tipo | Por qué en ese orden |
|---|---|---|---|
| 1 | Unificar `Asistencia` para que ambos controllers escriban siempre `CaMaId` (y dejar de necesitar el parche de `CaMaId == null` en Asistencia Global) | Bug de integridad de datos | Afecta números que ya se están mostrando hoy — cuanta más asistencia real se cargue, más data quedará mal etiquetada y más caro será migrarla después |
| 2 | Alta de `Cohorte` y `CarreraCohorte` | Feature faltante | Sin esto el sistema no sobrevive al cambio de ciclo lectivo — es un bloqueante de uso real, no una mejora |
| 3 | Confirmación real (no genérica) al borrar Carrera/Materia, mostrando cuántos alumnos/cátedras/asistencias se van a ver afectados | Prevención de pérdida de datos | Barato de hacer, evita un desastre irreversible |
| 4 | Índices únicos en `CarreraMateria`, `Inscripciones`, `CarreraCohorte` | Integridad de datos | Requiere limpiar duplicados existentes antes de poder crear el índice — mejor hacerlo temprano, antes de que haya más datos reales cargados |
| 5 | Sacar `UsuarioRol` y los DTOs muertos, o documentar por qué se mantienen | Deuda técnica / claridad | No rompe nada dejarlo, pero cuanto antes se saque, menos gente pierde tiempo entendiendo una relación que no hace nada |
| 6 | `CambiarContrasena` redirige por rol, enum para RoId | Bug menor + deuda técnica | Molesto pero no bloqueante |

---

## 6. Lo que NO entra en este diagnóstico (a propósito)

- Los 5 tickets pendientes de traer a esta rama (3.13/3.14/3.15/3.16/5.11) — ya están
  identificados y son trabajo puntual ya resuelto en `Development`, no arquitectura nueva.
- Decisiones de producto puras (ej. si Dirección necesita panel propio, si hace falta CRUD de
  Rol) — quedan para que el equipo las charle, acá solo se señala que la ausencia existe.

# READMEGUSTAVO — Módulo Profesor y trabajo sobre la rama

> Rama de trabajo: **`grupo2-010726-Gustavo`** (basada en `origin/master`).
> Documento que resume qué se hizo y por qué.

---

## 1. Contexto de ramas

- Se creó la rama **`grupo2-010726-Gustavo`** a partir del **master remoto** (`origin/master`), que es la única rama tomada como fuente de verdad.
- El `master` local estaba desactualizado (33 commits atrás); se sincronizó con `origin/master` por fast-forward.
- Luego se trajeron a esta rama los **últimos cambios de master** (7 commits de "correcciones de sintaxis").

**Cadena de conexión:** se usa siempre la de Grupo-2 (servidor **MUCHI**) en `appsettings.json`,
dejando las de master (`Santiago` / `localhost`) **comentadas** (no se borran). El archivo
`appsettings.json` está en `.gitignore` (config local).

```
Server=MUCHI;Database=Instituto;Trusted_Connection=True;TrustServerCertificate=True;
```

---

## 2. Objetivo: unificar/ordenar el módulo Profesor

El módulo docente venía **desalineado**:

| Elemento | Antes | Ahora |
|----------|-------|-------|
| Archivo | `ProfesorController.cs` | `ProfesorController.cs` |
| Clase | `DocenteController` ❌ | **`ProfesorController`** ✅ |
| Rol exigido | `"Docente"` (no existe) ❌ | **`"Profesor"`** (rol real sembrado) ✅ |
| Redirección de login | `case "DOCENTE"` (nunca matcheaba) ❌ | **`case "PROFESOR"`** ✅ |
| Vistas | no existían ❌ | **`Views/Profesor/` creadas** ✅ |
| Datos hacia la vista | entidades crudas + ViewBag ❌ | **DTOs** ✅ |

Se eligió la **Opción B**: un `ProfesorController` dedicado (convención MVC), en vez de mezclarlo
en un `UsuarioController`.

---

## 3. Cambios realizados

### 3.1. `ProfesorController` (refactor a DTOs)
Archivo: `ISFDyT124/Controllers/ProfesorController.cs`

- Clase renombrada a `ProfesorController` con `[Authorize(Roles = "Profesor")]`.
- Tres secciones, todas usando los DTOs de la carpeta `/DTO`:

| Sección | Acción | DTO usado |
|---------|--------|-----------|
| 1. Dashboard de cátedras | `Index()` | `CarreraMateriaDetalleDto` |
| 2. Tomar/editar asistencia | `Asistencia()` GET / POST | `UsuarioDetalleDto` / `AsistenciaCrearDto` |
| 3. Historial | `HistorialAsistencias(int maId)` | `AsistenciaDetalleDto` |

- **Alcance del profesor:** solo toma asistencia de **sus** materias y consulta el historial.
- La toma de asistencia hace *upsert* en lote (inserta o actualiza el registro del día) con PK manual.

### 3.2. Vistas nuevas (`Views/Profesor/`)
Replican **el mismo estilo/diseño** de las maquetas de `Views/Home/`, pero integradas al
`_Layout.cshtml` y dinámicas (con `@model` DTO):

| Vista | Réplica de | Clases CSS reutilizadas |
|-------|-----------|--------------------------|
| `Index.cshtml` | `Admin/UsuariosABM.cshtml` | `main-container-gestion`, `crud-table` |
| `Asistencia.cshtml` | `Home/Asistencia.cshtml` | `attendance-table`, `custom-checkbox`, `btn-guardar` |
| `HistorialAsistencias.cshtml` | `Home/AsistenciaGlobal.cshtml` | `global-table`, `title-cell` |

No se agregó CSS nuevo: se reutilizaron las clases existentes.

### 3.3. Login
Archivo: `ISFDyT124/Controllers/AccountController.cs`
- El `switch` de redirección post-login ahora usa `case "PROFESOR"` → `ProfesorController`.

### 3.4. Documentación para frontend
Archivo: `REVISION-FRONTEND-vistas-profesor.txt`
- Pide al equipo de frontend revisar las 3 vistas nuevas y define puntos abiertos
  (principalmente: la maqueta mostraba 4 "módulos" por alumno, pero el modelo de datos
  guarda un solo Presente/día).

---

## 4. Estado del build ⚠️

- **El código nuevo del módulo Profesor compila limpio** (0 errores en el controller y las 3 vistas).
- El proyecto **todavía NO compila** por **17 errores heredados de master** (ajenos a este trabajo):
  - Rename incompleto `UsDni` → `UsDNI` (Model/DTOs vs resto del código).
  - DbSets nombrados en singular en `AdminController` (`_context.CarreraCohorte` / `CarreraMateria`)
    que en el `InstitutoDbContext` están en plural.
- Estos 17 errores **se dejaron sin tocar** a propósito (los resuelve el equipo al sincronizar master).

---

## 5. Pendientes

- [ ] Destrabar los 17 errores de master para poder probar `/Profesor` en vivo.
- [ ] Definir con frontend "módulos vs presente" y el formato del historial.
- [ ] Agregar el acceso "Profesor" en el menú lateral de `_Layout.cshtml`.
- [ ] Sembrar datos de prueba (docente + cátedras + inscripciones) para ver el flujo con datos.

# Documentación Funcional: Dashboard del Profesor / Docente
**Proyecto**: Sistema de Asistencias - ISFDyT 124

Esta documentación describe la propuesta funcional y arquitectónica para el **Dashboard del Profesor/Docente** (Rol ID: 2), basada en la estructura actual de la base de datos y los modelos de Entity Framework Core registrados en [InstitutoDbContext](file:///c:/Users/iamgu/Desktop/Practicas%203/Sistema%20de%20Asistencias/SistemaAsistencias/ISFDyT124/Data/InstitutoDbContext.cs).

---

## 1. Arquitectura y Relaciones de Datos

El flujo de trabajo del profesor se apoya en las siguientes entidades del sistema:

*   **Usuario (`Usuario`)**: Representa al docente (con `RoId = 2`) y a los alumnos (con `RoId = 3`).
*   **Carrera y Materia (`Carrera`, `Materia`)**: Definen la estructura académica. Cada materia posee la propiedad `MaCantModulos` (de 1 a 4 módulos), que determina la duración de la clase diaria.
*   **CarreraMateria (`CarreraMateria`)**: Tabla intermedia que asocia una materia a una carrera específica.
*   **Asociación de Cátedras (`UsuarioCarreraMateria` / `Usuario.CarreraMaterias`)**: Relación de muchos a muchos que mapea qué materias y carreras tiene asignadas cada docente.
*   **Cohorte (`Cohorte` y `CarreraCohorte`)**: Define el año de cursada. Permite agrupar a los alumnos (`Usuario.CaCoId`) en una carrera y año específicos.
*   **Asistencia (`Asistencia`)**: Registra la asistencia diaria por alumno (`UsId`) y materia (`MaId`), almacenando la fecha (`AsFecha`), si estuvo presente (`AsPresente`) y si la falta fue justificada (`AsJustificacion`).

---

## 2. Flujo de Trabajo en el Dashboard

El dashboard del profesor se divide en tres secciones principales:

### SECCIÓN 1: Selección de Cátedra y Clase (`Home/Index`)
Al ingresar al sistema, el docente visualiza un panel de selección para iniciar la jornada:
1.  **Filtro por Cátedra**: Los selectores de **Carrera** y **Materia** cargan dinámicamente únicamente las opciones asociadas al docente autenticado a través de su relación `CarreraMaterias` en la base de datos (evitando que visualice materias de otros profesores).
2.  **Selección de Cohorte**: El docente selecciona el año/cohorte correspondiente para delimitar el grupo de alumnos.
3.  **Selección de Fecha**: Por defecto se establece la fecha actual, impidiendo la selección de fechas futuras.

---

### SECCIÓN 2: Planilla de Toma de Asistencia Diaria (`Home/Asistencia`)
Una vez confirmados los filtros, se renderiza la planilla con la lista de alumnos:
1.  **Carga Dinámica de Alumnos**: Se listan los alumnos correspondientes a la carrera y cohorte seleccionadas (`u.CaCoId == CaCoId` y `u.RoId == 3`), ordenados alfabéticamente por apellido y nombre.
2.  **Módulos Dinámicos**: La interfaz muestra tantas columnas de verificación (checkboxes) como módulos tenga configurados la materia (`MaCantModulos`). 
    *   *Ejemplo*: Si "Prácticas Profesionalizantes III" tiene 4 módulos, se muestran 4 checkboxes para marcar la presencia en cada bloque horario del día.
3.  **Carga Masiva (Bulk Save) con Lógica de Negocio**:
    *   Al hacer clic en "Guardar", se calcula el estado general: si el alumno asistió a los módulos requeridos, se guarda `AsPresente = true`.
    *   **Regla de Justificación**: Si el alumno está presente (`AsPresente = true`), la justificación se fuerza a `false`. Si está ausente (`AsPresente = false`), el docente o preceptor puede tildar el checkbox para marcar `AsJustificacion = true`.
4.  **Mecanismo de Upsert en BD**:
    *   Al cargar la página, el sistema verifica si ya existe asistencia registrada para ese día, materia y cohorte. De ser así, precarga los checkboxes correspondientes.
    *   Al guardar, si el registro ya existe, actualiza los campos (`Update`); si no, realiza una inserción (`Insert`), calculando el próximo ID de asistencia (`AsId`) de forma manual consecuente.

---

### SECCIÓN 3: Planilla de Asistencia Global e Historial (`Home/AsistenciaGlobal`)
Un panel de consulta que permite ver el desempeño de asistencia a lo largo del tiempo:
1.  **Grilla Bidimensional**: Muestra los alumnos en las filas y las fechas en las que se dictó la materia en las columnas.
2.  **Porcentaje de Regularidad**: Calcula en tiempo real la regularidad acumulada del alumno:
    $$\text{Asistencia \%} = \left( \frac{\text{Clases Presente}}{\text{Total de Clases Dictadas}} \right) \times 100$$
3.  **Alertas de Pérdida de Regularidad**: Si el porcentaje de asistencia acumulado es inferior al **75%** (o al límite institucional), la fila del alumno se tiñe de color **rojo/naranja**, permitiendo al profesor identificar rápidamente a los alumnos en riesgo de quedar libres.

---

## 3. Propuesta de Diseño del Controlador

Para implementar estas funciones de forma limpia y escalable en ASP.NET Core MVC, evaluamos dos alternativas de diseño:

### Alternativas de Implementación

| Criterio | Opción A: Extender `HomeController.cs` | Opción B: Crear un `ProfesorController.cs` Dedicado |
| :--- | :--- | :--- |
| **Responsabilidad Única (SRP)** | **Mala**. Mezcla la lógica genérica de inicio de la aplicación con la lógica específica de un rol crítico (docente). | **Excelente**. Concentra únicamente las operaciones, consultas y reglas de negocio pertenecientes al rol docente. |
| **Seguridad y Autorización** | **Compleja**. Requiere colocar etiquetas de autorización individuales (`[Authorize(Roles = "Profesor")]`) por acción, con alto riesgo de omitir alguna. | **Simple**. Se aplica `[Authorize(Roles = "Profesor")]` a nivel de clase, protegiendo todas sus rutas por defecto. |
| **Mantenibilidad** | **Baja**. Conduce a un controlador "monolito" difícil de testear y modificar a medida que crecen las reglas de negocio de asistencia. | **Alta**. Código desacoplado, modular y fácil de expandir (por ejemplo, si en el futuro se añade carga de calificaciones). |

### Decisión de Diseño
Se elige la **Opción B (`ProfesorController.cs`)** por ser la que mejor respeta las buenas prácticas de arquitectura de software, garantizando la seguridad por rol de forma nativa a nivel de clase y manteniendo el código ordenado y testeable.

---

## 4. Estructura Propuesta para `ProfesorController.cs`

A continuación se detalla el esqueleto en C# con la lógica recomendada para implementar las acciones del docente:

```csharp
using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    [Authorize(Roles = "Profesor")] // Seguridad garantizada a nivel de controlador
    public class ProfesorController : Controller
    {
        private readonly InstitutoDbContext _context;

        public ProfesorController(InstitutoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Dashboard del Profesor: Carga únicamente sus materias y carreras asignadas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Obtener el ID del Docente logueado
            var docenteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Cargar materias asociadas al docente desde la tabla intermedia UsuarioCarreraMateria
            var misCarrerasMaterias = await _context.Usuarios
                .Where(u => u.UsId == docenteId)
                .SelectMany(u => u.CarreraMaterias)
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .ToListAsync();

            // Mapear a DTO para pasar a la vista de selección
            var model = new HomeIndexDto
            {
                Carreras = misCarrerasMaterias
                    .Select(cm => cm.Carrera)
                    .Distinct()
                    .Select(c => new CarreraDetalleDto { CaId = c.CaId, CaDenominacion = c.CaDenominacion })
                    .ToList(),
                Materias = misCarrerasMaterias
                    .Select(cm => cm.Materia)
                    .Distinct()
                    .Select(m => new MateriaDetalleDto { MaId = m.MaId, MaDenominacion = m.MaDenominacion })
                    .ToList()
            };

            return View(model);
        }

        /// <summary>
        /// Carga la planilla de asistencia de los alumnos para el día seleccionado
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Asistencia(int caId, int coId, int maId, DateTime? fecha)
        {
            var fechaFiltro = fecha ?? DateTime.Today;

            if (fechaFiltro > DateTime.Today)
            {
                ModelState.AddModelError("", "No se puede registrar asistencia de fechas futuras.");
                return View("Error");
            }

            // Obtener la cantidad de módulos de la materia para renderizar las columnas en la vista
            var materia = await _context.Materias.FindAsync(maId);
            ViewBag.CantModulos = materia?.MaCantModulos ?? 1;

            // Cargar alumnos inscriptos en la Carrera/Cohorte
            var alumnos = await _context.Usuarios
                .Where(u => u.RoId == 3 && u.CaCoId == coId)
                .OrderBy(u => u.UsApellido)
                .ToListAsync();

            // Cargar asistencias ya tomadas en ese día para este curso/materia
            var asistenciasExistentes = await _context.Asistencias
                .Where(a => a.MaId == maId && a.AsFecha.Value.Date == fechaFiltro.Date)
                .ToDictionaryAsync(a => a.UsId.Value, a => a);

            ViewBag.AsistenciasExistentes = asistenciasExistentes;
            ViewBag.Fecha = fechaFiltro;

            return View(alumnos);
        }

        /// <summary>
        /// Guarda transaccionalmente la asistencia del día aplicando las reglas de negocio
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarAsistencia(int maId, DateTime fecha, Dictionary<int, AsistenciaInputDto> asistencias)
        {
            if (asistencias == null || !asistencias.Any())
            {
                return BadRequest("No se recibieron datos de asistencia.");
            }

            int proximoAsId = _context.Asistencias.Any() ? await _context.Asistencias.MaxAsync(a => a.AsId) + 1 : 1;

            foreach (var record in asistencias)
            {
                int alumnoId = record.Key;
                bool estaPresente = record.Value.AsPresente;
                bool justificado = record.Value.AsJustificacion;

                // Aplicar regla de negocio: Si está presente, no puede estar justificado
                if (estaPresente) justificado = false;

                var asistenciaExistente = await _context.Asistencias
                    .FirstOrDefaultAsync(a => a.UsId == alumnoId && a.MaId == maId && a.AsFecha.Value.Date == fecha.Date);

                if (asistenciaExistente != null)
                {
                    // Actualización
                    asistenciaExistente.AsPresente = estaPresente;
                    asistenciaExistente.AsJustificacion = justificado;
                    _context.Update(asistenciaExistente);
                }
                else
                {
                    // Creación
                    var nuevaAsistencia = new Asistencia
                    {
                        AsId = proximoAsId++,
                        AsFecha = fecha.Date,
                        AsPresente = estaPresente,
                        AsJustificacion = justificado,
                        UsId = alumnoId,
                        MaId = maId
                    };
                    _context.Asistencias.Add(nuevaAsistencia);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
```

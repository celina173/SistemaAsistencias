using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ISFDyT124.Models;

namespace ISFDyT124.Controllers
{
    /// Controlador responsable del flujo de trabajo de los Docentes.
    /// Contiene la lógica para seleccionar asignaturas, tomar asistencia diaria y consultar historial.
    [Authorize(Roles = "Docente")] // Protegemos todo el controlador
    public class DocenteController : Controller
    {
        private readonly InstitutoDbContext _context;

        // Constantes del sistema para mapear con los IDs de la tabla ROLES
        private const int ROL_ALUMNO_ID = 3;  // ID del rol Alumno en la base de datos

        public DocenteController(InstitutoDbContext context)
        {
            _context = context;
        }

        #region SECTION 1: DASHBOARD DEL DOCENTE (SELECCIÓN DE CLASE)

        /// <summary>
        /// Vista de inicio del docente.
        /// Carga los datos de las materias asignadas al docente logueado para que seleccione su curso actual.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // 1. Obtenemos el ID del docente logueado desde los Claims
            var docenteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(docenteIdClaim, out int docenteId)) return Unauthorized();

            // 2. Buscamos las materias/carreras asignadas específicamente a este docente
            var materiasDelDocente = await _context.Inscripciones
                .Include(i => i.CarrerasMaterias)
                    .ThenInclude(cm => cm.Materia)
                .Include(i => i.CarrerasMaterias)
                    .ThenInclude(cm => cm.Carrera)
                .Where(i => i.UsId == docenteId)
                .Select(i => i.CarrerasMaterias) // Nos quedamos solo con la relación Carrera-Materia
                .ToListAsync();

            // Mantenemos la estructura de ViewBags original, pero ahora filtrada por el docente
            ViewBag.Carreras = materiasDelDocente.Select(cm => cm.Carrera).Distinct().ToList();
            ViewBag.Materias = materiasDelDocente.Select(cm => cm.Materia).Distinct().ToList();
            //ViewBag.Cohortes = await _context.Cohortes.ToListAsync();

            // Pasamos la relación completa a la vista
            return View(materiasDelDocente);
        }

        #endregion

        #region SECTION 2: TOMA DE ASISTENCIA DIARIA (CARGA MASIVA Y EDICIÓN)

        /// <summary>
        /// Acción GET que renderiza la planilla de asistencia de los alumnos para una carrera, cohorte, materia y fecha determinadas.
        /// Respeta las relaciones y propiedades del esquema SQL.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CargarAsistencia(int caId, int coId, int maId, DateTime? fecha)
        {
            // Verificamos identidad nuevamente por seguridad
            var docenteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(docenteIdClaim, out int docenteId)) return Unauthorized();

            // Si no se especifica fecha, se asume el día de hoy
            var fechaSeleccionada = fecha ?? DateTime.Today;

            // Almacenamos los parámetros de búsqueda en el ViewBag para mantener el estado en la vista
            ViewBag.CarreraId = caId;
            ViewBag.CohorteId = coId;
            ViewBag.MateriaId = maId;
            ViewBag.Fecha = fechaSeleccionada;

            // Obtener información descriptiva para el encabezado de la vista
            ViewBag.CarreraNombre = (await _context.Carreras.FindAsync(caId))?.CaDenominacion ?? "Carrera";
            //ViewBag.CohorteNombre = (await _context.Cohortes.FindAsync(coId))?.CoDenominacion ?? "Cohorte";
            ViewBag.MateriaNombre = (await _context.Materias.FindAsync(maId))?.MaDenominacion ?? "Materia";

            // 1. OBTENCIÓN DE ALUMNOS (Lógica unificada):
            // Buscamos a los alumnos inscriptos en la misma combinación de Carrera-Materia
            var idCarreraMateria = await _context.CarrerasMaterias
                .Where(cm => cm.CaId == caId && cm.MaId == maId)
                .Select(cm => cm.CaMaId)
                .FirstOrDefaultAsync();

            var alumnos = await _context.Inscripciones
                .Include(i => i.Usuarios)
                    .ThenInclude(u => u.Rol)
                .Where(i => i.CaMaId == idCarreraMateria && i.Usuarios.Rol.RoDenominacion == "Alumno")
                .Select(i => i.Usuarios) // Extraemos los objetos Usuario (Alumnos)
                .OrderBy(u => u.UsApellido)
                .ThenBy(u => u.UsNombre)
                .ToListAsync();

            // 2. RECUPERAR REGISTROS DE ASISTENCIA EXISTENTES:
            // Si el docente ya tomó asistencia el día de hoy para esta materia, recuperamos esos registros
            // de la tabla ASISTENCIAS para que pueda editarlos o ver qué marcó antes.
            var asistenciasRegistradas = await _context.Asistencias
                .Where(a => a.MaId == maId && a.AsFecha.Value.Date == fechaSeleccionada.Date) 
                .ToDictionaryAsync(a => a.UsId ?? 0);

            ViewBag.AsistenciasExistentes = asistenciasRegistradas;

            return View(alumnos);
        }

        /// <summary>
        /// Acción POST que procesa y guarda la asistencia masiva de los alumnos.
        /// Realiza un insert en lote si no existe el registro de asistencia del día, o un update en lote si ya existía.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargarAsistencia(int caId, int coId, int maId, DateTime fecha, Dictionary<int, bool> asistenciasDic)
        {
            if (asistenciasDic == null || asistenciasDic.Count == 0)
            {
                TempData["ErrorMessage"] = "No se recibieron registros de asistencia para procesar.";
                return RedirectToAction(nameof(Index));
            }

            // Variable para llevar el control del cálculo de la clave primaria manual de la tabla ASISTENCIAS (AsId)
            int proximoAsId = _context.Asistencias.Any() ? await _context.Asistencias.MaxAsync(a => a.AsId) + 1 : 1;

            // Recorremos cada registro de asistencia del diccionario (Key: UsId, Value: Presente/Ausente)
            foreach (var kvp in asistenciasDic)
            {
                int alumnoId = kvp.Key;
                bool estaPresente = kvp.Value;

                // 1. Buscamos si el registro de asistencia para este alumno en este día y materia ya existe
                var asistenciaExistente = await _context.Asistencias
                    .FirstOrDefaultAsync(a => a.UsId == alumnoId && a.MaId == maId && a.AsFecha.Value.Date == fecha.Date); // Manejo nullable

                if (asistenciaExistente != null)
                {
                    // ACTUALIZACIÓN (UPDATE): Si el registro ya existe, actualizamos el estado de presente
                    asistenciaExistente.AsPresente = estaPresente;
                    _context.Update(asistenciaExistente);
                }
                else
                {
                    // INSERCIÓN (INSERT): Si es una asistencia nueva, creamos un registro
                    var nuevaAsistencia = new Asistencia
                    {
                        AsId = proximoAsId++, // Asignamos el ID manual y lo incrementamos para el siguiente alumno
                        AsFecha = fecha.Date,
                        AsPresente = estaPresente,
                        AsJustificacion = false, // Por defecto al tomar asistencia no está justificada la falta
                        UsId = alumnoId,
                        MaId = maId
                    };
                    _context.Asistencias.Add(nuevaAsistencia);
                }
            }

            // Guardamos todos los cambios transaccionalmente en la base de datos
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Las asistencias del día han sido registradas correctamente.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region SECTION 3: CONSULTA DE HISTORIAL DE ASISTENCIAS

        /// <summary>
        /// Permite consultar el historial completo de planillas de asistencia registradas para una Materia.
        /// </summary>
        public async Task<IActionResult> HistorialAsistencias(int maId)
        {
            ViewBag.MateriaNombre = (await _context.Materias.FindAsync(maId))?.MaDenominacion ?? "Materia";
            ViewBag.MateriaId = maId;

            // Buscamos todas las asistencias vinculadas con esta materia, incluyendo al Alumno y sus Roles para justificaciones
            var asistencias = await _context.Asistencias
                .Where(a => a.MaId == maId)
                .Include(a => a.Usuario)
                .OrderByDescending(a => a.AsFecha)
                .ThenBy(a => a.Usuario.UsApellido)
                .ToListAsync();

            return View(asistencias);
        }

        #endregion
    }
}
using ISFDyT124.Data;
using ISFDyT124.Models;
using ISFDyT124.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    /// <summary>
    /// Controlador responsable del flujo de trabajo de los Docentes.
    /// Contiene la lógica para seleccionar asignaturas, tomar asistencia diaria y consultar históricos.
    /// </summary>
    public class ProfesorController : Controller
    {
        private readonly InstitutoDbContext _context;

        // Constantes del sistema para mapear con los IDs de la tabla ROLES
        private const int ROL_DOCENTE_ID = 2; // ID del rol Docente en la base de datos
        private const int ROL_ALUMNO_ID = 3;  // ID del rol Alumno en la base de datos

        // Constructor con inyección de dependencias para el acceso a datos con EF Core
        public ProfesorController(InstitutoDbContext context)
        {
            _context = context;
        }

        #region SECTION 1: DASHBOARD DEL DOCENTE (SELECCIÓN DE CLASE)

        /// <summary>
        /// Vista de inicio del docente.
        /// Carga listas simuladas vacías para evitar errores de compilación al no existir los modelos Carrera, Cohorte, Materia.
        /// </summary>
        public IActionResult Index()
        {
            // 1. Obtener la lista completa de Carreras (Simulado)
            ViewBag.Carreras = new List<object>();

            // 2. Obtener la lista completa de Cohortes (Simulado)
            ViewBag.Cohortes = new List<object>();

            // 3. Obtener la lista completa de Materias (Simulado)
            ViewBag.Materias = new List<object>();

            return View();
        }

        #endregion

        #region SECTION 2: TOMA DE ASISTENCIA DIARIA (CARGA MASIVA Y EDICIÓN)

        /// <summary>
        /// Acción GET que renderiza la planilla de asistencia de los alumnos.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CargarAsistencia(int caId, int coId, int maId, DateTime? fecha)
        {
            var fechaSeleccionada = fecha ?? DateTime.Today;

            // Almacenamos los parámetros de búsqueda en el ViewBag
            ViewBag.CarreraId = caId;
            ViewBag.CohorteId = coId;
            ViewBag.MateriaId = maId;
            ViewBag.Fecha = fechaSeleccionada;

            // Información descriptiva simulada
            ViewBag.CarreraNombre = "Carrera Simulada";
            ViewBag.CohorteNombre = "Cohorte Simulada";
            ViewBag.MateriaNombre = "Materia Simulada";

            // 1. OBTENCIÓN DE ALUMNOS usando DTOs y la relación directa con el rol
            var alumnos = await _context.Usuarios
                .Where(u => u.RoId == ROL_ALUMNO_ID)
                .Select(u => new UsuarioDetalleDto
                {
                    UsId = u.UsId,
                    UsApellido = u.UsApellido,
                    UsNombre = u.UsNombre,
                    UsEmail = u.UsEmail,
                    UsDni = u.UsDni,
                    RoId = u.RoId
                })
                .OrderBy(u => u.UsApellido)
                .ThenBy(u => u.UsNombre)
                .ToListAsync();

            // 2. RECUPERAR REGISTROS DE ASISTENCIA EXISTENTES (Simulado)
            ViewBag.AsistenciasExistentes = new Dictionary<int, bool>();

            return View(alumnos);
        }

        /// <summary>
        /// Acción POST que procesa y guarda la asistencia de los alumnos (Simulado por falta de tabla Asistencias).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CargarAsistencia(int caId, int coId, int maId, DateTime fecha, Dictionary<int, bool> asistenciasDic)
        {
            if (asistenciasDic == null || asistenciasDic.Count == 0)
            {
                TempData["ErrorMessage"] = "No se recibieron registros de asistencia para procesar.";
                return RedirectToAction(nameof(Index));
            }

            // Simulamos el guardado de forma exitosa
            TempData["SuccessMessage"] = "Las asistencias del día han sido registradas (Simulado, sin persistencia en BD).";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region SECTION 3: CONSULTA DE HISTORIAL DE ASISTENCIAS

        /// <summary>
        /// Permite consultar el historial completo de planillas de asistencia registradas para una Materia.
        /// </summary>
        public IActionResult HistorialAsistencias(int maId)
        {
            ViewBag.MateriaNombre = "Materia Simulada";
            ViewBag.MateriaId = maId;

            // Retornamos lista vacía de DTOs
            return View(new List<UsuarioDetalleDto>());
        }

        #endregion
    }
}

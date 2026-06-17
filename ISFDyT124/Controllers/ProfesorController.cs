using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    // Solo los usuarios con el rol "Profesor" pueden acceder a este controlador
    [Authorize(Roles = "Admin")]
    public class ProfesorController : Controller
    {
        private readonly InstitutoDbContext _context;

        // Inyección del contexto de base de datos
        public ProfesorController(InstitutoDbContext context)
        {
            _context = context;
        }

        #region SECCIÓN 1: DASHBOARD DE CÁTEDRAS (INICIO)

        // Muestra la vista principal del docente con las materias y carreras que tiene asignadas
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Obtener el ID del docente autenticado
            var docenteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Consultar las cátedras (Carrera-Materia) asociadas a este docente
            var misCatedras = await _context.Usuarios
                .Where(u => u.UsId == docenteId)
                .SelectMany(u => u.CarreraMaterias)
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .ToListAsync();

            // Mapear los datos al DTO para poblar los selectores en la vista
            var model = new HomeIndexDto
            {
                Carreras = misCatedras
                    .Select(cm => cm.Carrera)
                    .Where(c => c != null)
                    .Distinct()
                    .Select(c => new CarreraDetalleDto { CaId = c.CaId, CaDenominacion = c.CaDenominacion })
                    .ToList(),
                Materias = misCatedras
                    .Select(cm => cm.Materia)
                    .Where(m => m != null)
                    .Distinct()
                    .Select(m => new MateriaDetalleDto { MaId = m.MaId, MaDenominacion = m.MaDenominacion })
                    .ToList()
            };

            // Cargar los cohortes para el selector en la vista
            ViewBag.Cohortes = await _context.Cohortes
                .Select(co => new CohorteDetalleDto { CoId = co.CoId, CoAnio = co.CoAnio })
                .ToListAsync();

            return View(model);
        }

        #endregion

        #region SECCIÓN 2: TOMA Y EDICIÓN DE ASISTENCIA DIARIA

        // Renderiza la planilla diaria de alumnos para tomar asistencia
        [HttpGet]
        public async Task<IActionResult> Asistencia(int caId, int coId, int maId, DateTime? fecha)
        {
            var fechaFiltro = fecha ?? DateTime.Today;

            // Validación: No permitir registrar asistencias en el futuro
            if (fechaFiltro > DateTime.Today)
            {
                ModelState.AddModelError("", "No se puede registrar asistencia de fechas futuras.");
                return View("Error");
            }

            // Obtener la cantidad de módulos de la materia (de 1 a 4) para las columnas
            var materia = await _context.Materias.FindAsync(maId);
            ViewBag.CantModulos = materia?.MaCantModulos ?? 1;

            // Obtener el id de la relación Carrera-Cohorte seleccionada
            var carreraCohorte = await _context.CarreraCohortes
                .FirstOrDefaultAsync(cc => cc.CaId == caId && cc.CoId == coId);

            int caCoId = carreraCohorte?.CaCoId ?? 0;

            // Cargar los alumnos pertenecientes a esa Carrera y Cohorte
            var alumnos = await _context.Usuarios
                .Where(u => u.RoId == 3 && u.CaCoId == caCoId)
                .OrderBy(u => u.UsApellido)
                .ThenBy(u => u.UsNombre)
                .ToListAsync();

            // Consultar si ya se tomó asistencia este día para precargar los datos
            var asistenciasExistentes = await _context.Asistencias
                .Where(a => a.MaId == maId && a.AsFecha.Value.Date == fechaFiltro.Date)
                .ToDictionaryAsync(a => a.UsId.Value, a => a);

            var carrera = await _context.Carreras.FindAsync(caId);
            ViewBag.CarreraNombre = carrera?.CaDenominacion ?? "Carrera";
            ViewBag.MateriaNombre = materia?.MaDenominacion ?? "Materia";

            ViewBag.AsistenciasExistentes = asistenciasExistentes;
            ViewBag.CarreraId = caId;
            ViewBag.CohorteId = coId;
            ViewBag.MateriaId = maId;
            ViewBag.Fecha = fechaFiltro;

            return View(alumnos);
        }

        // Guarda o actualiza (Upsert) de manera transaccional la asistencia del día
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarAsistencia(int maId, DateTime fecha, Dictionary<int, AsistenciaCrearDto> asistencias)
        {
            if (asistencias == null || !asistencias.Any())
            {
                return BadRequest("No se recibieron datos de asistencia para procesar.");
            }

            // Obtener el próximo ID secuencial de asistencia debido a que la PK no es autoincremental
            int proximoAsId = _context.Asistencias.Any() ? await _context.Asistencias.MaxAsync(a => a.AsId) + 1 : 1;

            foreach (var record in asistencias)
            {
                int alumnoId = record.Key;
                bool estaPresente = record.Value.AsPresente;
                bool justificado = record.Value.AsJustificacion;

                // Regla de negocio: si el alumno asistió, la justificación de falta debe ser falsa
                if (estaPresente)
                {
                    justificado = false;
                }

                // Buscar si ya existe un registro de asistencia para este alumno en este día y materia
                var asistenciaExistente = await _context.Asistencias
                    .FirstOrDefaultAsync(a => a.UsId == alumnoId && a.MaId == maId && a.AsFecha.Value.Date == fecha.Date);

                if (asistenciaExistente != null)
                {
                    // Actualizar registro existente
                    asistenciaExistente.AsPresente = estaPresente;
                    asistenciaExistente.AsJustificacion = justificado;
                    _context.Update(asistenciaExistente);
                }
                else
                {
                    // Crear nuevo registro de asistencia
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

            // Guardar todos los cambios transaccionalmente en la base de datos
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Las asistencias han sido guardadas correctamente.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region SECCIÓN 3: CONSULTA DE HISTORIAL (ASISTENCIA GLOBAL)

        // Permite visualizar el historial completo de planillas y regularidad
        [HttpGet]
        public async Task<IActionResult> HistorialAsistencias(int maId)
        {
            var materia = await _context.Materias.FindAsync(maId);
            ViewBag.MateriaNombre = materia?.MaDenominacion ?? "Materia";
            ViewBag.MateriaId = maId;

            // Obtener todos los registros de asistencia vinculados a esta materia
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

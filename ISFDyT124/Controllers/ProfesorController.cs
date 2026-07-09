using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;

namespace ISFDyT124.Controllers
{
    /// <summary>
    /// Controlador del módulo Profesor: el docente ve sus cátedras (materias que tiene),
    /// toma/edita la asistencia diaria y consulta el historial de asistencias.
    /// Usa exactamente el rol "Profesor" (tal como está sembrado en la tabla ROLES).
    /// Trabaja con los DTOs de la carpeta /DTO en lugar de exponer las entidades.
    /// </summary>
    [Authorize(Roles = "Profesor")]
    public class ProfesorController : Controller
    {
        private readonly InstitutoDbContext _context;

        public ProfesorController(InstitutoDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        #region SECCIÓN 1: DASHBOARD — CÁTEDRAS DEL DOCENTE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Pantalla de inicio del profesor: lista SOLO las cátedras (Carrera-Materia)
        /// que tiene asignadas, para que elija con cuál trabajar.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var docenteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(docenteIdClaim, out int docenteId))
                return Unauthorized();

            // Cátedras del docente vía Inscripciones -> CarrerasMaterias, proyectadas al DTO.
            var catedras = await _context.Inscripciones
                .Where(i => i.UsId == docenteId && i.CarrerasMaterias != null)
                .Include(i => i.CarrerasMaterias!).ThenInclude(cm => cm.Carrera)
                .Include(i => i.CarrerasMaterias!).ThenInclude(cm => cm.Materia)
                .Select(i => new CarreraMateriaDetalleDto
                {
                    CaMaId = i.CarrerasMaterias!.CaMaId,
                    CaId = i.CarrerasMaterias.CaId,
                    MaId = i.CarrerasMaterias.MaId,
                    CarreraDenominacion = i.CarrerasMaterias.Carrera != null ? i.CarrerasMaterias.Carrera.CaDenominacion : "-",
                    MateriaDenominacion = i.CarrerasMaterias.Materia != null ? i.CarrerasMaterias.Materia.MaDenominacion : "-"
                })
                .ToListAsync();

            // Deduplicar por cátedra (un docente podría tener varias inscripciones a la misma).
            catedras = catedras
                .GroupBy(c => c.CaMaId)
                .Select(g => g.First())
                .OrderBy(c => c.CarreraDenominacion)
                .ThenBy(c => c.MateriaDenominacion)
                .ToList();

            return View(catedras);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region SECCIÓN 2: TOMA Y EDICIÓN DE ASISTENCIA DIARIA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Renderiza la planilla de alumnos de una cátedra para tomar asistencia en una fecha.
        /// Si ya se cargó asistencia ese día, la precarga para poder editarla.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Asistencia(int caMaId, DateTime? fecha)
        {
            var fechaFiltro = fecha ?? DateTime.Today;

            // No permitir cargar asistencia de fechas futuras.
            if (fechaFiltro.Date > DateTime.Today)
            {
                TempData["ErrorMessage"] = "No se puede registrar asistencia de fechas futuras.";
                return RedirectToAction(nameof(Index));
            }

            var catedra = await _context.CarrerasMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .FirstOrDefaultAsync(cm => cm.CaMaId == caMaId);

            if (catedra == null)
                return NotFound();

            int maId = catedra.MaId;

            // Datos de encabezado / columnas de módulos.
            ViewBag.CaMaId = caMaId;
            ViewBag.MateriaId = maId;
            ViewBag.Fecha = fechaFiltro;
            ViewBag.CarreraNombre = catedra.Carrera?.CaDenominacion ?? "Carrera";
            ViewBag.MateriaNombre = catedra.Materia?.MaDenominacion ?? "Materia";
            ViewBag.CantModulos = catedra.Materia?.MaCantModulos ?? 1;

            // Alumnos inscriptos en esa cátedra (Carrera-Materia), proyectados al DTO.
            var alumnos = await _context.Inscripciones
                .Where(i => i.CaMaId == caMaId
                            && i.Usuarios != null
                            && i.Usuarios.Rol != null
                            && i.Usuarios.Rol.RoDenominacion == "Alumno")
                .Include(i => i.Usuarios!).ThenInclude(u => u.Rol)
                .Select(i => new UsuarioDetalleDto
                {
                    UsId = i.Usuarios!.UsId,
                    UsApellido = i.Usuarios.UsApellido,
                    UsNombre = i.Usuarios.UsNombre,
                    UsEmail = i.Usuarios.UsEmail,
                    RoDenominacion = i.Usuarios.Rol != null ? i.Usuarios.Rol.RoDenominacion : null
                })
                .OrderBy(u => u.UsApellido)
                .ThenBy(u => u.UsNombre)
                .ToListAsync();

            // Asistencias ya registradas ese día (para precargar/editar), como dict UsId -> DTO.
            var existentes = await _context.Asistencias
                .Where(a => a.MaId == maId && a.AsFecha != null && a.AsFecha.Value.Date == fechaFiltro.Date)
                .Select(a => new AsistenciaDetalleDto
                {
                    AsId = a.AsId,
                    UsId = a.UsId,
                    MaId = a.MaId,
                    AsFecha = a.AsFecha,
                    AsPresente = a.AsPresente,
                    AsJustificacion = a.AsJustificacion
                })
                .ToListAsync();

            ViewBag.AsistenciasExistentes = existentes.ToDictionary(a => a.UsId ?? 0);

            return View(alumnos);
        }

        /// <summary>
        /// Guarda o actualiza (upsert) la asistencia del día en lote, usando AsistenciaCrearDto.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asistencia(int maId, DateTime fecha, List<AsistenciaCrearDto> asistencias)
        {
            if (asistencias == null || asistencias.Count == 0)
            {
                TempData["ErrorMessage"] = "No se recibieron datos de asistencia para procesar.";
                return RedirectToAction(nameof(Index));
            }

            // La PK de ASISTENCIAS no es autoincremental: se calcula manualmente.
            int proximoAsId = _context.Asistencias.Any()
                ? await _context.Asistencias.MaxAsync(a => a.AsId) + 1
                : 1;

            foreach (var dto in asistencias)
            {
                if (dto.UsId == null) continue;

                bool presente = dto.AsPresente;
                // Regla de negocio: si asistió, no corresponde justificación de falta.
                bool justificado = presente ? false : dto.AsJustificacion;

                var existente = await _context.Asistencias.FirstOrDefaultAsync(a =>
                    a.UsId == dto.UsId && a.MaId == maId &&
                    a.AsFecha != null && a.AsFecha.Value.Date == fecha.Date);

                if (existente != null)
                {
                    existente.AsPresente = presente;
                    existente.AsJustificacion = justificado;
                    _context.Update(existente);
                }
                else
                {
                    _context.Asistencias.Add(new Asistencia
                    {
                        AsId = proximoAsId++,
                        AsFecha = fecha.Date,
                        AsPresente = presente,
                        AsJustificacion = justificado,
                        UsId = dto.UsId,
                        MaId = maId
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Las asistencias han sido guardadas correctamente.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region SECCIÓN 3: HISTORIAL DE ASISTENCIAS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Historial (solo lectura) de todas las asistencias registradas para una materia.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> HistorialAsistencias(int maId)
        {
            ViewBag.MateriaId = maId;
            ViewBag.MateriaNombre = (await _context.Materias.FindAsync(maId))?.MaDenominacion ?? "Materia";

            var historial = await _context.Asistencias
                .Where(a => a.MaId == maId)
                .Include(a => a.Usuario)
                .Include(a => a.Materias)
                .OrderByDescending(a => a.AsFecha)
                .ThenBy(a => a.Usuario!.UsApellido)
                .Select(a => new AsistenciaDetalleDto
                {
                    AsId = a.AsId,
                    UsId = a.UsId,
                    MaId = a.MaId,
                    AsFecha = a.AsFecha,
                    AsPresente = a.AsPresente,
                    AsJustificacion = a.AsJustificacion,
                    UsuarioNombre = a.Usuario != null ? a.Usuario.UsApellido + ", " + a.Usuario.UsNombre : "-",
                    MateriaDenominacion = a.Materias != null ? a.Materias.MaDenominacion : "-"
                })
                .ToListAsync();

            return View(historial);
        }

        #endregion
    }
}

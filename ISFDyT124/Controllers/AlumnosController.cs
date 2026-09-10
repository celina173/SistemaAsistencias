using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    /// <summary>
    /// Gestión de estudiantes (ticket 3.3). En pantalla todo se rotula "Estudiantes";
    /// el nombre de código mantiene "Alumno" por consistencia con el resto del proyecto.
    ///
    /// Es un CRUD acotado al rol Estudiante (RoId = 3), separado del CRUD genérico de
    /// AdminController a propósito: acá también entra el Docente, y las acciones de
    /// AdminController pueden crear/editar CUALQUIER rol (incluido Admin) — abrirlas al
    /// Docente sería una escalada de privilegios.
    ///
    /// Alcance por rol:
    /// - Admin / Dirección: ven y gestionan todos los estudiantes.
    /// - Docente: solo los estudiantes de las carreras de sus cátedras (mismo criterio
    ///   que la planilla de asistencia — vía Usuarios.CaCoId, no por materia).
    /// </summary>
    [Authorize(Roles = "Admin,Dirección,Docente")]
    public class AlumnosController : Controller
    {
        private const int RolEstudianteId = 3;

        private readonly InstitutoDbContext _context;

        public AlumnosController(InstitutoDbContext context)
        {
            _context = context;
        }

        private int UsuarioActualId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool EsAdminODireccion =>
            User.IsInRole("Admin") || User.IsInRole("Dirección");

        /// <summary>
        /// IDs de CarreraCohorte que el usuario actual tiene permitido ver/asignar.
        /// null = sin restricción (Admin/Dirección). Lista = las carrera/cohorte de las
        /// carreras en las que el Docente tiene al menos una cátedra.
        /// </summary>
        private async Task<List<int>?> CaCoIdsPermitidosAsync()
        {
            if (EsAdminODireccion)
                return null;

            var carreraIds = await _context
                .Usuarios.Where(u => u.UsId == UsuarioActualId)
                .SelectMany(u => u.CarreraMaterias)
                .Select(cm => cm.CaId)
                .Distinct()
                .ToListAsync();

            return await _context
                .CarreraCohortes.Where(cc => carreraIds.Contains(cc.CaId))
                .Select(cc => cc.CaCoId)
                .ToListAsync();
        }

        /// <summary>Query base de estudiantes visibles según el alcance del usuario actual.</summary>
        private IQueryable<Usuario> AlumnosVisibles(List<int>? caCoIdsPermitidos)
        {
            var query = _context.Usuarios.Where(u => u.RoId == RolEstudianteId);

            if (caCoIdsPermitidos != null)
                query = query.Where(u =>
                    u.CaCoId != null && caCoIdsPermitidos.Contains(u.CaCoId.Value)
                );

            return query;
        }

        /// <summary>Carga en ViewBag la lista de Carrera/Cohorte para los formularios, ya filtrada por alcance.</summary>
        private async Task CargarCarreraCohortesAsync(List<int>? caCoIdsPermitidos)
        {
            var query = _context
                .CarreraCohortes.Include(cc => cc.Carrera)
                .Include(cc => cc.Cohorte)
                .AsQueryable();

            if (caCoIdsPermitidos != null)
                query = query.Where(cc => caCoIdsPermitidos.Contains(cc.CaCoId));

            ViewBag.CarreraCohortesList = await query
                .Select(cc => new
                {
                    cc.CaCoId,
                    Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio,
                })
                .ToListAsync();
        }

        /// <summary>CaId de la carrera a la que pertenece un CarreraCohorte (null si no existe).</summary>
        private async Task<int?> CaIdDeCarreraCohorteAsync(int caCoId) =>
            await _context
                .CarreraCohortes.Where(cc => cc.CaCoId == caCoId)
                .Select(cc => (int?)cc.CaId)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Repone en ViewBag lo que necesitan los formularios al re-renderizar por error.
        /// Las materias las trae la vista por AJAX (MateriasPorCarreraCohorte).
        /// </summary>
        private Task RecargarFormAsync(AlumnoFormDto model, List<int>? caCoIdsPermitidos) =>
            CargarCarreraCohortesAsync(caCoIdsPermitidos);

        /// <summary>De la selección recibida, devuelve solo los CaMaId que realmente son materias de esa carrera.</summary>
        private async Task<List<int>> CaMaIdsValidosAsync(int caCoId, List<int>? seleccion)
        {
            if (seleccion == null || seleccion.Count == 0)
                return new List<int>();

            int? caId = await CaIdDeCarreraCohorteAsync(caCoId);
            if (caId == null)
                return new List<int>();

            return await _context
                .CarreraMaterias.Where(cm => cm.CaId == caId.Value && seleccion.Contains(cm.CaMaId))
                .Select(cm => cm.CaMaId)
                .ToListAsync();
        }

        /// <summary>
        /// Endpoint AJAX para el formulario: materias de la carrera de un CarreraCohorte.
        /// Acotado al alcance del usuario (un Docente solo puede pedir las de sus carreras).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MateriasPorCarreraCohorte(int caCoId)
        {
            var permitidos = await CaCoIdsPermitidosAsync();
            if (permitidos != null && !permitidos.Contains(caCoId))
                return Json(Array.Empty<object>());

            int? caId = await CaIdDeCarreraCohorteAsync(caCoId);
            if (caId == null)
                return Json(Array.Empty<object>());

            var materias = await _context
                .CarreraMaterias.Where(cm => cm.CaId == caId.Value)
                .Include(cm => cm.Materia)
                .Select(cm => new { caMaId = cm.CaMaId, denominacion = cm.Materia!.MaDenominacion })
                .ToListAsync();

            return Json(materias);
        }

        // ─────────────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var permitidos = await CaCoIdsPermitidosAsync();

            var alumnos = await AlumnosVisibles(permitidos)
                .Include(u => u.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(u => u.CarreraCohorte)
                    .ThenInclude(cc => cc!.Cohorte)
                .OrderBy(u => u.UsApellido)
                .ThenBy(u => u.UsNombre)
                .Select(u => new UsuarioDetalleDto
                {
                    UsId = u.UsId,
                    UsApellido = u.UsApellido,
                    UsNombre = u.UsNombre,
                    UsEmail = u.UsEmail,
                    UsDni = u.UsDni,
                    RoId = u.RoId,
                    CaCoId = u.CaCoId,
                    CarreraCohorteDenominacion =
                        u.CaCoId != null && u.CarreraCohorte != null
                            ? u.CarreraCohorte.Carrera!.CaDenominacion
                                + " - "
                                + u.CarreraCohorte.Cohorte!.CoAnio
                            : null,
                })
                .ToListAsync();

            return View(alumnos);
        }

        [HttpGet]
        public async Task<IActionResult> Agregar()
        {
            var model = new AlumnoFormDto();
            await RecargarFormAsync(model, await CaCoIdsPermitidosAsync());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(AlumnoFormDto model)
        {
            var permitidos = await CaCoIdsPermitidosAsync();

            if (!ModelState.IsValid)
            {
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            if (model.CaCoId == null)
            {
                ModelState.AddModelError(
                    nameof(model.CaCoId),
                    "El estudiante debe estar asociado a una carrera/cohorte."
                );
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            if (permitidos != null && !permitidos.Contains(model.CaCoId.Value))
            {
                ModelState.AddModelError(
                    nameof(model.CaCoId),
                    "Solo podés asignar estudiantes a las carreras de tus cátedras."
                );
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni))
            {
                ModelState.AddModelError(nameof(model.UsDni), "El DNI ya se encuentra registrado.");
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            int nuevoUsId = await _context.Usuarios.AnyAsync()
                ? await _context.Usuarios.MaxAsync(u => u.UsId) + 1
                : 1;

            var alumno = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDni = model.UsDni,
                UsEmail = model.UsEmail,
                // El estudiante no inicia sesión, pero la columna es NOT NULL: se guarda
                // el hash del DNI, mismo criterio que UsuarioAgregar en AdminController.
                UsContrasena = PasswordService.HashPassword(model.UsDni.ToString()),
                RoId = RolEstudianteId,
                CaCoId = model.CaCoId,
            };

            _context.Usuarios.Add(alumno);
            await _context.SaveChangesAsync();

            // Inscripciones a las materias elegidas (mismo criterio que la carga masiva:
            // la inscripción es lo que hace que se le tome asistencia en esa cátedra).
            var caMaValidos = await CaMaIdsValidosAsync(model.CaCoId.Value, model.SelectedCaMaIds);
            foreach (var caMaId in caMaValidos)
                _context.Inscripciones.Add(new Inscripciones { UsId = alumno.UsId, CaMaId = caMaId });
            if (caMaValidos.Count > 0)
                await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Estudiante agregado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var permitidos = await CaCoIdsPermitidosAsync();

            var alumno = await AlumnosVisibles(permitidos).FirstOrDefaultAsync(u => u.UsId == id);

            if (alumno == null)
                return NotFound();

            var dto = new AlumnoFormDto
            {
                UsId = alumno.UsId,
                UsApellido = alumno.UsApellido,
                UsNombre = alumno.UsNombre,
                UsEmail = alumno.UsEmail,
                UsDni = alumno.UsDni,
                CaCoId = alumno.CaCoId,
                SelectedCaMaIds = await _context
                    .Inscripciones.Where(i => i.UsId == alumno.UsId)
                    .Select(i => i.CaMaId)
                    .ToListAsync(),
            };

            await RecargarFormAsync(dto, permitidos);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, AlumnoFormDto model)
        {
            if (id != model.UsId)
                return BadRequest();

            var permitidos = await CaCoIdsPermitidosAsync();

            var alumno = await AlumnosVisibles(permitidos).FirstOrDefaultAsync(u => u.UsId == id);

            if (alumno == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            if (model.CaCoId == null)
            {
                ModelState.AddModelError(
                    nameof(model.CaCoId),
                    "El estudiante debe estar asociado a una carrera/cohorte."
                );
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            if (permitidos != null && !permitidos.Contains(model.CaCoId.Value))
            {
                ModelState.AddModelError(
                    nameof(model.CaCoId),
                    "Solo podés asignar estudiantes a las carreras de tus cátedras."
                );
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni && u.UsId != id))
            {
                ModelState.AddModelError(nameof(model.UsDni), "El DNI ya se encuentra registrado.");
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            alumno.UsApellido = model.UsApellido;
            alumno.UsNombre = model.UsNombre;
            alumno.UsDni = model.UsDni;
            alumno.UsEmail = model.UsEmail;
            alumno.CaCoId = model.CaCoId;

            // Sincroniza las inscripciones con lo tildado: como el estudiante pertenece a
            // una sola carrera, su set final de inscripciones = las materias válidas de esa
            // carrera que quedaron marcadas. Las asistencias ya cargadas (UsId + MaId) no
            // se tocan; solo cambia de qué cátedras se lo lista de acá en más.
            var caMaValidos = await CaMaIdsValidosAsync(model.CaCoId.Value, model.SelectedCaMaIds);
            var deseadas = caMaValidos.ToHashSet();
            var inscActuales = await _context
                .Inscripciones.Where(i => i.UsId == id)
                .ToListAsync();

            _context.Inscripciones.RemoveRange(
                inscActuales.Where(i => !deseadas.Contains(i.CaMaId))
            );

            var existentes = inscActuales.Select(i => i.CaMaId).ToHashSet();
            foreach (var caMaId in caMaValidos.Where(x => !existentes.Contains(x)))
                _context.Inscripciones.Add(new Inscripciones { UsId = id, CaMaId = caMaId });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Estudiante actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var permitidos = await CaCoIdsPermitidosAsync();

            var alumno = await AlumnosVisibles(permitidos)
                .Include(u => u.UsuarioRoles)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (alumno == null)
                return NotFound();

            var inscripciones = await _context
                .Inscripciones.Where(i => i.UsId == alumno.UsId)
                .ToListAsync();

            _context.Inscripciones.RemoveRange(inscripciones);
            _context.UsuarioRoles.RemoveRange(alumno.UsuarioRoles);
            _context.Usuarios.Remove(alumno);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Estudiante eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}

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
    [Authorize(Roles = "Admin,Dirección,Docente")]
    public class AlumnosController : Controller
    {
        private readonly InstitutoDbContext _context;

        public AlumnosController(InstitutoDbContext context)
        {
            _context = context;
        }

        private int UsuarioActualId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool EsAdminODireccion =>
            User.IsInRole("Admin") || User.IsInRole("Dirección");

        private async Task<List<int>?> CaCoIdsPermitidosAsync()
        {
            if (EsAdminODireccion)
                return null;

            return await _context
                .Usuarios.Where(u => u.UsId == UsuarioActualId)
                .SelectMany(u => u.CarreraMaterias)
                .Where(cm => cm.CaCoId != null)
                .Select(cm => cm.CaCoId!.Value)
                .Distinct()
                .ToListAsync();
        }

        private IQueryable<Usuario> AlumnosVisibles(List<int>? caCoIdsPermitidos)
        {
            var query = _context.Usuarios.Where(u => u.RoId == RolId.Estudiante);

            if (caCoIdsPermitidos != null)
                query = query.Where(u =>
                    u.CaCoId != null && caCoIdsPermitidos.Contains(u.CaCoId.Value)
                );

            return query;
        }

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

        private Task RecargarFormAsync(AlumnoFormDto model, List<int>? caCoIdsPermitidos) =>
            CargarCarreraCohortesAsync(caCoIdsPermitidos);

        private async Task<List<int>> CaMaIdsValidosAsync(int caCoId, List<int>? seleccion)
        {
            if (seleccion == null || seleccion.Count == 0)
                return new List<int>();

            return await _context
                .CarreraMaterias.Where(cm => cm.CaCoId == caCoId && seleccion.Contains(cm.CaMaId))
                .Select(cm => cm.CaMaId)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> MateriasPorCarreraCohorte(int caCoId)
        {
            var permitidos = await CaCoIdsPermitidosAsync();
            if (permitidos != null && !permitidos.Contains(caCoId))
                return Json(Array.Empty<object>());

            var materias = await _context
                .CarreraMaterias.Where(cm => cm.CaCoId == caCoId)
                .Include(cm => cm.Materia)
                .Select(cm => new { caMaId = cm.CaMaId, denominacion = cm.Materia!.MaDenominacion })
                .ToListAsync();

            return Json(materias);
        }

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
                    UsActivo = u.UsActivo,
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
            var model = new AlumnoFormDto
            {
                UsActivo = true,
                InFechaInscripcion = DateTime.Today
            };
            await RecargarFormAsync(model, await CaCoIdsPermitidosAsync());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(AlumnoFormDto model)
        {
            var permitidos = await CaCoIdsPermitidosAsync();

            if (model.InFechaInscripcion.HasValue && model.InFechaInscripcion.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.InFechaInscripcion), "La fecha de inscripción no puede ser posterior al día de hoy.");
            }

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

            if (!string.IsNullOrWhiteSpace(model.UsEmail) && await _context.Usuarios.AnyAsync(u => u.UsEmail == model.UsEmail))
            {
                ModelState.AddModelError(nameof(model.UsEmail), "El email ya se encuentra registrado.");
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            using var transaccionId = await _context.Database.BeginTransactionAsync();

            int nuevoUsId = await _context
                .Database.SqlQuery<int>(
                    $"SELECT ISNULL(MAX(UsId), 0) + 1 AS Value FROM Usuarios WITH (UPDLOCK, HOLDLOCK)"
                )
                .FirstAsync();

            var alumno = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDni = model.UsDni,
                UsEmail = model.UsEmail,
                UsContrasena = PasswordService.HashPassword(model.UsDni.ToString()),
                RoId = RolId.Estudiante,
                CaCoId = model.CaCoId,
                UsActivo = model.UsActivo
            };

            _context.Usuarios.Add(alumno);
            await _context.SaveChangesAsync();

            DateTime fechaInscripcion = model.InFechaInscripcion ?? DateTime.Today;

            var caMaValidos = await CaMaIdsValidosAsync(model.CaCoId.Value, model.SelectedCaMaIds);
            foreach (var caMaId in caMaValidos)
                _context.Inscripciones.Add(new Inscripciones { UsId = alumno.UsId, CaMaId = caMaId, InFechaInscripcion = fechaInscripcion });
            if (caMaValidos.Count > 0)
                await _context.SaveChangesAsync();

            await transaccionId.CommitAsync();

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

            var fechaExistente = await _context.Inscripciones
                .Where(i => i.UsId == alumno.UsId)
                .Select(i => (DateTime?)i.InFechaInscripcion)
                .FirstOrDefaultAsync();

            var dto = new AlumnoFormDto
            {
                UsId = alumno.UsId,
                UsApellido = alumno.UsApellido,
                UsNombre = alumno.UsNombre,
                UsEmail = alumno.UsEmail,
                UsDni = alumno.UsDni,
                CaCoId = alumno.CaCoId,
                UsActivo = alumno.UsActivo,
                InFechaInscripcion = fechaExistente ?? DateTime.Today,
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

            if (model.InFechaInscripcion.HasValue && model.InFechaInscripcion.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.InFechaInscripcion), "La fecha de inscripción no puede ser posterior al día de hoy.");
            }

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

            if (!string.IsNullOrWhiteSpace(model.UsEmail) && await _context.Usuarios.AnyAsync(u => u.UsEmail == model.UsEmail && u.UsId != id))
            {
                ModelState.AddModelError(nameof(model.UsEmail), "El email ya se encuentra registrado por otro usuario.");
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            alumno.UsApellido = model.UsApellido;
            alumno.UsNombre = model.UsNombre;
            alumno.UsDni = model.UsDni;
            alumno.UsEmail = model.UsEmail;
            alumno.CaCoId = model.CaCoId;
            alumno.UsActivo = model.UsActivo;

            DateTime fechaInscripcion = model.InFechaInscripcion ?? DateTime.Today;

            var caMaValidos = await CaMaIdsValidosAsync(model.CaCoId.Value, model.SelectedCaMaIds);
            var deseadas = caMaValidos.ToHashSet();
            var inscActuales = await _context
                .Inscripciones.Where(i => i.UsId == id)
                .ToListAsync();

            _context.Inscripciones.RemoveRange(
                inscActuales.Where(i => !deseadas.Contains(i.CaMaId))
            );

            foreach (var insc in inscActuales.Where(i => deseadas.Contains(i.CaMaId)))
            {
                insc.InFechaInscripcion = fechaInscripcion;
            }

            var existentes = inscActuales.Select(i => i.CaMaId).ToHashSet();
            foreach (var caMaId in caMaValidos.Where(x => !existentes.Contains(x)))
                _context.Inscripciones.Add(new Inscripciones { UsId = id, CaMaId = caMaId, InFechaInscripcion = fechaInscripcion });

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
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (alumno == null)
                return NotFound();

            var inscripciones = await _context
                .Inscripciones.Where(i => i.UsId == alumno.UsId)
                .ToListAsync();

            _context.Inscripciones.RemoveRange(inscripciones);
            _context.Usuarios.Remove(alumno);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo eliminar el estudiante. Puede tener datos relacionados que lo impiden.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Estudiante eliminado definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, bool ConfirmarCambioEstado)
        {
            var permitidos = await CaCoIdsPermitidosAsync();
            var alumno = await AlumnosVisibles(permitidos).FirstOrDefaultAsync(u => u.UsId == id);
            if (alumno == null)
                return NotFound();

            if (!ConfirmarCambioEstado)
                return BadRequest(new { success = false, error = "Debe confirmar el cambio de estado del estudiante." });

            alumno.UsActivo = !alumno.UsActivo;
            await _context.SaveChangesAsync();
            return Json(new { success = true, estado = alumno.UsActivo });
        }
    }
}
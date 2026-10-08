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
                    UsActivo = u.UsActivo, // MAPEO AGREGADO PARA EL ESTADO LÓGICO
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
                UsActivo = true // Por defecto, al abrir el form de agregar, nace activo
            };
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

            // Usuarios.UsEmail tiene un índice único desde la migración Unique_Email_Usuario --
            // sin este chequeo, un email repetido pasaba la validación del modelo y recién
            // reventaba al guardar con un 500 crudo (DbUpdateException sin capturar), en vez
            // de mostrar un error claro. Mismo chequeo que ya usa AdminController.
            if (!string.IsNullOrWhiteSpace(model.UsEmail) && await _context.Usuarios.AnyAsync(u => u.UsEmail == model.UsEmail))
            {
                ModelState.AddModelError(nameof(model.UsEmail), "El email ya se encuentra registrado.");
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            // Usuarios.UsId no es IDENTITY -- se calcula a mano como MAX+1. Sin lock, dos
            // altas simultáneas podían leer el mismo MAX y chocar al insertar (ticket 6.13).
            // UPDLOCK+HOLDLOCK dentro de una transacción serializa el cálculo.
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
                UsActivo = model.UsActivo // ASIGNA EL ESTADO DESDE EL FORMULARIO (Si lo habilitaste en la vista Agregar)
            };

            _context.Usuarios.Add(alumno);
            await _context.SaveChangesAsync();

            var caMaValidos = await CaMaIdsValidosAsync(model.CaCoId.Value, model.SelectedCaMaIds);
            foreach (var caMaId in caMaValidos)
                _context.Inscripciones.Add(new Inscripciones { UsId = alumno.UsId, CaMaId = caMaId });
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

            var dto = new AlumnoFormDto
            {
                UsId = alumno.UsId,
                UsApellido = alumno.UsApellido,
                UsNombre = alumno.UsNombre,
                UsEmail = alumno.UsEmail,
                UsDni = alumno.UsDni,
                CaCoId = alumno.CaCoId,
                UsActivo = alumno.UsActivo, // MAPEO DEL ESTADO PARA PRECARGAR EL FORMULARIO DE EDICIÓN
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

            // Mismo chequeo que en Agregar: el índice único de UsEmail hacía reventar con un
            // 500 crudo si se editaba un alumno con un email ya usado por otro usuario.
            if (!string.IsNullOrWhiteSpace(model.UsEmail) && await _context.Usuarios.AnyAsync(u => u.UsEmail == model.UsEmail && u.UsId != id))
            {
                ModelState.AddModelError(nameof(model.UsEmail), "El email ya se encuentra registrado por otro usuario.");
                await RecargarFormAsync(model, permitidos);
                return View(model);
            }

            // ACTUALIZACIÓN DE DATOS DEL ESTUDIANTE, INCLUYENDO EL ESTADO LÓGICO
            alumno.UsApellido = model.UsApellido;
            alumno.UsNombre = model.UsNombre;
            alumno.UsDni = model.UsDni;
            alumno.UsEmail = model.UsEmail;
            alumno.CaCoId = model.CaCoId;
            alumno.UsActivo = model.UsActivo; // ACTUALIZA EL ESTADO DESDE EL FORMULARIO DE EDICIÓN

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

        // ─────────────────────────────────────────────────────────────────────
        // (OPCIONAL) EL MÉTODO ELIMINAR SIGUE EXISTIENDO SI QUERÉS HACER UN BORRADO DEFINITIVO
        // DESDE OTRO LUGAR, PERO EN LA VISTA YA NO HAY BOTÓN PARA LLAMARLO.
        // ─────────────────────────────────────────────────────────────────────
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

            // FIX: sin try/catch, cualquier choque de FK al borrar tiraba una
            // DbUpdateException sin capturar y reventaba la request con un error 500 en vez
            // de avisarle al usuario. Mismo criterio que ya usa AdminController.UsuarioEliminarConfirmado.
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

        // ─────────────────────────────────────────────────────────────────────
        // CAMBIO DE ESTADO LÓGICO SIN RECARGAR LA PÁGINA (AJAX)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            // Le faltaba el token antifalsificación (cualquier sitio externo podía disparar
            // este POST desde el navegador de un Admin/Docente logueado) y el chequeo de
            // alcance -- un Docente podía cambiar el estado de un alumno de una carrera que
            // no es la suya. Se agregan los dos, mismo criterio que el resto del controller.
            var permitidos = await CaCoIdsPermitidosAsync();
            var alumno = await AlumnosVisibles(permitidos).FirstOrDefaultAsync(u => u.UsId == id);
            if (alumno != null)
            {
                // Alterna el estado activo/inactivo (Toogle)
                alumno.UsActivo = !alumno.UsActivo;
                await _context.SaveChangesAsync();

                // En lugar de recargar la página, devolvemos un JSON de éxito con el nuevo estado
                return Json(new { success = true, estado = alumno.UsActivo });
            }
            return Json(new { success = false });
        }
    }
}

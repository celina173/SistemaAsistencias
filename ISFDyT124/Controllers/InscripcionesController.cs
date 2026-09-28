using ISFDyT124.Data;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ISFDyT124.Controllers
{
    [Authorize(Roles = "Admin,Dirección,Docente")]
    public class InscripcionesController : Controller
    {
        private readonly InstitutoDbContext _context;
        public InscripcionesController(InstitutoDbContext context)
        {
            _context = context;
        }

        private int UsuarioActualId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool EsAdminODireccion =>
            User.IsInRole("Admin") || User.IsInRole("Dirección");

        /// <summary>
        /// IDs de CarreraMateria (cátedra puntual) que el usuario actual tiene permitido
        /// ver/editar. null = sin restricción (Admin/Dirección). Lista = solo las cátedras
        /// propias del Docente -- antes un Docente veía y editaba inscripciones de
        /// CUALQUIER carrera del instituto, sin ningún scoping (hallazgo de seguridad).
        /// </summary>
        private async Task<List<int>?> CaMaIdsPermitidosAsync()
        {
            if (EsAdminODireccion)
                return null;

            return await _context
                .Usuarios.Where(u => u.UsId == UsuarioActualId)
                .SelectMany(u => u.CarreraMaterias)
                .Select(cm => cm.CaMaId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Inscripciones.ToListAsync());
        }

        // GET: INSCRIPCIONESS/GestionInscripcionesMaterias or INSCRIPCIONESS/GestionInscripcionesMaterias/5
        public async Task<IActionResult> GestionInscripcionesMaterias(int? inid)
        {
            var permitidos = await CaMaIdsPermitidosAsync();

            if (inid == null)
            {
                // No id provided: show the management/list page (the view can render a list or present UI to add/edit)
                var query = _context.Inscripciones
                    .Include(i => i.Usuarios)
                    .Include(i => i.CarreraMateria).ThenInclude(cm => cm!.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                    .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Materia)
                    .AsQueryable();

                // Un Docente solo ve inscripciones de sus propias cátedras (antes veía las
                // de cualquier carrera del instituto, sin ningún filtro).
                if (permitidos != null)
                    query = query.Where(i => permitidos.Contains(i.CaMaId));

                var all = await query.ToListAsync();

                // Defensive: if any navigation is null, try to load it explicitly to avoid empty cells in the view
                for (int idx = 0; idx < all.Count; idx++)
                {
                    var ins = all[idx];
                    if (ins.Usuarios == null)
                    {
                        var u = await _context.Usuarios.FindAsync(ins.UsId);
                        ins.Usuarios = u;
                    }

                    if (ins.CarreraMateria == null)
                    {
                        var cm = await _context.CarreraMaterias
                            .Include(x => x.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                            .Include(x => x.Materia)
                            .FirstOrDefaultAsync(x => x.CaMaId == ins.CaMaId);
                        ins.CarreraMateria = cm;
                    }
                }

                return View(all);
            }

            var inscripciones = await _context.Inscripciones
                .Include(i => i.Usuarios)
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm!.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Materia)
                .FirstOrDefaultAsync(m => m.InId == inid);
            if (inscripciones == null)
            {
                return NotFound();
            }

            // Un Docente no puede ver el detalle de una inscripción de una cátedra ajena.
            if (permitidos != null && !permitidos.Contains(inscripciones.CaMaId))
            {
                return NotFound();
            }

            // return a list with the single record so the view can render uniformly as a list
            return View("GestionInscripcionesMaterias", new List<Inscripciones> { inscripciones });
        }

        // GET: INSCRIPCIONESS/Create
        public async Task<IActionResult> AgregarInscripcionMateria()
        {
            // find role id for 'Estudiante' (case-insensitive)
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion.ToLower() == "estudiante");

            List<object> estudiantes;
            if (rol != null)
            {
                estudiantes = await _context.Usuarios
                    .Where(u => u.RoId == rol.RoId)
                    .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                    .ToListAsync<object>();
            }
            else
            {
                // no role named 'Estudiante' found -> empty list
                estudiantes = new List<object>();
            }

            ViewData["UsId"] = new SelectList(estudiantes, "UsId", "FullName");

            // Un Docente solo puede inscribir alumnos en sus propias cátedras (antes veía
            // y podía elegir cualquier carrera/materia del instituto).
            var permitidos = await CaMaIdsPermitidosAsync();

            var camQuery = _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Cohorte)
                .Include(cm => cm.Materia)
                .AsQueryable();
            if (permitidos != null)
                camQuery = camQuery.Where(cm => permitidos.Contains(cm.CaMaId));

            var cam = await camQuery
                .Select(cm => new
                {
                    cm.CaMaId,
                    Display = (cm.CarreraCohorte != null && cm.CarreraCohorte.Carrera != null ? cm.CarreraCohorte.Carrera.CaDenominacion : "")
                        + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "")
                        + (cm.CarreraCohorte != null && cm.CarreraCohorte.Cohorte != null ? $" ({cm.CarreraCohorte.Cohorte.CoAnio})" : "")
                })
                .ToListAsync();
            ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "Display");

            // populate Carreras and Materias separately for the autocomplete inputs, acotadas
            // a las que participan en alguna cátedra permitida.
            List<int> caIdsPermitidos;
            List<int> maIdsPermitidos;
            if (permitidos != null)
            {
                var camPermitidas = await _context.CarreraMaterias
                    .Where(cm => permitidos.Contains(cm.CaMaId))
                    .Include(cm => cm.CarreraCohorte)
                    .ToListAsync();
                caIdsPermitidos = camPermitidas
                    .Where(cm => cm.CarreraCohorte != null)
                    .Select(cm => cm.CarreraCohorte!.CaId)
                    .Distinct()
                    .ToList();
                maIdsPermitidos = camPermitidas.Select(cm => cm.MaId).Distinct().ToList();
            }
            else
            {
                caIdsPermitidos = await _context.Carreras.Select(c => c.CaId).ToListAsync();
                maIdsPermitidos = await _context.Materias.Select(m => m.MaId).ToListAsync();
            }

            var carreras = await _context.Carreras
                .Where(c => caIdsPermitidos.Contains(c.CaId))
                .Select(c => new { c.CaId, c.CaDenominacion })
                .ToListAsync();

            var materias = await _context.Materias
                .Where(m => maIdsPermitidos.Contains(m.MaId))
                .Select(m => new { m.MaId, m.MaDenominacion })
                .ToListAsync();

            // expose JSON for client-side filtering
            ViewData["StudentsJson"] = JsonSerializer.Serialize(estudiantes);
            ViewData["CarrerasJson"] = JsonSerializer.Serialize(carreras);
            ViewData["MateriasJson"] = JsonSerializer.Serialize(materias);

            return View();
        }

        // POST: INSCRIPCIONESS/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarInscripcionMateria([Bind("InId,UsId,CaMaId")] Inscripciones inscripciones, int? SelectedUsId, int? SelectedCaId, int? SelectedMaId)
        {
            // Map frontend-selected ids into the entity before validation
            if (SelectedUsId.HasValue)
            {
                inscripciones.UsId = SelectedUsId.Value;
            }

            if (SelectedCaId.HasValue && SelectedMaId.HasValue)
            {
                var caMa = await _context.CarreraMaterias.FirstOrDefaultAsync(cm =>
                    cm.CarreraCohorte != null
                    && cm.CarreraCohorte.CaId == SelectedCaId.Value
                    && cm.MaId == SelectedMaId.Value);
                if (caMa != null)
                {
                    inscripciones.CaMaId = caMa.CaMaId;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "No existe la combinación seleccionada de Carrera y Materia.");
                }
            }

            // Evitar duplicados: si ya existe una inscripción para el mismo alumno y carrera/materia, informar error
            if (inscripciones.UsId != 0 && inscripciones.CaMaId != 0)
            {
                var already = await _context.Inscripciones.AnyAsync(i => i.UsId == inscripciones.UsId && i.CaMaId == inscripciones.CaMaId);
                if (already)
                {
                    ModelState.AddModelError(string.Empty, "El estudiante ya está inscripto en la carrera/materia seleccionada.");
                }
            }

            // Un Docente no puede inscribir alumnos en una cátedra que no es suya, ni
            // aunque arme el POST a mano con un CaMaId ajeno.
            var permitidosPost = await CaMaIdsPermitidosAsync();
            if (permitidosPost != null && !permitidosPost.Contains(inscripciones.CaMaId))
            {
                ModelState.AddModelError(string.Empty, "No tenés esa cátedra asignada.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(inscripciones);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(GestionInscripcionesMaterias));
                }
                catch (Exception)
                {
                    // No se expone el mensaje crudo de la excepción al usuario -- podía
                    // filtrar detalles internos de la base de datos (ticket 6.14).
                    ModelState.AddModelError(string.Empty, "No se pudo guardar la inscripción. Verifique los datos e intente nuevamente.");
                }
            }

            // repopulate selects when returning view on error
            var estudiantes = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
                .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                .ToListAsync();
            ViewData["UsId"] = new SelectList(estudiantes, "UsId", "FullName", inscripciones.UsId);

            var cam = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Cohorte)
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    cm.CaMaId,
                    Display = (cm.CarreraCohorte != null && cm.CarreraCohorte.Carrera != null ? cm.CarreraCohorte.Carrera.CaDenominacion : "")
                        + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "")
                        + (cm.CarreraCohorte != null && cm.CarreraCohorte.Cohorte != null ? $" ({cm.CarreraCohorte.Cohorte.CoAnio})" : "")
                })
                .ToListAsync();
            ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "Display", inscripciones.CaMaId);

            // also repopulate JSON lists required by the autocomplete view
            var estudiantesJsonList = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
                .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                .ToListAsync();
            ViewData["StudentsJson"] = JsonSerializer.Serialize(estudiantesJsonList);

            var carrerasJsonList = await _context.Carreras
                .Select(c => new { c.CaId, c.CaDenominacion })
                .ToListAsync();
            ViewData["CarrerasJson"] = JsonSerializer.Serialize(carrerasJsonList);

            var materiasJsonList = await _context.Materias
                .Select(m => new { m.MaId, m.MaDenominacion })
                .ToListAsync();
            ViewData["MateriasJson"] = JsonSerializer.Serialize(materiasJsonList);

            return View(inscripciones);
        }

        // GET: INSCRIPCIONESS/Edit/5
        public async Task<IActionResult> ModificarInscripcionMateria(int? inid)
        {
            if (inid == null)
            {
                // No id: redirect back to management list
                return RedirectToAction(nameof(GestionInscripcionesMaterias));
            }

            var inscripciones = await _context.Inscripciones
                .Include(i => i.Usuarios)
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm!.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Materia)
                .FirstOrDefaultAsync(i => i.InId == inid);
            if (inscripciones == null)
            {
                return NotFound();
            }

            // Un Docente no puede entrar a editar una inscripción de una cátedra ajena.
            var permitidosEdit = await CaMaIdsPermitidosAsync();
            if (permitidosEdit != null && !permitidosEdit.Contains(inscripciones.CaMaId))
            {
                return NotFound();
            }

            // Defensive: if Usuario navigation wasn't loaded for any reason, load explicitly
            if (inscripciones.Usuarios == null && inscripciones.UsId != 0)
            {
                var u = await _context.Usuarios.FindAsync(inscripciones.UsId);
                inscripciones.Usuarios = u;
            }

            // populate selects for edit view
            var estudiantes = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
                .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                .ToListAsync();
            ViewData["UsId"] = new SelectList(estudiantes, "UsId", "FullName", inscripciones.UsId);

            var cam = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Cohorte)
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    cm.CaMaId,
                    Display = (cm.CarreraCohorte != null && cm.CarreraCohorte.Carrera != null ? cm.CarreraCohorte.Carrera.CaDenominacion : "")
                        + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "")
                        + (cm.CarreraCohorte != null && cm.CarreraCohorte.Cohorte != null ? $" ({cm.CarreraCohorte.Cohorte.CoAnio})" : "")
                })
                .ToListAsync();
            ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "Display", inscripciones.CaMaId);

            return View(inscripciones);
        }

        // POST: INSCRIPCIONESS/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // FIX: el Bind incluía "Usuarios" (navegación a Usuario) y "Carreras_Materias" (nombre
        // que ni siquiera existe como propiedad, la real es "CarreraMateria"). Al permitir bindear
        // "Usuarios", un POST manual con campos "Usuarios.UsId"/"Usuarios.UsContrasena"/etc. hacía
        // que el model binder arme un Usuario anidado y que _context.Update() lo trate como
        // modificado: cualquier usuario con acceso a esta acción (incluye Docente) podía pisar
        // datos de OTRO usuario (o el propio hash de contraseña) vía overposting. El formulario
        // real solo envía InId/UsId/CaMaId, así que sacar "Usuarios" no cambia el comportamiento
        // legítimo, solo cierra el hueco.
        public async Task<IActionResult> ModificarInscripcionMateria(int? inid, [Bind("InId,UsId,CaMaId")] Inscripciones inscripciones)
        {
            if (inid != inscripciones.InId)
            {
                return NotFound();
            }

            // Un Docente no puede editar una inscripción que hoy pertenece a una cátedra
            // ajena, ni reasignarla a una cátedra ajena -- se chequean las dos, la actual
            // (antes de pisarla con Update) y la nueva que viene en el POST.
            var permitidosMod = await CaMaIdsPermitidosAsync();
            if (permitidosMod != null)
            {
                var caMaIdActual = await _context.Inscripciones
                    .Where(i => i.InId == inid)
                    .Select(i => (int?)i.CaMaId)
                    .FirstOrDefaultAsync();
                if (caMaIdActual == null || !permitidosMod.Contains(caMaIdActual.Value) || !permitidosMod.Contains(inscripciones.CaMaId))
                {
                    return NotFound();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inscripciones);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InscripcionesExists(inscripciones.InId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(GestionInscripcionesMaterias));
            }
            // repopulate selects when returning view on error
            var estudiantes = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
                .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                .ToListAsync();
            ViewData["UsId"] = new SelectList(estudiantes, "UsId", "FullName", inscripciones.UsId);

            var cam = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.CarreraCohorte).ThenInclude(cc => cc!.Cohorte)
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    cm.CaMaId,
                    Display = (cm.CarreraCohorte != null && cm.CarreraCohorte.Carrera != null ? cm.CarreraCohorte.Carrera.CaDenominacion : "")
                        + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "")
                        + (cm.CarreraCohorte != null && cm.CarreraCohorte.Cohorte != null ? $" ({cm.CarreraCohorte.Cohorte.CoAnio})" : "")
                })
                .ToListAsync();
            ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "Display", inscripciones.CaMaId);

            return View(inscripciones);
        }
        //Bind
        private bool InscripcionesExists(int? inid)
        {
            return _context.Inscripciones.Any(e => e.InId == inid);
        }
    }
}

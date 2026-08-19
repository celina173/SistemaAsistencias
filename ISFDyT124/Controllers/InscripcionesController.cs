using ISFDyT124.Data;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ISFDyT124.Controllers
{
    public class InscripcionesController : Controller
    {
        private readonly InstitutoDbContext _context;
        public InscripcionesController(InstitutoDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.Inscripciones.ToListAsync());
        }

        // GET: INSCRIPCIONESS/GestionInscripcionesMaterias or INSCRIPCIONESS/GestionInscripcionesMaterias/5
        public async Task<IActionResult> GestionInscripcionesMaterias(int? inid)
        {
            if (inid == null)
            {
                // No id provided: show the management/list page (the view can render a list or present UI to add/edit)
                var all = await _context.Inscripciones
                    .Include(i => i.Usuarios)
                    .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Carrera)
                    .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Materia)
                    .ToListAsync();

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
                            .Include(x => x.Carrera)
                            .Include(x => x.Materia)
                            .FirstOrDefaultAsync(x => x.CaMaId == ins.CaMaId);
                        ins.CarreraMateria = cm;
                    }   
                }

                return View(all);
            }

            var inscripciones = await _context.Inscripciones
                .Include(i => i.Usuarios)
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Carrera)
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Materia)
                .FirstOrDefaultAsync(m => m.InId == inid);
            if (inscripciones == null)
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

            // populate Carreras and Materias separately for the autocomplete inputs
            var carreras = await _context.Carreras
                .Select(c => new { c.CaId, c.CaDenominacion })
                .ToListAsync();

            var materias = await _context.Materias
                .Select(m => new { m.MaId, m.MaDenominacion })
                .ToListAsync();

            // populate Carreras_Materias using concatenated Carrera - Materia as display text (fallback/reference)
            var cam = await _context.CarrerasMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Select(cm => new { cm.CaMaId, Display = (cm.Carrera != null ? cm.Carrera.CaDenominacion : "") + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "") })
                .ToListAsync();
            ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "Display");

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
                var caMa = await _context.CarrerasMaterias.FirstOrDefaultAsync(cm => cm.CaId == SelectedCaId.Value && cm.MaId == SelectedMaId.Value);
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

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(inscripciones);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(GestionInscripcionesMaterias));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error al guardar la inscripción: " + ex.Message);
                }
            }

            // repopulate selects when returning view on error
            var estudiantes = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
                .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                .ToListAsync();
            ViewData["UsId"] = new SelectList(estudiantes, "UsId", "FullName", inscripciones.UsId);

            var cam = await _context.CarrerasMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Select(cm => new { cm.CaMaId, Display = (cm.Carrera != null ? cm.Carrera.CaDenominacion : "") + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "") })
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
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Carrera)
                .Include(i => i.CarreraMateria).ThenInclude(cm => cm.Materia)
                .FirstOrDefaultAsync(i => i.InId == inid);
            if (inscripciones == null)
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

            var cam = await _context.CarrerasMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Select(cm => new { cm.CaMaId, Display = (cm.Carrera != null ? cm.Carrera.CaDenominacion : "") + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "") })
                .ToListAsync();
            ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "Display", inscripciones.CaMaId);

            return View(inscripciones);
        }

        // POST: INSCRIPCIONESS/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModificarInscripcionMateria(int? inid, [Bind("InId,UsId,CaMaId,Usuarios,Carreras_Materias")] Inscripciones inscripciones)
        {
            if (inid != inscripciones.InId)
            {
                return NotFound();
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

            var cam = await _context.CarrerasMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Select(cm => new { cm.CaMaId, Display = (cm.Carrera != null ? cm.Carrera.CaDenominacion : "") + " - " + (cm.Materia != null ? cm.Materia.MaDenominacion : "") })
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

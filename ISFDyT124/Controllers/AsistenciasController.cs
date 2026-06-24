using ISFDyT124.Data;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//namespace ISFDyT124.Controllers
//{
//    public class AsistenciasController : Controller
//    {
//        private readonly InstitutoDbContext _context;

//        public AsistenciasController(InstitutoDbContext context)
//        {
//            _context = context;
//        }

        // GET: Asistencias
        //public async Task<IActionResult> Index()
        //{
        //    var asistencias = _context.Asistencias
        //        .Include(a => a.Usuario)
        //        .Include(a => a.Materias);
        //    // populate Carreras_Materias select list (CaMaDenominacion)
        //    var cam = await _context.CarreraMaterias
        //        //.Select(cm => new { cm.CaMaId, cm.CaMaDenominacion })
        //        .ToListAsync();
        //    ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "CaMaDenominacion");

        //    return View(await asistencias.ToListAsync());
        //}

        ////GET: Asistencias/Asistencia
        ////Vista estática para toma de asistencia(diseño)
        //public async Task<IActionResult> Asistencia(int? CaMaId)
        //{
        //    var model = new AsistenciaFormViewModel();
        //    if (CaMaId == null)
        //    {
        //        return View(model);
        //    }

        //    model.CaMaId = CaMaId;

        //    // find role 'Estudiante'
        //    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion == "Estudiante");
        //    if (role == null)
        //    {
        //        return View(model);
        //    }

        //    // find users inscribed to this Carreras_Materias via Inscripciones
        //    var estudiantes = await _context.Inscripciones
        //        .Where(i => i.CaMaId == CaMaId)
        //        .Select(i => i.Usuarios)
        //        .Where(u => u != null && u.RoId == role.RoId)
        //        .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
        //        .ToListAsync();

        //    foreach (var s in estudiantes)
        //    {
        //        model.Rows.Add(new AsistenciaRowViewModel { UsId = s.UsId, FullName = s.FullName });
        //    }

        //    var caMaName = await _context.CarrerasMaterias
        //        .Where(cm => cm.CaMaId == CaMaId)
        //        .FirstOrDefaultAsync();
        //    ViewData["CaMaDenominacion"] = caMaName;

        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Asistencia(AsistenciaFormViewModel model)
        //{
        //    if (model.CaMaId == null)
        //    {
        //        ModelState.AddModelError(string.Empty, "Debe seleccionar una carrera/materia.");
        //        return View(model);
        //    }

        //    var carreraMateria = await _context.CarrerasMaterias.FindAsync(model.CaMaId.Value);

        //    foreach (var row in model.Rows)
        //    {
        //        var presente = row.Modulos != null && row.Modulos.Any(x => x);

        //        var entity = new Asistencia
        //        {
        //            AsFecha = DateTime.Now,
        //            AsPresente = presente,
        //            AsJustificacion = row.AsJustificacion,
        //            UsId = row.UsId,
        //            CaMaId = model.CaMaId,
        //        };

        //        _context.Asistencias.Add(entity);
        //    }

        //    await _context.SaveChangesAsync();
        ////    return RedirectToAction(nameof(Index));
        //}

        // GET: Asistencias/AsistenciaGlobal
        // Vista estática para asistencia global (diseño)
        //public IActionResult AsistenciaGlobal()
        //{
        //    return View();
        //}

        //// GET: Asistencias/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var asistencia = await _context.Asistencias
        //        .FirstOrDefaultAsync(m => m.AsId == id);
        //    if (asistencia == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(asistencia);
        //}

//        // GET: Asistencias/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: Asistencias/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("AsId,AsFecha,AsPresente,AsJustificacion,UsId,MaId")] Asistencia asistencia)
//        {
//            // -LÓGICA DE NEGOCIO: CREACIÓN -

//            //1: Automatización de fecha 
//            // Si el usuario no eligió fecha, el sistema pone la fecha actual automáticamente
//            if (asistencia.AsFecha == null)
//            {
//                asistencia.AsFecha = DateTime.Now;
//            }

//            // 2. Validación: Fecha no futura, No permitimos registros en fechas futuras
//            if (asistencia.AsFecha > DateTime.Now)
//            {
//                ModelState.AddModelError("AsFecha", "No puedes registrar fechas futuras.");
//            }

//            // 3. Lógica: Si marca 'Presente', la justificación debe ser false obligatoriamente.
//            if (asistencia.AsPresente)
//            {
//                asistencia.AsJustificacion = false;

//            } // 4. Aplicamos flexibilidad en ausencias: Si marca 'Ausente' (AsPresente = false), 
//              // el sistema permite al usuario decidir libremente si el estudiante justifica o no.

//            else
//            {
//                // No aplicamos restricciones adicionales, se respeta la decisión del usuario.
//            }

//            if (ModelState.IsValid)
//            {
//                _context.Add(asistencia);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }

//            return View(asistencia);
//        }

//        //// GET: Asistencias/Edit/5
//        //public async Task<IActionResult> Edit(int? id)
//        //{
//        //    if (id == null)
//        //    {
//        //        return NotFound();
//        //    }

//        //    var asistencia = await _context.Asistencias.FindAsync(id);
//        //    if (asistencia == null)
//        //    {
//        //        return NotFound();
//        //    }
//        //    return View(asistencia);
//        //}

//        // POST: Asistencias/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, [Bind("AsId,AsFecha,AsPresente,AsJustificacion,UsId,MaId")] Asistencia asistencia)
//        {
//            if (id != asistencia.AsId)
//            {
//                return NotFound();
//            }

//            // -LÓGICA DE NEGOCIO: EDICIÓN -

//            // 1. Automatización: Si por error se borra la fecha, la restauramos al momento actual
//            if (asistencia.AsFecha == null) { asistencia.AsFecha = DateTime.Now; }

//            // 2. Validación: Mantenemos la regla de no permitir fechas futuras
//            if (asistencia.AsFecha > DateTime.Now)
//            {
//                ModelState.AddModelError("AsFecha", "No puedes registrar fechas futuras.");
//            }

//            // 3. Limpieza de datos: Si se marca 'Presente' durante la edición, limpiamos la justificación.
//            if (asistencia.AsPresente)
//            {
//                asistencia.AsJustificacion = false;
//            }

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(asistencia);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!AsistenciaExists(asistencia.AsId))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            return View(asistencia);
//        }

//        //(Delete y AsistenciaExists se mantienen iguales)

//        // GET: Asistencias/Delete/5
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var asistencia = await _context.Asistencias
//                .FirstOrDefaultAsync(m => m.AsId == id);
//            if (asistencia == null)
//            {
//                return NotFound();
//            }

//            return View(asistencia);
//        }

//        // POST: Asistencias/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var asistencia = await _context.Asistencias.FindAsync(id);
//            if (asistencia != null)
//            {
//                _context.Asistencias.Remove(asistencia);
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool AsistenciaExists(int id)
//        {
//            return _context.Asistencias.Any(e => e.AsId == id);
//        }
//    }
//}
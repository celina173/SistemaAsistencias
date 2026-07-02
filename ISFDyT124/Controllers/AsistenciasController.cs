using ISFDyT124.Data;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISFDyT124.Data;

namespace ISFDyT124.Controllers
{
    public class AsistenciasController : Controller
    {
        private readonly InstitutoDbContext _context;

        public AsistenciasController(InstitutoDbContext context)
        {
            _context = context;
        }

        // GET: Asistencias
        public async Task<IActionResult> Index()
        {
            var asistencias = _context.Asistencias
                .Include(a => a.Usuario)
                .Include(a => a.Materias);
            // populate Carreras_Materias select list (CaMaDenominacion)
            var cam = await _context.CarrerasMaterias
                //.Select(cm => new { cm.CaMaId, cm.CaMaDenominacion })
                .ToListAsync();
            ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "CaMaDenominacion");

            return View(await asistencias.ToListAsync());
        }

        //GET: Asistencias/Asistencia
        //Vista estática para toma de asistencia(diseño)
        public async Task<IActionResult> Asistencia(int? CaMaId)
        {
            var model = new AsistenciaFormViewModel();
            if (CaMaId == null)
            {
                return View(model);
            }

            model.CaMaId = CaMaId;

            // find role 'Estudiante'
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion == "Estudiante");
            if (role == null)
            {
                return View(model);
            }

            // find users inscribed to this Carreras_Materias via Inscripciones
            var estudiantes = await _context.Inscripciones
                .Where(i => i.CaMaId == CaMaId)
                .Select(i => i.Usuarios)
                .Where(u => u != null && u.RoId == role.RoId)
                .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                .ToListAsync();

            foreach (var s in estudiantes)
            {
                model.Rows.Add(new AsistenciaRowViewModel { UsId = s.UsId, FullName = s.FullName });
            }

            var caMaName = await _context.CarrerasMaterias
                .Where(cm => cm.CaMaId == CaMaId)
                .FirstOrDefaultAsync();
            ViewData["CaMaDenominacion"] = caMaName;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asistencia(AsistenciaFormViewModel model)
        {
            if (model.CaMaId == null)
            {
                ModelState.AddModelError(string.Empty, "Debe seleccionar una carrera/materia.");
                return View(model);
            }

            var carreraMateria = await _context.CarrerasMaterias.FindAsync(model.CaMaId.Value);

            foreach (var row in model.Rows)
            {
                var presente = row.Modulos != null && row.Modulos.Any(x => x);

                var entity = new Asistencia
                {
                    AsFecha = DateTime.Now,
                    AsPresente = presente,
                    AsJustificacion = row.AsJustificacion,
                    UsId = row.UsId,
                };

                _context.Asistencias.Add(entity);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Asistencias/AsistenciaGlobal
        // Vista estática para asistencia global (diseño)
        public IActionResult AsistenciaGlobal()
        {
            return View();
        }

        private bool AsistenciaExists(int id)
        {
            return _context.Asistencias.Any(e => e.AsId == id);
        }
    }
}
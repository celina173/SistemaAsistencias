using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ISFDyT124.Models.AsistenciaGlobalViewModel;

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
        public async Task<IActionResult> Index(int? selectedCarreraId, int? selectedMateriaId)
        {
            // Support alternate input names coming from the view/form (e.g. SelectedCarreraId/SelectedMateriaId)
            if (!selectedCarreraId.HasValue)
            {
                var s = (Request.HasFormContentType ? Request.Form["SelectedCarreraId"].FirstOrDefault() : null) ?? Request.Query["SelectedCarreraId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(s) && int.TryParse(s, out var v1)) selectedCarreraId = v1;
            }
            if (!selectedMateriaId.HasValue)
            {
                var s2 = (Request.HasFormContentType ? Request.Form["SelectedMateriaId"].FirstOrDefault() : null) ?? Request.Query["SelectedMateriaId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(s2) && int.TryParse(s2, out var v2)) selectedMateriaId = v2;
            }
            var carreras = await _context.Carreras
                .Select(c => new CarreraDetalleDto
                {
                    CaId = c.CaId,
                    CaDenominacion = c.CaDenominacion,
                    CarreraMateriasCount = c.CarreraMaterias != null ? c.CarreraMaterias.Count() : 0,
                    CarreraCohortesCount = c.CarreraCohortes != null ? c.CarreraCohortes.Count() : 0,
                })
                .ToListAsync();

            var materias = await _context.Materias
                .Select(m => new MateriaDetalleDto
                {
                    MaId = m.MaId,
                    MaDenominacion = m.MaDenominacion,
                    MaModalidad = m.MaModalidad,
                    MaCantModulos = m.MaCantModulos,
                    CarreraMateriasCount = m.CarreraMaterias != null ? m.CarreraMaterias.Count() : 0
                })
                .ToListAsync();

            var modelDto = new HomeIndexDto
            {
                Carreras = carreras,
                Materias = materias,
                SelectedCarreraId = selectedCarreraId,
                SelectedMateriaId = selectedMateriaId
            };

            // If both Carrera and Materia were selected, resolve the corresponding CaMaId
            if (selectedCarreraId.HasValue && selectedMateriaId.HasValue)
            {
                var caMa = await _context.CarrerasMaterias
                    .FirstOrDefaultAsync(cm => cm.CaId == selectedCarreraId.Value && cm.MaId == selectedMateriaId.Value);

                if (caMa != null)
                {
                    // if query contains _global=1, redirect to AsistenciaGlobal, otherwise to Asistencia
                    var isGlobal = Request.Query.ContainsKey("_global") && Request.Query["_global"].ToString() == "1";
                    if (isGlobal)
                    {
                        return RedirectToAction(nameof(AsistenciaGlobal), new { CaMaId = caMa.CaMaId });
                    }
                    return RedirectToAction(nameof(Asistencia), new { CaMaId = caMa.CaMaId });
                }

                // if no matching CarreraMateria found, add model error and show index with message
                ModelState.AddModelError(string.Empty, "No existe una relación Carrera-Materia para la selección realizada.");
            }

            return View(modelDto);
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

            // Load carrera/materia display for header
            var caMaInfo = await _context.CarrerasMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .FirstOrDefaultAsync(cm => cm.CaMaId == CaMaId);

            if (caMaInfo != null)
            {
                var carreraText = caMaInfo.Carrera != null ? (caMaInfo.Carrera.CaDenominacion ?? string.Empty) : string.Empty;
                var materiaText = caMaInfo.Materia != null ? (caMaInfo.Materia.MaDenominacion ?? string.Empty) : string.Empty;
                ViewData["CaMaDenominacion"] = string.IsNullOrWhiteSpace(carreraText) ? materiaText : carreraText + " - " + materiaText;
            }
            else
            {
                ViewData["CaMaDenominacion"] = string.Empty;
            }

            ViewData["AsistenciaFecha"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // find role 'Estudiante' (case-insensitive)
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion.ToLower() == "estudiante");
            if (role == null)
            {
                // no role configured; return empty model
                return View(model);
            }

            // find users inscribed to this Carreras_Materias via Inscripciones — ensure we load Usuario and its Rol
            var estudiantes = await (from i in _context.Inscripciones
                                     join u in _context.Usuarios on i.UsId equals u.UsId
                                     where i.CaMaId == CaMaId && u.RoId == role.RoId
                                     select new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                                    .ToListAsync();

            // determine number of modules for this materia
            int maCantModulos = 1; // default
            var caMa = await _context.CarrerasMaterias.FirstOrDefaultAsync(cm => cm.CaMaId == CaMaId);
            if (caMa != null)
            {
                var materia = await _context.Materias.FindAsync(caMa.MaId);
                if (materia != null)
                {
                    if (materia.MaCantModulos.HasValue && materia.MaCantModulos.Value > 0)
                    {
                        maCantModulos = materia.MaCantModulos.Value;
                    }
                    else
                    {
                        maCantModulos = 1;
                    }
                }
            }

            foreach (var s in estudiantes)
            {
                var row = new AsistenciaRowViewModel { UsId = s.UsId, FullName = s.FullName };
                // initialize Modulos list according to MaCantModulos
                row.Modulos = Enumerable.Range(0, maCantModulos).Select(_ => "false").ToList();
                model.Rows.Add(row);
            }

            ViewData["MaCantModulos"] = maCantModulos;
            model.ModuleCount = maCantModulos;

            var caMaName = await _context.CarrerasMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Where(cm => cm.CaMaId == CaMaId)
                .FirstOrDefaultAsync();

            // prepare a friendly display string for the selected carrera/materia
            if (caMaName != null)
            {
                var carreraText = caMaName.Carrera != null ? (caMaName.Carrera.CaDenominacion ?? "") : "";
                var materiaText = caMaName.Materia != null ? (caMaName.Materia.MaDenominacion ?? "") : "";
                ViewData["CaMaDenominacion"] = string.IsNullOrWhiteSpace(carreraText) ? materiaText : carreraText + " - " + materiaText;
            }
            else
            {
                ViewData["CaMaDenominacion"] = string.Empty;
            }

            ViewData["AsistenciaFecha"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

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
            int moduleCount = model.ModuleCount > 0 ? model.ModuleCount : 1;

            foreach (var row in model.Rows)
            {
                for (int i = 0; i < moduleCount; i++)
                {
                    bool moduloPresente = row.Modulos != null
                                          && i < row.Modulos.Count
                                          && row.Modulos[i] == "true";

                    var entity = new Asistencia
                    {
                        AsFecha = DateTime.Now,
                        AsPresente = moduloPresente,
                        AsJustificacion = row.AsJustificacion,
                        UsId = row.UsId,
                        CaMaId = model.CaMaId,
                    };

                    _context.Asistencias.Add(entity);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Asistencias/AsistenciaGlobal
        // Muestra el histórico de asistencias por materia (filtrado por CaMaId)
        public async Task<IActionResult> AsistenciaGlobal(int? CaMaId)
        {
            var model = new AsistenciaGlobalViewModel();

            if (CaMaId == null)
            {
                return View(model);
            }

            model.CaMaId = CaMaId;

            // Buscar el rol Estudiante
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoDenominacion == "Estudiante");

            if (role == null)
            {
                return View(model);
            }

            // Buscar los alumnos inscriptos en esa materia
            var estudiantes = await (from i in _context.Inscripciones
                                     join u in _context.Usuarios on i.UsId equals u.UsId
                                     where i.CaMaId == CaMaId && u.RoId == role.RoId
                                     select u)
                        .ToListAsync();

            var usIdsInscritos = estudiantes
                .Select(u => u.UsId)
                .ToList();

            // Traer las asistencias de esos alumnos
            var todasLasAsistencias = await _context.Asistencias
                .Where(a => a.UsId.HasValue && usIdsInscritos.Contains(a.UsId.Value))
                .ToListAsync();

            // Fechas distintas en que hubo clase
            model.Fechas = todasLasAsistencias
                .Where(a => a.AsFecha.HasValue)
                .Select(a => a.AsFecha.Value.Date)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            // Armar una fila por alumno
            foreach (var alumno in estudiantes)
            {
                var asistenciasAlumno = todasLasAsistencias
                    .Where(a => a.UsId == alumno.UsId)
                    .ToList();

                // Porcentaje por día (módulos presentes / total módulos ese día)
                var asistenciaPorFecha = model.Fechas
                    .ToDictionary(
                        fecha => fecha,
                        fecha =>
                        {
                            var modulosDia = asistenciasAlumno
                                .Where(a => a.AsFecha.HasValue && a.AsFecha.Value.Date == fecha)
                                .ToList();
                            if (!modulosDia.Any()) return 0m;
                            return Math.Round((decimal)modulosDia.Count(a => a.AsPresente) / modulosDia.Count * 100, 1);
                        }
                    );

                // Porcentaje global (módulos presentes / total módulos de todas las clases)
                int presentes = asistenciasAlumno.Count(a => a.AsPresente);
                int total = asistenciasAlumno.Count;
                decimal porcentaje = total > 0 ? Math.Round((decimal)presentes / total * 100, 1) : 0;

                model.Rows.Add(new AsistenciaGlobalRowViewModel
                {
                    UsId = alumno.UsId,
                    FullName = $"{alumno.UsApellido} {alumno.UsNombre}",
                    AsistenciaPorFecha = asistenciaPorFecha,
                    PorcentajeAsistencia = porcentaje
                });
            }

            return View(model);
        }

        private bool AsistenciaExists(int id)
        {
            return _context.Asistencias.Any(e => e.AsId == id);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static ISFDyT124.Models.AsistenciaGlobalViewModel;

namespace ISFDyT124.Controllers
{
    [Authorize(Roles = "Admin,Dirección,Docente")]
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
                var s =
                    (
                        Request.HasFormContentType
                            ? Request.Form["SelectedCarreraId"].FirstOrDefault()
                            : null
                    ) ?? Request.Query["SelectedCarreraId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(s) && int.TryParse(s, out var v1))
                    selectedCarreraId = v1;
            }
            if (!selectedMateriaId.HasValue)
            {
                var s2 =
                    (
                        Request.HasFormContentType
                            ? Request.Form["SelectedMateriaId"].FirstOrDefault()
                            : null
                    ) ?? Request.Query["SelectedMateriaId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(s2) && int.TryParse(s2, out var v2))
                    selectedMateriaId = v2;
            }
            var carreras = await _context
                .Carreras.Select(c => new CarreraDetalleDto
                {
                    CaId = c.CaId,
                    CaDenominacion = c.CaDenominacion,
                    // El "?? new List<>()" que había acá antes rompía la traducción a SQL:
                    // EF Core no sabe traducir un null-coalesce sobre una navegación dentro de
                    // una consulta. El "!" es solo una marca para el compilador (no hace nada en
                    // tiempo de ejecución), así que no afecta la traducción — una navegación null
                    // simplemente no aporta filas al SelectMany.
                    CarreraMateriasCount =
                        c.CarreraCohortes != null
                            ? c.CarreraCohortes.SelectMany(cc => cc.CarreraMaterias!).Count()
                            : 0,
                    CarreraCohortesCount =
                        c.CarreraCohortes != null ? c.CarreraCohortes.Count() : 0,
                })
                .ToListAsync();

            var materias = await _context
                .Materias.Select(m => new MateriaDetalleDto
                {
                    MaId = m.MaId,
                    MaDenominacion = m.MaDenominacion,
                    MaModalidad = m.MaModalidad,
                    MaCantModulos = m.MaCantModulos,
                    CarreraMateriasCount =
                        m.CarreraMaterias != null ? m.CarreraMaterias.Count() : 0,
                })
                .ToListAsync();

            var modelDto = new HomeIndexDto
            {
                Carreras = carreras,
                Materias = materias,
                SelectedCarreraId = selectedCarreraId,
                SelectedMateriaId = selectedMateriaId,
            };

            // If both Carrera and Materia were selected, resolve the corresponding CaMaId
            if (selectedCarreraId.HasValue && selectedMateriaId.HasValue)
            {
                var caMa = await _context.CarreraMaterias.FirstOrDefaultAsync(cm =>
                    cm.CarreraCohorte != null
                    && cm.CarreraCohorte.CaId == selectedCarreraId.Value
                    && cm.MaId == selectedMateriaId.Value
                );

                if (caMa != null)
                {
                    // if query contains _global=1, redirect to AsistenciaGlobal, otherwise to Asistencia
                    var isGlobal =
                        Request.Query.ContainsKey("_global")
                        && Request.Query["_global"].ToString() == "1";
                    if (isGlobal)
                    {
                        return RedirectToAction(
                            nameof(AsistenciaGlobal),
                            new { CaMaId = caMa.CaMaId }
                        );
                    }
                    return RedirectToAction(nameof(Asistencia), new { CaMaId = caMa.CaMaId });
                }

                // if no matching CarreraMateria found, add model error and show index with message
                ModelState.AddModelError(
                    string.Empty,
                    "No existe una relación Carrera-Materia para la selección realizada."
                );
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

            // Alumnos inscriptos a esta cátedra vía Inscripciones (la inscripción ya es la prueba
            // de que corresponde tomarle asistencia acá, sin depender de un nombre de rol puntual).
            var estudiantes = await (
                from i in _context.Inscripciones
                join u in _context.Usuarios on i.UsId equals u.UsId
                where i.CaMaId == CaMaId
                select new
                {
                    u.UsId,
                    FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim(),
                }
            ).ToListAsync();

            // Carrera y materia reales de esta cátedra (antes se guardaba el objeto CarreraMateria
            // entero en ViewData, lo que mostraba "ISFDyT124.Models.CarreraMateria" en pantalla).
            var caMa = await _context
                .CarreraMaterias.Include(cm => cm.CarreraCohorte)
                .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.Materia)
                .FirstOrDefaultAsync(cm => cm.CaMaId == CaMaId);

            int maCantModulos = 1; // default
            if (caMa != null)
            {
                ViewBag.CarreraNombre = caMa.CarreraCohorte?.Carrera?.CaDenominacion ?? "Carrera";
                ViewBag.MateriaNombre = caMa.Materia?.MaDenominacion ?? "Materia";

                if (caMa.Materia?.MaCantModulos is int cant && cant > 0)
                {
                    maCantModulos = cant;
                }
            }

            foreach (var s in estudiantes)
            {
                var row = new AsistenciaRowViewModel { UsId = s.UsId, FullName = s.FullName };
                // initialize Modulos list according to MaCantModulos
                row.Modulos = Enumerable.Range(0, maCantModulos).Select(_ => false).ToList();
                model.Rows.Add(row);
            }

            ViewData["MaCantModulos"] = maCantModulos;
            model.ModuleCount = maCantModulos;

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

            var carreraMateria = await _context.CarreraMaterias.FindAsync(model.CaMaId.Value);

            int moduleCount = model.ModuleCount > 0 ? model.ModuleCount : 1;
            foreach (var row in model.Rows)
            {
                var checkedCount = row.Modulos != null ? row.Modulos.Count(x => x) : 0;
                var presente = checkedCount > 0;
                decimal porcentaje = 0;
                if (moduleCount > 0)
                {
                    porcentaje = Math.Round((decimal)checkedCount / moduleCount * 100, 1);
                }

                var entity = new Asistencia
                {
                    AsFecha = DateTime.Now,
                    AsPresente = presente,
                    AsJustificacion = row.AsJustificacion,
                    UsId = row.UsId,
                    CaMaId = model.CaMaId,
                };

                _context.Asistencias.Add(entity);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Las asistencias han sido guardadas correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Asistencias/AsistenciaGlobal
        // Muestra el histórico de asistencias por materia (filtrado por CaMaId)
        public async Task<IActionResult> AsistenciaGlobal(int? CaMaId)
        {
            // Verificamos si es Admin/Directivo
            if (User.IsInRole("Admin") || User.IsInRole("Dirección"))
            {
                // Traemos los datos separados para poder armar la cascada en la vista
                ViewBag.TodasLasCatedras = await _context
                    .CarreraMaterias.Where(cm => cm.CarreraCohorte != null)
                    .Select(cm => new
                    {
                        CaMaId = cm.CaMaId,
                        CaId = cm.CarreraCohorte!.CaId,
                        Carrera = cm.CarreraCohorte.Carrera!.CaDenominacion,
                        Materia = cm.Materia!.MaDenominacion
                    })
                    .ToListAsync();
            }

            var model = new AsistenciaGlobalViewModel();

            if (CaMaId == null)
            {
                return View(model); // Retorna la vista vacía si el Admin no eligió nada aún
            }

            model.CaMaId = CaMaId;

            // NUEVO: Buscamos los nombres reales de la Carrera y Materia
            var infoCatedra = await _context
                .CarreraMaterias.Where(cm => cm.CaMaId == CaMaId)
                .Select(cm => new
                {
                    Carrera = cm.CarreraCohorte != null ? cm.CarreraCohorte.Carrera!.CaDenominacion : null,
                    Materia = cm.Materia!.MaDenominacion
                })
                .FirstOrDefaultAsync();

            if (infoCatedra != null)
            {
                ViewBag.CarreraNombre = infoCatedra.Carrera;
                ViewBag.MateriaNombre = infoCatedra.Materia;
            }

            // Alumnos inscriptos en esta materia vía Inscripciones (ver Asistencia() más arriba)
            var estudiantes = await (
                from i in _context.Inscripciones
                join u in _context.Usuarios on i.UsId equals u.UsId
                where i.CaMaId == CaMaId
                select u
            )
                .Distinct()
                .ToListAsync();

            var usIdsInscritos = estudiantes.Select(u => u.UsId).ToList();

            // Traer asistencias relacionadas a esta materia y a esos alumnos.
            // Algunos registros históricos pueden no tener CaMaId (se guardaron antes de asignarlo).
            // Para mostrar el histórico como primario, incluimos también registros con CaMaId NULL
            // siempre que pertenezcan a alumnos inscriptos en esta materia.
            var todasLasAsistencias = await _context
                .Asistencias.Where(a =>
                    a.UsId.HasValue
                    && usIdsInscritos.Contains(a.UsId.Value)
                    && (a.CaMaId == CaMaId || a.CaMaId == null)
                )
                .ToListAsync();

            // Columnas (fechas)
            model.Fechas = todasLasAsistencias
                .Where(a => a.AsFecha.HasValue)
                .Select(a => a.AsFecha.Value.Date)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            // Armar filas por alumno usando AsPorcentaje si existe, sino AsPresente como 100/0
            foreach (var alumno in estudiantes)
            {
                var asistenciasAlumno = todasLasAsistencias
                    .Where(a => a.UsId == alumno.UsId)
                    .ToList();

                var asistenciaPorFecha = new Dictionary<DateTime, decimal>();
                decimal sumaPorcentajes = 0m;
                foreach (var fecha in model.Fechas)
                {
                    var registro = asistenciasAlumno.FirstOrDefault(a =>
                        a.AsFecha.HasValue && a.AsFecha.Value.Date == fecha
                    );
                    decimal pct = 0m;
                    if (registro != null)
                    {
                        // Si existe AsPorcentaje en el registro, usarlo; si no, fallback a AsPresente (100/0)
                        var prop = registro.GetType().GetProperty("AsPorcentaje");
                        if (prop != null)
                        {
                            var val = prop.GetValue(registro);
                            if (val is decimal d)
                                pct = d;
                            else if (val is decimal?)
                                pct = ((decimal?)val) ?? 0m;
                        }
                        else
                        {
                            pct = registro.AsPresente ? 100m : 0m;
                        }
                    }
                    asistenciaPorFecha[fecha] = pct;
                    sumaPorcentajes += pct;
                }

                int totalFechas = model.Fechas.Count;
                decimal promedio =
                    totalFechas > 0 ? Math.Round(sumaPorcentajes / totalFechas, 1) : 0m;

                model.Rows.Add(
                    new AsistenciaGlobalRowViewModel
                    {
                        UsId = alumno.UsId,
                        FullName = $"{alumno.UsApellido} {alumno.UsNombre}",
                        AsistenciaPorFecha = asistenciaPorFecha,
                        PorcentajeAsistencia = promedio,
                    }
                );
            }

            return View(model);
        }

        private bool AsistenciaExists(int id)
        {
            return _context.Asistencias.Any(e => e.AsId == id);
        }
    }
}

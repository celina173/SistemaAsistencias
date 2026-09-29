using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        /// <summary>
        /// IDs de CarreraMateria (cátedra puntual) que el usuario actual tiene permitido
        /// ver/tomar asistencia. null = sin restricción (Admin/Dirección). Lista = solo las
        /// cátedras propias del Docente de la cohorte del año en curso -- antes un Docente
        /// veía y podía elegir CUALQUIER carrera/materia del instituto en esta pantalla,
        /// igual que ya se corrigió en Home/Index (mismo bug, pantalla distinta).
        /// </summary>
        private async Task<List<int>?> CaMaIdsPermitidosAsync()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Dirección"))
                return null;

            var docenteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(docenteIdClaim, out int docenteId))
                return new List<int>();

            int anioActual = DateTime.Today.Year;

            return await _context
                .Usuarios.Where(u => u.UsId == docenteId)
                .SelectMany(u => u.CarreraMaterias)
                .Where(cm =>
                    cm.CarreraCohorte != null
                    && cm.CarreraCohorte.Cohorte != null
                    && cm.CarreraCohorte.Cohorte.CoAnio == anioActual
                )
                .Select(cm => cm.CaMaId)
                .Distinct()
                .ToListAsync();
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

            // Un Docente solo debe ver las carreras/materias de sus propias cátedras
            // asignadas -- antes se listaban TODAS sin importar el rol (mismo bug que ya
            // se había corregido en Home/Index, pero esta pantalla se había quedado sin
            // el filtro). Admin/Dirección siguen viendo el listado completo.
            var permitidos = await CaMaIdsPermitidosAsync();
            List<int>? caIdsPermitidos = null;
            List<int>? maIdsPermitidos = null;
            if (permitidos != null)
            {
                var camPermitidas = await _context
                    .CarreraMaterias.Where(cm => permitidos.Contains(cm.CaMaId))
                    .Include(cm => cm.CarreraCohorte)
                    .ToListAsync();
                caIdsPermitidos = camPermitidas
                    .Where(cm => cm.CarreraCohorte != null)
                    .Select(cm => cm.CarreraCohorte!.CaId)
                    .Distinct()
                    .ToList();
                maIdsPermitidos = camPermitidas.Select(cm => cm.MaId).Distinct().ToList();
            }

            var carrerasQuery = _context.Carreras.AsQueryable();
            if (caIdsPermitidos != null)
                carrerasQuery = carrerasQuery.Where(c => caIdsPermitidos.Contains(c.CaId));

            var carreras = await carrerasQuery
                .Select(c => new CarreraDetalleDto
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

            var materiasQuery = _context.Materias.AsQueryable();
            if (maIdsPermitidos != null)
                materiasQuery = materiasQuery.Where(m => maIdsPermitidos.Contains(m.MaId));

            var materias = await materiasQuery
                .Select(m => new MateriaDetalleDto
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
                    // Defensa en profundidad: aunque el listado ya viene acotado a las
                    // cátedras propias, un Docente podría armar la URL a mano con un
                    // CaId/MaId que no es suyo -- se rechaza igual acá.
                    if (permitidos != null && !permitidos.Contains(caMa.CaMaId))
                    {
                        ModelState.AddModelError(string.Empty, "No tenés esa cátedra asignada.");
                        return View(modelDto);
                    }

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

            // Un Docente no puede ver/tomar asistencia de una cátedra que no es suya,
            // aunque cambie el CaMaId a mano en la URL (mismo chequeo que ya tiene
            // ProfesorController.Asistencia).
            var permitidos = await CaMaIdsPermitidosAsync();
            if (permitidos != null && !permitidos.Contains(CaMaId.Value))
            {
                return NotFound();
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
                // Necesario en la vista para armar la fila de la cola offline (ticket 8.2):
                // OfflineAsistencia.encolarAsistencia espera el MaId de la materia, igual
                // que en la pantalla del Docente.
                ViewBag.MateriaId = caMa.MaId;

                if (caMa.Materia?.MaCantModulos is int cant && cant > 0)
                {
                    maCantModulos = cant;
                }
            }

            // Asistencia ya cargada HOY para esta cátedra (para precargar/editar). Antes esto
            // no se consultaba: reabrir la pantalla el mismo día reseteaba TODOS los checkboxes
            // a "ausente", y si el Admin guardaba de nuevo sin marcar a nadie (por ejemplo solo
            // para agregar un alumno tarde) pisaba en silencio la asistencia ya guardada de todo
            // el curso, marcando a todos como ausentes. El modelo solo guarda el resultado
            // agregado por alumno (AsPresente/AsJustificacion), no qué módulo puntual se tildó,
            // así que la reconstrucción es best-effort: si ya estaba presente, se precargan
            // todos los módulos tildados (mismo % que había, y guardar sin tocar nada da el
            // mismo resultado); si estaba ausente, quedan destildados. Se agrupa por alumno
            // (no ToDictionary directo) por el mismo motivo que ProfesorController.Asistencia:
            // si quedaron dos filas del mismo alumno/fecha, se toma la más reciente.
            var hoy = DateTime.Today;
            var existentesPorAlumno = (
                await _context
                    .Asistencias.Where(a =>
                        a.CaMaId == CaMaId && a.AsFecha != null && a.AsFecha.Value.Date == hoy
                    )
                    .ToListAsync()
            )
                .GroupBy(a => a.UsId ?? 0)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AsId).First());

            foreach (var s in estudiantes)
            {
                var row = new AsistenciaRowViewModel { UsId = s.UsId, FullName = s.FullName };
                if (existentesPorAlumno.TryGetValue(s.UsId, out var previa))
                {
                    row.Modulos = Enumerable
                        .Range(0, maCantModulos)
                        .Select(_ => previa.AsPresente)
                        .ToList();
                    row.AsJustificacion = previa.AsJustificacion;
                }
                else
                {
                    row.Modulos = Enumerable.Range(0, maCantModulos).Select(_ => false).ToList();
                }
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

            // Un Docente no puede guardar asistencia de una cátedra que no es suya, ni
            // aunque arme el POST a mano con un CaMaId ajeno.
            var permitidosPost = await CaMaIdsPermitidosAsync();
            if (permitidosPost != null && !permitidosPost.Contains(model.CaMaId.Value))
            {
                return NotFound();
            }

            var carreraMateria = await _context.CarreraMaterias.FindAsync(model.CaMaId.Value);
            var hoy = DateTime.Today;

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

                // Upsert por CaMaId+alumno+fecha (mismo criterio que ya usa
                // ProfesorController.Asistencia): sin esto, guardar dos veces el mismo día
                // no actualizaba lo ya cargado, insertaba una fila nueva cada vez — la
                // asistencia quedaba duplicada y los reportes contaban de más (ticket 5.12,
                // relacionado con QA-06).
                var existente = await _context.Asistencias.FirstOrDefaultAsync(a =>
                    a.UsId == row.UsId
                    && a.CaMaId == model.CaMaId
                    && a.AsFecha != null
                    && a.AsFecha.Value.Date == hoy
                );

                if (existente != null)
                {
                    existente.AsPresente = presente;
                    existente.AsJustificacion = row.AsJustificacion;
                    _context.Update(existente);
                }
                else
                {
                    _context.Asistencias.Add(
                        new Asistencia
                        {
                            AsFecha = hoy,
                            AsPresente = presente,
                            AsJustificacion = row.AsJustificacion,
                            UsId = row.UsId,
                            MaId = carreraMateria?.MaId,
                            CaMaId = model.CaMaId,
                        }
                    );
                }
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

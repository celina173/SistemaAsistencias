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
    using System.Globalization;
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
        /// ver/tomar asistencia. null = sin restricción (Admin/Dirección). Lista = las
        /// cátedras propias del Docente -- antes un Docente veía y podía elegir CUALQUIER
        /// carrera/materia del instituto en esta pantalla, igual que ya se corrigió en
        /// Home/Index (mismo bug, pantalla distinta).
        /// No se filtra por año de cohorte: la cohorte de una cátedra puede representar el
        /// año de ingreso de esa camada (ej. una materia de 2do año de la carrera queda
        /// atada a la cohorte del año anterior), así que una cátedra de un año pasado sigue
        /// siendo una cátedra vigente que el docente tiene que poder usar.
        /// </summary>
        private async Task<List<int>?> CaMaIdsPermitidosAsync()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Dirección"))
                return null;

            var docenteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(docenteIdClaim, out int docenteId))
                return new List<int>();

            return await _context
                .Usuarios.Where(u => u.UsId == docenteId)
                .SelectMany(u => u.CarreraMaterias)
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
                return View(model);

            var permitidos = await CaMaIdsPermitidosAsync();

            if (permitidos != null && !permitidos.Contains(CaMaId.Value))
                return NotFound();

            var caMa = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.Materia)
                .Include(cm => cm.DiasCursada)
                .FirstOrDefaultAsync(cm => cm.CaMaId == CaMaId.Value);

            if (caMa == null)
                return NotFound();

            model.CaMaId = CaMaId.Value;

            ViewBag.CarreraNombre =
                caMa.CarreraCohorte?.Carrera?.CaDenominacion ?? "Carrera";

            ViewBag.MateriaNombre =
                caMa.Materia?.MaDenominacion ?? "Materia";

            ViewBag.MateriaId = caMa.MaId;

            var moduleCount = caMa.Materia?.MaCantModulos is int cantidad
                && cantidad > 0
                ? cantidad
                : 1;

            model.ModuleCount = moduleCount;
            ViewData["MaCantModulos"] = moduleCount;

            var fechasPendientes = await ObtenerFechasProgramadasAsync(
                caMa,
                soloPendientes: true);

            if (fechasPendientes.Count > 0)
                model.FechaAsistencia = fechasPendientes[0];

            PrepararFechasPendientes(model, fechasPendientes);

            var estudiantes = await (
                from i in _context.Inscripciones
                join u in _context.Usuarios on i.UsId equals u.UsId
                where i.CaMaId == CaMaId.Value && u.UsActivo
                select new
                {
                    u.UsId,
                    FullName = ((u.UsApellido ?? "") + " "
                        + (u.UsNombre ?? "")).Trim()
                }
            ).ToListAsync();

            foreach (var estudiante in estudiantes)
            {
                model.Rows.Add(new AsistenciaRowViewModel
                {
                    UsId = estudiante.UsId,
                    FullName = estudiante.FullName,
                    Modulos = Enumerable
                        .Range(0, moduleCount)
                        .Select(_ => false)
                        .ToList()
                });
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asistencia(AsistenciaFormViewModel model)
        {
            if (model.CaMaId == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debe seleccionar una carrera/materia.");

                return View(model);
            }

            var permitidos = await CaMaIdsPermitidosAsync();

            if (permitidos != null &&
                !permitidos.Contains(model.CaMaId.Value))
            {
                return NotFound();
            }

            var caMa = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.Materia)
                .Include(cm => cm.DiasCursada)
                .FirstOrDefaultAsync(cm => cm.CaMaId == model.CaMaId.Value);

            if (caMa == null)
                return NotFound();

            var fechasPendientes = await ObtenerFechasProgramadasAsync(
                caMa,
                soloPendientes: true);

            var fechaSeleccionada = model.FechaAsistencia?.Date;

            if (!fechaSeleccionada.HasValue ||
                !fechasPendientes.Contains(fechaSeleccionada.Value))
            {
                if (fechasPendientes.Count == 0)
                {
                    TempData["OperationErrorMessage"] =
                        "No se pudo registrar la asistencia porque no hay días disponibles.";
                }
                else
                {
                    TempData["OperationErrorMessage"] =
                        "No se pudo registrar la asistencia. Seleccioná una fecha disponible e intentá nuevamente.";
                }

                PrepararFechasPendientes(model, fechasPendientes);

                ViewBag.CarreraNombre =
                    caMa.CarreraCohorte?.Carrera?.CaDenominacion ?? "Carrera";

                ViewBag.MateriaNombre =
                    caMa.Materia?.MaDenominacion ?? "Materia";

                ViewBag.MateriaId = caMa.MaId;

                var cantidadModulos = caMa.Materia?.MaCantModulos is int cant
                    && cant > 0
                        ? cant
                        : 1;

                model.ModuleCount = cantidadModulos;
                ViewData["MaCantModulos"] = cantidadModulos;

                // Reconstruir las filas para recuperar los nombres
                // de los estudiantes al devolver la vista con el error.
                var estudiantes = await (
                    from i in _context.Inscripciones
                    join u in _context.Usuarios on i.UsId equals u.UsId
                    where i.CaMaId == model.CaMaId.Value && u.UsActivo
                    select new
                    {
                        u.UsId,
                        FullName = ((u.UsApellido ?? "") + " " +
                                    (u.UsNombre ?? "")).Trim()
                    }
                ).Distinct().ToListAsync();

                model.Rows = estudiantes
                    .Select(estudiante => new AsistenciaRowViewModel
                    {
                        UsId = estudiante.UsId,
                        FullName = estudiante.FullName,
                        Modulos = Enumerable
                            .Range(0, cantidadModulos)
                            .Select(_ => false)
                            .ToList()
                    })
                    .ToList();

                return View(model);
            }

            // La cantidad de módulos se obtiene de la base de datos.
            var moduleCount = caMa.Materia?.MaCantModulos is int cantidad
                && cantidad > 0
                ? cantidad
                : 1;

            var estudiantesValidos = await _context.Inscripciones
                .Where(i => i.CaMaId == model.CaMaId.Value)
                .Join(
                    _context.Usuarios.Where(u => u.UsActivo),
                    i => i.UsId,
                    u => u.UsId,
                    (i, u) => u.UsId)
                .Distinct()
                .ToListAsync();

            var idsValidos = estudiantesValidos.ToHashSet();

            if (model.Rows.Any(row => !idsValidos.Contains(row.UsId)))
            {
                return BadRequest(
                    "La planilla contiene estudiantes inactivos o no inscriptos " +
                    "en esta cátedra. Recargá la página e intentá nuevamente.");
            }

            foreach (var row in model.Rows)
            {
                var checkedCount = row.Modulos?.Count(x => x) ?? 0;
                var presente = checkedCount > 0;

                var porcentaje = moduleCount > 0
                    ? Math.Round((decimal)checkedCount / moduleCount * 100, 1)
                    : 0m;

                _context.Asistencias.Add(new Asistencia
                {
                    AsFecha = fechaSeleccionada.Value,
                    AsPresente = presente,
                    AsPorcentaje = porcentaje,
                    AsJustificacion = row.AsJustificacion,
                    UsId = row.UsId,
                    MaId = caMa.MaId,
                    CaMaId = caMa.CaMaId
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Las asistencias han sido guardadas correctamente.";

            return RedirectToAction(
                nameof(AsistenciaGlobal),
                new { CaMaId = caMa.CaMaId });
        }

        // GET: Asistencias/AsistenciaGlobal
        // Muestra el histórico de asistencias por materia (filtrado por CaMaId)

        public async Task<IActionResult> AsistenciaGlobal(int? CaMaId)
        {
            // Obtener las cátedras que puede consultar el usuario actual.
            var catedrasQuery = _context.CarreraMaterias
                .Where(cm => cm.CarreraCohorte != null);

            var caMaIdsPermitidos = await CaMaIdsPermitidosAsync();

            // Los docentes solo ven las cátedras que tienen asignadas.
            // Admin y Dirección pueden consultar todas.
            if (caMaIdsPermitidos != null)
            {
                catedrasQuery = catedrasQuery
                    .Where(cm => caMaIdsPermitidos.Contains(cm.CaMaId));
            }

            ViewBag.TodasLasCatedras = await catedrasQuery
                .Select(cm => new
                {
                    CaMaId = cm.CaMaId,
                    CaId = cm.CarreraCohorte!.CaId,
                    Carrera = cm.CarreraCohorte.Carrera!.CaDenominacion,
                    Materia = cm.Materia!.MaDenominacion
                })
                .ToListAsync();

            var model = new AsistenciaGlobalViewModel();

            // Si no se seleccionó una cátedra, mostrar la vista sin datos.
            if (CaMaId == null)
            {
                return View(model);
            }

            // Verificar que el docente tenga permiso para consultar esta cátedra.
            var permitidos = await CaMaIdsPermitidosAsync();

            if (permitidos != null && !permitidos.Contains(CaMaId.Value))
            {
                return NotFound();
            }

            model.CaMaId = CaMaId;

            // Obtener la cátedra con su carrera, materia, cohorte y días de cursada.
            var infoCatedra = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.DiasCursada)
                .Include(cm => cm.Materia)
                .FirstOrDefaultAsync(cm => cm.CaMaId == CaMaId.Value);

            if (infoCatedra == null)
            {
                return NotFound();
            }

            ViewBag.CarreraNombre =
                infoCatedra.CarreraCohorte?.Carrera?.CaDenominacion;

            ViewBag.MateriaNombre =
                infoCatedra.Materia?.MaDenominacion;

            // Obtener los alumnos activos inscriptos en esta cátedra.
            var estudiantes = await (
                from i in _context.Inscripciones
                join u in _context.Usuarios on i.UsId equals u.UsId
                where i.CaMaId == CaMaId.Value && u.UsActivo
                select u
            )
                .Distinct()
                .ToListAsync();

            var usIdsInscritos = estudiantes
                .Select(u => u.UsId)
                .ToList();

            // Obtener las asistencias de estos alumnos para la cátedra.
            // Se mantienen los registros históricos cuyo CaMaId sea NULL.
            var todasLasAsistencias = await _context.Asistencias
                .Where(a =>
                    a.UsId.HasValue &&
                    usIdsInscritos.Contains(a.UsId.Value) &&
                    (a.CaMaId == CaMaId.Value || a.CaMaId == null))
                .ToListAsync();

            // Obtener las fechas válidas según:
            // - Inicio y fin del período académico.
            // - Días de cursada configurados para la cátedra.
            // - Fecha actual: no se consideran jornadas futuras.
            var fechasProgramadas = await ObtenerFechasProgramadasAsync(
                infoCatedra,
                soloPendientes: false);

            var fechasProgramadasSet = fechasProgramadas.ToHashSet();

            // Mostrar únicamente jornadas válidas que ya tengan registros
            // de asistencia para esta cátedra y sus alumnos.
            model.Fechas = todasLasAsistencias
                .Where(a => a.AsFecha.HasValue)
                .Select(a => a.AsFecha!.Value.Date)
                .Where(fecha => fechasProgramadasSet.Contains(fecha))
                .Distinct()
                .OrderBy(fecha => fecha)
                .ToList();

            // Obtener la fecha de inscripción de cada alumno.
            var fechasInscripcion = await _context.Inscripciones
                .Where(i =>
                    i.CaMaId == CaMaId.Value &&
                    usIdsInscritos.Contains(i.UsId))
                .Select(i => new
                {
                    i.UsId,
                    i.InFechaInscripcion
                })
                .ToListAsync();

            var fechasInscripcionDict = fechasInscripcion
                .GroupBy(i => i.UsId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Min(i => i.InFechaInscripcion.Date));

            // Construir una fila por alumno y calcular sus porcentajes.
            foreach (var alumno in estudiantes)
            {
                var asistenciasAlumno = todasLasAsistencias
                    .Where(a => a.UsId == alumno.UsId)
                    .ToList();

                var asistenciaPorFecha = new Dictionary<DateTime, decimal?>();

                decimal sumaPorcentajes = 0m;
                int fechasAplicables = 0;

                // Protección por si algún alumno no tiene una fecha de inscripción.
                if (!fechasInscripcionDict.TryGetValue(
                        alumno.UsId,
                        out var fechaInscripcion))
                {
                    fechaInscripcion = DateTime.MaxValue;
                }

                foreach (var fecha in model.Fechas)
                {
                    // Antes de su inscripción, la asistencia no es aplicable.
                    if (fecha.Date < fechaInscripcion)
                    {
                        asistenciaPorFecha[fecha] = null;
                        continue;
                    }

                    // Si existen registros duplicados históricos para un alumno
                    // y fecha, tomar el más reciente.
                    var registro = asistenciasAlumno
                        .Where(a =>
                            a.AsFecha.HasValue &&
                            a.AsFecha.Value.Date == fecha.Date)
                        .OrderByDescending(a => a.AsId)
                        .FirstOrDefault();

                    decimal porcentaje = registro != null
                        ? registro.AsPorcentaje
                            ?? (registro.AsPresente ? 100m : 0m)
                        : 0m;

                    asistenciaPorFecha[fecha] = porcentaje;

                    sumaPorcentajes += porcentaje;
                    fechasAplicables++;
                }

                decimal? promedio = fechasAplicables > 0
                    ? Math.Round(
                        sumaPorcentajes / fechasAplicables,
                        1)
                    : null;

                model.Rows.Add(new AsistenciaGlobalRowViewModel
                {
                    UsId = alumno.UsId,
                    FullName = $"{alumno.UsApellido} {alumno.UsNombre}",
                    AsistenciaPorFecha = asistenciaPorFecha,
                    PorcentajeAsistencia = promedio
                });
            }

            return View(model);
        }


        private async Task<List<DateTime>> ObtenerFechasProgramadasAsync(
            CarreraMateria carreraMateria,
            bool soloPendientes)
        {
            var cohorte = carreraMateria.CarreraCohorte;

            if (cohorte?.CaCoFechaInicio == null ||
                cohorte.CaCoFechaFin == null ||
                carreraMateria.DiasCursada == null ||
                !carreraMateria.DiasCursada.Any())
            {
                return new List<DateTime>();
            }

            var inicio = cohorte.CaCoFechaInicio.Value.Date;
            var fin = cohorte.CaCoFechaFin.Value.Date;
            var hoy = DateTime.Today;

            // No se habilitan fechas futuras.
            if (inicio > hoy || inicio > fin)
                return new List<DateTime>();

            if (fin > hoy)
                fin = hoy;

            var diasHabilitados = carreraMateria.DiasCursada
                .Select(d => d.DiaSemana)
                .ToHashSet();

            var fechasRegistradas = await _context.Asistencias
                .Where(a =>
                    a.CaMaId == carreraMateria.CaMaId &&
                    a.AsFecha.HasValue)
                .Select(a => a.AsFecha!.Value.Date)
                .Distinct()
                .ToListAsync();

            var registradas = fechasRegistradas.ToHashSet();
            var fechas = new List<DateTime>();

            for (var fecha = inicio; fecha <= fin; fecha = fecha.AddDays(1))
            {
                if (!diasHabilitados.Contains(fecha.DayOfWeek))
                    continue;

                if (soloPendientes && registradas.Contains(fecha))
                    continue;

                fechas.Add(fecha);
            }

            return fechas;
        }

        private void PrepararFechasPendientes(
            AsistenciaFormViewModel model,
            List<DateTime> fechasPendientes)
        {
            var cultura = new CultureInfo("es-AR");

            ViewBag.FechasPendientes = fechasPendientes
                .Select(fecha =>
                {
                    var etiqueta = fecha.ToString(
                        "dddd — dd/MM/yyyy",
                        cultura);

                    etiqueta = char.ToUpper(etiqueta[0], cultura)
                        + etiqueta.Substring(1);

                    return new SelectListItem
                    {
                        Value = fecha.ToString("yyyy-MM-dd"),
                        Text = etiqueta,
                        Selected = model.FechaAsistencia.HasValue
                            && model.FechaAsistencia.Value.Date == fecha
                    };
                })
                .ToList();

            ViewBag.SinFechasPendientes = fechasPendientes.Count == 0;
        }


        private bool AsistenciaExists(int id)
        {
            return _context.Asistencias.Any(e => e.AsId == id);
        }

    }
}


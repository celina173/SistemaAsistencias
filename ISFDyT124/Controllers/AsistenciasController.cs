
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Globalization;
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
        /// IDs de CarreraMateria que el usuario puede consultar.
        /// null significa que no hay restricción para Admin y Dirección.
        /// Los docentes solo pueden acceder a sus cátedras asignadas.
        /// </summary>
        private async Task<List<int>?> CaMaIdsPermitidosAsync()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Dirección"))
                return null;

            var docenteIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(docenteIdClaim, out int docenteId))
                return new List<int>();

            return await _context.Usuarios
                .Where(u => u.UsId == docenteId)
                .SelectMany(u => u.CarreraMaterias)
                .Select(cm => cm.CaMaId)
                .Distinct()
                .ToListAsync();
        }

        // GET: Asistencias
        public async Task<IActionResult> Index(
            int? selectedCarreraId,
            int? selectedMateriaId)
        {
            if (!selectedCarreraId.HasValue)
            {
                var s = (
                    Request.HasFormContentType
                        ? Request.Form["SelectedCarreraId"].FirstOrDefault()
                        : null
                ) ?? Request.Query["SelectedCarreraId"].FirstOrDefault();

                if (!string.IsNullOrEmpty(s) &&
                    int.TryParse(s, out var v1))
                {
                    selectedCarreraId = v1;
                }
            }

            if (!selectedMateriaId.HasValue)
            {
                var s2 = (
                    Request.HasFormContentType
                        ? Request.Form["SelectedMateriaId"].FirstOrDefault()
                        : null
                ) ?? Request.Query["SelectedMateriaId"].FirstOrDefault();

                if (!string.IsNullOrEmpty(s2) &&
                    int.TryParse(s2, out var v2))
                {
                    selectedMateriaId = v2;
                }
            }

            var permitidos = await CaMaIdsPermitidosAsync();

            List<int>? caIdsPermitidos = null;
            List<int>? maIdsPermitidos = null;

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

                maIdsPermitidos = camPermitidas
                    .Select(cm => cm.MaId)
                    .Distinct()
                    .ToList();
            }

            var carrerasQuery = _context.Carreras.AsQueryable();

            if (caIdsPermitidos != null)
            {
                carrerasQuery = carrerasQuery
                    .Where(c => caIdsPermitidos.Contains(c.CaId));
            }

            var carreras = await carrerasQuery
                .Select(c => new CarreraDetalleDto
                {
                    CaId = c.CaId,
                    CaDenominacion = c.CaDenominacion,
                    CarreraMateriasCount =
                        c.CarreraCohortes != null
                            ? c.CarreraCohortes
                                .SelectMany(cc => cc.CarreraMaterias!)
                                .Count()
                            : 0,
                    CarreraCohortesCount =
                        c.CarreraCohortes != null
                            ? c.CarreraCohortes.Count()
                            : 0
                })
                .ToListAsync();

            var materiasQuery = _context.Materias.AsQueryable();

            if (maIdsPermitidos != null)
            {
                materiasQuery = materiasQuery
                    .Where(m => maIdsPermitidos.Contains(m.MaId));
            }

            var materias = await materiasQuery
                .Select(m => new MateriaDetalleDto
                {
                    MaId = m.MaId,
                    MaDenominacion = m.MaDenominacion,
                    MaModalidad = m.MaModalidad,
                    MaCantModulos = m.MaCantModulos,
                    CarreraMateriasCount =
                        m.CarreraMaterias != null
                            ? m.CarreraMaterias.Count()
                            : 0
                })
                .ToListAsync();

            var modelDto = new HomeIndexDto
            {
                Carreras = carreras,
                Materias = materias,
                SelectedCarreraId = selectedCarreraId,
                SelectedMateriaId = selectedMateriaId
            };

            if (selectedCarreraId.HasValue &&
                selectedMateriaId.HasValue)
            {
                var caMa = await _context.CarreraMaterias
                    .FirstOrDefaultAsync(cm =>
                        cm.CarreraCohorte != null &&
                        cm.CarreraCohorte.CaId == selectedCarreraId.Value &&
                        cm.MaId == selectedMateriaId.Value);

                if (caMa != null)
                {
                    if (permitidos != null &&
                        !permitidos.Contains(caMa.CaMaId))
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "No tenés esa cátedra asignada.");

                        return View(modelDto);
                    }

                    var isGlobal =
                        Request.Query.ContainsKey("_global") &&
                        Request.Query["_global"].ToString() == "1";

                    if (isGlobal)
                    {
                        return RedirectToAction(
                            nameof(AsistenciaGlobal),
                            new { CaMaId = caMa.CaMaId });
                    }

                    return RedirectToAction(
                        nameof(Asistencia),
                        new { CaMaId = caMa.CaMaId });
                }

                ModelState.AddModelError(
                    string.Empty,
                    "No existe una relación Carrera-Materia para la selección realizada.");
            }

            return View(modelDto);
        }

        // GET: Asistencias/Asistencia
        public async Task<IActionResult> Asistencia(int? CaMaId)
        {
            var model = new AsistenciaFormViewModel();

            if (!CaMaId.HasValue)
                return View(model);

            var permitidos = await CaMaIdsPermitidosAsync();

            if (permitidos != null &&
                !permitidos.Contains(CaMaId.Value))
            {
                return NotFound();
            }

            var caMa = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.Materia)
                .Include(cm => cm.DiasCursada)
                .FirstOrDefaultAsync(cm => cm.CaMaId == CaMaId.Value);

            if (caMa == null)
                return NotFound();

            model.CaMaId = caMa.CaMaId;

            ViewBag.CarreraNombre =
                caMa.CarreraCohorte?.Carrera?.CaDenominacion ?? "Carrera";

            ViewBag.MateriaNombre =
                caMa.Materia?.MaDenominacion ?? "Materia";

            ViewBag.MateriaId = caMa.MaId;

            var moduleCount =
                caMa.Materia?.MaCantModulos is int cantidad &&
                cantidad > 0
                    ? cantidad
                    : 1;

            model.ModuleCount = moduleCount;
            ViewData["MaCantModulos"] = moduleCount;

            var estudiantes = await (
                from i in _context.Inscripciones
                join u in _context.Usuarios
                    on i.UsId equals u.UsId
                where i.CaMaId == CaMaId.Value && u.UsActivo
                select new
                {
                    u.UsId,
                    FullName = (
                        (u.UsApellido ?? "") + " " +
                        (u.UsNombre ?? "")
                    ).Trim()
                }
            )
            .Distinct()
            .ToListAsync();

            var fechasPendientes =
                await ObtenerFechasProgramadasAsync(
                    caMa,
                    soloPendientes: true);

            if (fechasPendientes.Count > 0)
                model.FechaAsistencia = fechasPendientes[0];

            PrepararFechasPendientes(model, fechasPendientes);

            foreach (var estudiante in estudiantes)
            {
                model.Rows.Add(new AsistenciaRowViewModel
                {
                    UsId = estudiante.UsId,
                    FullName = estudiante.FullName,
                    Modulos = Enumerable
                        .Repeat(false, moduleCount)
                        .ToList()
                });
            }

            return View(model);
        }
        // POST: Asistencias/Asistencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asistencia(
            AsistenciaFormViewModel model)
        {
            if (!model.CaMaId.HasValue)
            {
                return BadRequest(
                    "Debe seleccionar una carrera/materia.");
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
                .FirstOrDefaultAsync(cm =>
                    cm.CaMaId == model.CaMaId.Value);

            if (caMa == null)
                return NotFound();

            var fechaSeleccionada = model.FechaAsistencia?.Date;

            if (!fechaSeleccionada.HasValue)
            {
                TempData["OperationErrorMessage"] =
                    "Seleccioná una fecha válida.";

                return RedirectToAction(
                    nameof(Asistencia),
                    new { CaMaId = caMa.CaMaId });
            }

            var fechasProgramadas =
                await ObtenerFechasProgramadasAsync(
                    caMa,
                    soloPendientes: false);

            if (!fechasProgramadas.Contains(fechaSeleccionada.Value))
            {
                return BadRequest(
                    "La fecha no pertenece al calendario de cursada.");
            }

            var fechasPendientes =
                await ObtenerFechasProgramadasAsync(
                    caMa,
                    soloPendientes: true);

            if (!fechasPendientes.Contains(fechaSeleccionada.Value))
            {
                TempData["OperationErrorMessage"] =
                    "La fecha ya fue registrada o no está disponible para una nueva carga.";

                return RedirectToAction(
                    nameof(Asistencia),
                    new { CaMaId = caMa.CaMaId });
            }

            var moduleCount =
                caMa.Materia?.MaCantModulos is int cantidad &&
                cantidad > 0
                    ? cantidad
                    : 1;

            var estudiantesValidos = await _context.Inscripciones
                .Where(i => i.CaMaId == caMa.CaMaId)
                .Join(
                    _context.Usuarios.Where(u => u.UsActivo),
                    i => i.UsId,
                    u => u.UsId,
                    (i, u) => u.UsId)
                .Distinct()
                .ToListAsync();

            var idsValidos = estudiantesValidos.ToHashSet();

            if (model.Rows == null ||
                model.Rows.Any(row =>
                    !idsValidos.Contains(row.UsId)) ||
                model.Rows.Select(row => row.UsId)
                    .Distinct()
                    .Count() != model.Rows.Count)
            {
                return BadRequest(
                    "La planilla contiene estudiantes inválidos o repetidos.");
            }

            // Verificar que cada fila contenga exactamente los módulos
            // correspondientes a la materia.
            if (model.Rows.Any(row =>
                row.Modulos == null ||
                row.Modulos.Count != moduleCount))
            {
                return BadRequest(
                    "La cantidad de módulos recibida no coincide con la materia.");
            }

            foreach (var row in model.Rows)
            {
                var checkedCount = row.Modulos.Count(presente => presente);

                var porcentaje = Math.Round(
                    (decimal)checkedCount / moduleCount * 100m,
                    1);

                var asistencia = new Asistencia
                {
                    AsFecha = fechaSeleccionada.Value,
                    AsPresente = checkedCount > 0,
                    AsPorcentaje = porcentaje,
                    AsJustificacion = row.AsJustificacion,
                    AsObservacion = row.AsObservacion,
                    UsId = row.UsId,
                    MaId = caMa.MaId,
                    CaMaId = caMa.CaMaId
                };

                for (var i = 0; i < moduleCount; i++)
                {
                    asistencia.Modulos.Add(new AsistenciaModulo
                    {
                        NumeroModulo = i + 1,
                        AsMoPresente = row.Modulos[i]
                    });
                }

                _context.Asistencias.Add(asistencia);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Las asistencias han sido guardadas correctamente.";

            return RedirectToAction(
                nameof(AsistenciaGlobal),
                new { CaMaId = caMa.CaMaId });
        }
        // GET: Asistencias/AsistenciaGlobal
        public async Task<IActionResult> AsistenciaGlobal(int? CaMaId)
        {
            var catedrasQuery = _context.CarreraMaterias
                .Where(cm => cm.CarreraCohorte != null);

            var caMaIdsPermitidos = await CaMaIdsPermitidosAsync();

            if (caMaIdsPermitidos != null)
            {
                catedrasQuery = catedrasQuery
                    .Where(cm =>
                        caMaIdsPermitidos.Contains(cm.CaMaId));
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

            if (CaMaId == null)
                return View(model);

            var permitidos = await CaMaIdsPermitidosAsync();

            if (permitidos != null &&
                !permitidos.Contains(CaMaId.Value))
            {
                return NotFound();
            }

            model.CaMaId = CaMaId;

            var infoCatedra = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.DiasCursada)
                .Include(cm => cm.Materia)
                .FirstOrDefaultAsync(cm =>
                    cm.CaMaId == CaMaId.Value);

            if (infoCatedra == null)
                return NotFound();

            ViewBag.CarreraNombre =
                infoCatedra.CarreraCohorte?.Carrera?.CaDenominacion;

            ViewBag.MateriaNombre =
                infoCatedra.Materia?.MaDenominacion;

            var estudiantes = await (
                from i in _context.Inscripciones
                join u in _context.Usuarios
                    on i.UsId equals u.UsId
                where i.CaMaId == CaMaId.Value && u.UsActivo
                select u
            )
            .Distinct()
            .ToListAsync();

            var usIdsInscritos = estudiantes
                .Select(u => u.UsId)
                .ToList();

            var todasLasAsistencias = await _context.Asistencias
                .Where(a =>
                    a.UsId.HasValue &&
                    usIdsInscritos.Contains(a.UsId.Value) &&
                    (a.CaMaId == CaMaId.Value || a.CaMaId == null))
                .ToListAsync();

            var fechasProgramadas =
                await ObtenerFechasProgramadasAsync(
                    infoCatedra,
                    soloPendientes: false);

            var fechasProgramadasSet =
                fechasProgramadas.ToHashSet();

            model.Fechas = todasLasAsistencias
                .Where(a => a.AsFecha.HasValue)
                .Select(a => a.AsFecha!.Value.Date)
                .Where(fecha =>
                    fechasProgramadasSet.Contains(fecha))
                .Distinct()
                .OrderBy(fecha => fecha)
                .ToList();

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
                    grupo => grupo.Min(
                        i => i.InFechaInscripcion.Date));

            foreach (var alumno in estudiantes)
            {
                var asistenciasAlumno = todasLasAsistencias
                    .Where(a => a.UsId == alumno.UsId)
                    .ToList();

                var asistenciaPorFecha =
                    new Dictionary<DateTime, decimal?>();

                decimal sumaPorcentajes = 0m;
                int fechasAplicables = 0;

                if (!fechasInscripcionDict.TryGetValue(
                    alumno.UsId,
                    out var fechaInscripcion))
                {
                    fechaInscripcion = DateTime.MaxValue;
                }

                foreach (var fecha in model.Fechas)
                {
                    // Las clases anteriores a la inscripción no son
                    // aplicables y no cuentan en el promedio.
                    if (fecha.Date < fechaInscripcion)
                    {
                        asistenciaPorFecha[fecha] = null;
                        continue;
                    }

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
                    FullName =
                        $"{alumno.UsApellido} {alumno.UsNombre}",
                    AsistenciaPorFecha = asistenciaPorFecha,
                    PorcentajeAsistencia = promedio
                });
            }

            return View(model);
        }

        // GET: Asistencias/ModificarAsistencia
        public async Task<IActionResult> ModificarAsistencia(int? CaMaId, DateTime? fecha)
        {
            if (!CaMaId.HasValue || !fecha.HasValue)
                return BadRequest("Debe indicar la cátedra y la fecha.");

            var permitidos = await CaMaIdsPermitidosAsync();

            if (permitidos != null &&
                !permitidos.Contains(CaMaId.Value))
            {
                return NotFound();
            }

            var caMa = await _context.CarreraMaterias
                .Include(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.Materia)
                .Include(cm => cm.DiasCursada)
                .FirstOrDefaultAsync(cm => cm.CaMaId == CaMaId.Value);

            if (caMa == null)
                return NotFound();

            var fechaSeleccionada = fecha.Value.Date;

            var fechasProgramadas = await ObtenerFechasProgramadasAsync(
                caMa,
                soloPendientes: false);

            if (!fechasProgramadas.Contains(fechaSeleccionada))
                return NotFound();

            var estudiantes = await (
                from i in _context.Inscripciones
                join u in _context.Usuarios
                    on i.UsId equals u.UsId
                where i.CaMaId == CaMaId.Value && u.UsActivo
                select new
                {
                    u.UsId,
                    FullName = (
                        (u.UsApellido ?? "") + " " +
                        (u.UsNombre ?? "")
                    ).Trim()
                }
            )
            .Distinct()
            .ToListAsync();

            var idsEstudiantes = estudiantes
                .Select(e => e.UsId)
                .Distinct()
                .ToList();

            var fechasInscripcion = await _context.Inscripciones
                .Where(i =>
                    i.CaMaId == CaMaId.Value &&
                    idsEstudiantes.Contains(i.UsId))
                .GroupBy(i => i.UsId)
                .Select(g => new
                {
                    UsId = g.Key,
                    FechaInscripcion = g.Min(i => i.InFechaInscripcion)
                })
                .ToDictionaryAsync(
                    x => x.UsId,
                    x => x.FechaInscripcion.Date);

            var registros = await _context.Asistencias
                .Include(a => a.Modulos)
                .Where(a =>
                    a.CaMaId == CaMaId.Value &&
                    a.AsFecha.HasValue &&
                    a.AsFecha.Value.Date == fechaSeleccionada &&
                    a.UsId.HasValue &&
                    idsEstudiantes.Contains(a.UsId.Value))
                .OrderByDescending(a => a.AsId)
                .ToListAsync();

            if (registros.Count == 0)
                return NotFound();

            var moduleCount =
                caMa.Materia?.MaCantModulos is int cantidad &&
                cantidad > 0
                    ? cantidad
                    : 1;

            var model = new AsistenciaFormViewModel
            {
                CaMaId = caMa.CaMaId,
                FechaAsistencia = fechaSeleccionada,
                ModuleCount = moduleCount
            };

            ViewBag.CarreraNombre =
                caMa.CarreraCohorte?.Carrera?.CaDenominacion ?? "Carrera";

            ViewBag.MateriaNombre =
                caMa.Materia?.MaDenominacion ?? "Materia";

            ViewBag.MateriaId = caMa.MaId;
            ViewData["MaCantModulos"] = moduleCount;

            foreach (var estudiante in estudiantes)
            {
                // Si existen registros duplicados, se utiliza el más reciente,
                // igual que en la consulta de asistencia global.
                var registro = registros.FirstOrDefault(a =>
                    a.UsId == estudiante.UsId);

                if (registro == null)
                    continue;

                var bloqueada =
                    fechasInscripcion.TryGetValue(
                        estudiante.UsId,
                        out var fechaInscripcion) &&
                    fechaSeleccionada < fechaInscripcion;

                var tieneDetalle = registro.Modulos != null &&
                    registro.Modulos.Any();

                model.Rows.Add(new AsistenciaRowViewModel
                {
                    UsId = estudiante.UsId,
                    FullName = estudiante.FullName,
                    AsPresente = registro.AsPresente,
                    AsJustificacion = registro.AsJustificacion,
                    AsObservacion = registro.AsObservacion,
                    PorcentajeEditado = registro.AsPorcentaje
                        ?? (registro.AsPresente ? 100m : 0m),
                    BloqueadaPorInscripcion = bloqueada,
                    TieneDetalleModulos = tieneDetalle,

                    Modulos = Enumerable.Range(1, moduleCount)
                        .Select(numero => tieneDetalle &&
                            registro.Modulos.Any(m =>
                                m.NumeroModulo == numero &&
                                m.AsMoPresente))
                        .ToList()
                });
            }

            return View(model);
        }


        // POST: Asistencias/ModificarAsistencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModificarAsistencia(
            AsistenciaFormViewModel model)
        {
            if (!model.CaMaId.HasValue ||
                !model.FechaAsistencia.HasValue)
            {
                return BadRequest("Debe indicar la cátedra y la fecha.");
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
                .FirstOrDefaultAsync(cm =>
                    cm.CaMaId == model.CaMaId.Value);

            if (caMa == null)
                return NotFound();

            var fechaSeleccionada = model.FechaAsistencia.Value.Date;

            var fechasProgramadas = await ObtenerFechasProgramadasAsync(
                caMa,
                soloPendientes: false);

            if (!fechasProgramadas.Contains(fechaSeleccionada))
                return BadRequest("La fecha no pertenece al calendario de cursada.");

            var moduleCount =
                caMa.Materia?.MaCantModulos is int cantidad &&
                cantidad > 0
                    ? cantidad
                    : 1;

            var estudiantesValidos = await _context.Inscripciones
                .Where(i => i.CaMaId == caMa.CaMaId)
                .Join(
                    _context.Usuarios.Where(u => u.UsActivo),
                    i => i.UsId,
                    u => u.UsId,
                    (i, u) => u.UsId)
                .Distinct()
                .ToListAsync();

            var idsValidos = estudiantesValidos.ToHashSet();

            if (model.Rows == null ||
                model.Rows.Any(row => !idsValidos.Contains(row.UsId)) ||
                model.Rows.Select(row => row.UsId)
                    .Distinct()
                    .Count() != model.Rows.Count)
            {
                return BadRequest(
                    "La planilla contiene estudiantes inválidos o repetidos.");
            }

            if (model.Rows.Any(row =>
                row.Modulos == null ||
                row.Modulos.Count != moduleCount))
            {
                return BadRequest(
                    "La cantidad de módulos recibida no coincide con la materia.");
            }

            var fechasInscripcion = await _context.Inscripciones
                .Where(i =>
                    i.CaMaId == caMa.CaMaId &&
                    idsValidos.Contains(i.UsId))
                .GroupBy(i => i.UsId)
                .Select(g => new
                {
                    UsId = g.Key,
                    FechaInscripcion = g.Min(i => i.InFechaInscripcion)
                })
                .ToDictionaryAsync(
                    x => x.UsId,
                    x => x.FechaInscripcion.Date);

            var registros = await _context.Asistencias
                .Include(a => a.Modulos)
                .Where(a =>
                    a.CaMaId == caMa.CaMaId &&
                    a.AsFecha.HasValue &&
                    a.AsFecha.Value.Date == fechaSeleccionada &&
                    a.UsId.HasValue &&
                    idsValidos.Contains(a.UsId.Value))
                .OrderByDescending(a => a.AsId)
                .ToListAsync();

            if (registros.Count == 0)
                return NotFound();

            foreach (var row in model.Rows)
            {
                if (fechasInscripcion.TryGetValue(
                        row.UsId,
                        out var fechaInscripcion) &&
                    fechaSeleccionada < fechaInscripcion)
                {
                    // No se modifica una asistencia anterior a la inscripción.
                    continue;
                }

                // Nunca crear una asistencia faltante durante una edición.
                var registro = registros.FirstOrDefault(a =>
                    a.UsId == row.UsId);

                if (registro == null)
                    continue;

                var checkedCount = row.Modulos.Count(presente => presente);

                registro.AsPorcentaje = Math.Round(
                    (decimal)checkedCount / moduleCount * 100m,
                    1);

                registro.AsPresente = checkedCount > 0;
                registro.AsJustificacion = row.AsJustificacion;
                registro.AsObservacion = row.AsObservacion;

                // Actualizar los módulos existentes o completar el detalle
                // de un registro histórico que todavía no tenía módulos.
                for (var numero = 1; numero <= moduleCount; numero++)
                {
                    var modulo = registro.Modulos.FirstOrDefault(m =>
                        m.NumeroModulo == numero);

                    if (modulo == null)
                    {
                        modulo = new AsistenciaModulo
                        {
                            AsId = registro.AsId,
                            NumeroModulo = numero,
                            AsMoPresente = row.Modulos[numero - 1]
                        };

                        registro.Modulos.Add(modulo);
                    }
                    else
                    {
                        modulo.AsMoPresente = row.Modulos[numero - 1];
                    }
                }

                // Quitar detalles que ya no corresponden a la cantidad
                // configurada de módulos de la materia.
                var sobrantes = registro.Modulos
                    .Where(m => m.NumeroModulo > moduleCount)
                    .ToList();

                if (sobrantes.Count > 0)
                    _context.Set<AsistenciaModulo>().RemoveRange(sobrantes);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "La asistencia histórica se actualizó correctamente.";

            return RedirectToAction(
                nameof(AsistenciaGlobal),
                new { CaMaId = caMa.CaMaId });
        }


        private async Task<List<DateTime>> ObtenerFechasProgramadasAsync(
            CarreraMateria carreraMateria,
            bool soloPendientes)
        {
            var cohorte = carreraMateria.CarreraCohorte;

            // Calendario de cursada incompleto (la cohorte no tiene fecha de inicio y fin, o la
            // cátedra no tiene días de cursada cargados): se mantiene el comportamiento anterior
            // en lugar de dejar la lista vacía, que impedía tomar asistencia en toda cátedra que
            // todavía no se configuró.
            if (cohorte?.CaCoFechaInicio == null ||
                cohorte.CaCoFechaFin == null ||
                carreraMateria.DiasCursada == null ||
                !carreraMateria.DiasCursada.Any())
            {
                return await ObtenerFechasSinCalendarioAsync(
                    carreraMateria,
                    soloPendientes);
            }

            var inicio = cohorte.CaCoFechaInicio.Value.Date;
            var fin = cohorte.CaCoFechaFin.Value.Date;
            var hoy = DateTime.Today;

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

            for (var fecha = inicio;
                 fecha <= fin;
                 fecha = fecha.AddDays(1))
            {
                if (!diasHabilitados.Contains(fecha.DayOfWeek))
                    continue;

                if (soloPendientes && registradas.Contains(fecha))
                    continue;

                fechas.Add(fecha);
            }

            return fechas;
        }

        // Fechas disponibles para una cátedra sin calendario de cursada completo.
        // - Para cargar una asistencia nueva (soloPendientes): únicamente el día de hoy, si todavía
        //   no tiene carga.
        // - Para validar, editar o consultar (no soloPendientes): las fechas ya registradas más hoy,
        //   así el historial existente sigue siendo accesible.
        // Las cátedras con calendario completo no pasan por acá y se rigen por su calendario.
        private async Task<List<DateTime>> ObtenerFechasSinCalendarioAsync(
            CarreraMateria carreraMateria,
            bool soloPendientes)
        {
            var hoy = DateTime.Today;

            var registradas = await _context.Asistencias
                .Where(a =>
                    a.CaMaId == carreraMateria.CaMaId &&
                    a.AsFecha.HasValue)
                .Select(a => a.AsFecha!.Value.Date)
                .Distinct()
                .ToListAsync();

            if (soloPendientes)
            {
                return registradas.Contains(hoy)
                    ? new List<DateTime>()
                    : new List<DateTime> { hoy };
            }

            if (!registradas.Contains(hoy))
                registradas.Add(hoy);

            registradas.Sort();
            return registradas;
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

                    etiqueta = char.ToUpper(
                        etiqueta[0],
                        cultura) + etiqueta.Substring(1);

                    return new SelectListItem
                    {
                        Value = fecha.ToString("yyyy-MM-dd"),
                        Text = etiqueta,
                        Selected =
                            model.FechaAsistencia.HasValue &&
                            model.FechaAsistencia.Value.Date == fecha
                    };
                })
                .ToList();

            ViewBag.SinFechasPendientes =
                fechasPendientes.Count == 0;
        }

        private bool AsistenciaExists(int id)
        {
            return _context.Asistencias
                .Any(e => e.AsId == id);
        }
    }
}

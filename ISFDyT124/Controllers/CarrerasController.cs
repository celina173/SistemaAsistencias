
using ISFDyT124.Data;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

[Authorize(Roles = "Admin,Dirección")]
public class CarrerasController : Controller
{
    private readonly InstitutoDbContext _context;

    public CarrerasController(InstitutoDbContext context)
    {
        _context = context;
    }

    // GET: CARRERAS
    public async Task<IActionResult> Index()
    {
        var carreras = await _context.Carreras.ToListAsync();

        var relacionesCohorte = await (
            from cc in _context.CarreraCohortes
            join co in _context.Cohortes
                on cc.CoId equals co.CoId
            select new
            {
                cc.CaId,
                co.CoAnio
            }
        ).ToListAsync();

        var cohortesPorCarrera = relacionesCohorte
            .GroupBy(c => c.CaId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => string.Join(
                    ", ",
                    grupo.Select(c => c.CoAnio)
                        .Distinct()
                        .OrderBy(anio => anio)
                )
            );

        ViewData["CohortesPorCarrera"] = cohortesPorCarrera;

        return View(carreras);
    }

    // GET: CARRERAS/Details/5
    public async Task<IActionResult> Details(int? CaId)
    {
        if (CaId == null)
            return NotFound();

        var carrera = await _context.Carreras
            .Include(c => c.CarreraCohortes!)
                .ThenInclude(cc => cc.Cohorte)
            .FirstOrDefaultAsync(c => c.CaId == CaId.Value);

        if (carrera == null)
            return NotFound();

        // Cargar los contadores de relaciones de forma informativa
        await CargarResumenBorradoCarrera(CaId.Value);

        return View(carrera);
    }

    // GET: CARRERAS/Create
    public IActionResult Create()
    {
        ViewData["CaCoFechaInicio"] = "";
        ViewData["CaCoFechaFin"] = "";

        return View();
    }

    // POST: CARRERAS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CaDenominacion")] Carrera carrera,
        [FromForm] string? CoAnio,
        [FromForm] DateTime? CaCoFechaInicio,
        [FromForm] DateTime? CaCoFechaFin)
    {
        // Validar el año de cohorte.
        if (string.IsNullOrWhiteSpace(CoAnio) ||
            !Regex.IsMatch(CoAnio, @"^\d{4}$"))
        {
            ModelState.AddModelError(
                "CoAnio",
                "Debe ingresar un año de cohorte válido de 4 dígitos.");
        }
        else if (!int.TryParse(CoAnio, out var anio) ||
                 anio < 2000 || anio > 2100)
        {
            ModelState.AddModelError(
                "CoAnio",
                "Ingrese un año de cohorte entre 2000 y 2100.");
        }

        // Validar las fechas del período académico.
        if (!CaCoFechaInicio.HasValue)
        {
            ModelState.AddModelError(
                "CaCoFechaInicio",
                "Debe ingresar la fecha de inicio.");
        }

        if (!CaCoFechaFin.HasValue)
        {
            ModelState.AddModelError(
                "CaCoFechaFin",
                "Debe ingresar la fecha de finalización.");
        }

        if (CaCoFechaInicio.HasValue &&
            CaCoFechaFin.HasValue &&
            CaCoFechaInicio.Value.Date > CaCoFechaFin.Value.Date)
        {
            ModelState.AddModelError(
                "CaCoFechaFin",
                "La fecha de finalización no puede ser anterior a la fecha de inicio.");
        }

        if (ModelState.IsValid)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Crear la carrera.
                _context.Carreras.Add(carrera);
                await _context.SaveChangesAsync();

                var anioInt = int.Parse(CoAnio!);

                // 2. Buscar la cohorte del año seleccionado.
                var cohorte = await _context.Cohortes
                    .FromSqlInterpolated(
                        $"SELECT * FROM Cohortes WITH (UPDLOCK, HOLDLOCK) WHERE CoAnio = {anioInt}")
                    .FirstOrDefaultAsync();

                // 3. Crear la cohorte si todavía no existe.
                if (cohorte == null)
                {
                    var maxId = await _context.Database.SqlQuery<int>(
                        $"SELECT ISNULL(MAX(CoId), 0) AS Value FROM Cohortes WITH (UPDLOCK, HOLDLOCK)")
                        .FirstAsync();

                    cohorte = new Cohorte
                    {
                        CoId = maxId + 1,
                        CoAnio = anioInt,
                        CoEstado = true
                    };

                    _context.Cohortes.Add(cohorte);
                    await _context.SaveChangesAsync();
                }

                // 4. Asociar la carrera con su cohorte y período académico.
                var carreraCohorte = new CarreraCohorte
                {
                    CaId = carrera.CaId,
                    CoId = cohorte.CoId,
                    CaCoFechaInicio = CaCoFechaInicio!.Value.Date,
                    CaCoFechaFin = CaCoFechaFin!.Value.Date
                };

                _context.CarreraCohortes.Add(carreraCohorte);
                await _context.SaveChangesAsync();

                // 5. Confirmar todos los cambios juntos.
                await transaccion.CommitAsync();

                TempData["SuccessMessage"] =
                    "Carrera agregada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar la carrera y su período académico. " +
                    "Verificá los datos e intentá nuevamente.");
            }
        }

        // Conservar los valores introducidos si hay errores.
        ViewData["CoAnio"] = CoAnio;

        ViewData["CaCoFechaInicio"] =
            CaCoFechaInicio?.ToString("yyyy-MM-dd")
            ?? Request.Form["CaCoFechaInicio"].ToString();

        ViewData["CaCoFechaFin"] =
            CaCoFechaFin?.ToString("yyyy-MM-dd")
            ?? Request.Form["CaCoFechaFin"].ToString();

        return View(carrera);
    }

    // GET: CARRERAS/Edit/5
    public async Task<IActionResult> Edit(int? CaId)
    {
        if (CaId == null)
            return NotFound();

        var carrera = await _context.Carreras
            .Include(c => c.CarreraCohortes!)
                .ThenInclude(cc => cc.Cohorte)
            .FirstOrDefaultAsync(c => c.CaId == CaId.Value);

        if (carrera == null)
            return NotFound();

        // Obtener la cohorte y el período académico de la carrera.
        var relacion = carrera.CarreraCohortes?
            .OrderBy(cc => cc.CaCoId)
            .FirstOrDefault();

        ViewData["CoAnio"] =
            relacion?.Cohorte?.CoAnio.ToString() ?? "";

        ViewData["CaCoFechaInicio"] =
            relacion?.CaCoFechaInicio?.ToString("yyyy-MM-dd") ?? "";

        ViewData["CaCoFechaFin"] =
            relacion?.CaCoFechaFin?.ToString("yyyy-MM-dd") ?? "";

        return View(carrera);
    }

    // POST: CARRERAS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? CaId,
        [Bind("CaId,CaDenominacion")] Carrera carrera,
        [FromForm] string? CoAnio,
        [FromForm] DateTime? CaCoFechaInicio,
        [FromForm] DateTime? CaCoFechaFin)
    {
        if (CaId == null || CaId.Value != carrera.CaId)
            return NotFound();

        // Validar el año de cohorte.
        if (string.IsNullOrWhiteSpace(CoAnio) ||
            !Regex.IsMatch(CoAnio, @"^\d{4}$"))
        {
            ModelState.AddModelError(
                "CoAnio",
                "Debe ingresar un año de cohorte válido de 4 dígitos.");
        }
        else if (!int.TryParse(CoAnio, out var anio) ||
                 anio < 2000 || anio > 2100)
        {
            ModelState.AddModelError(
                "CoAnio",
                "Ingrese un año de cohorte entre 2000 y 2100.");
        }

        // Validar las fechas del período académico.
        if (!CaCoFechaInicio.HasValue)
        {
            ModelState.AddModelError(
                "CaCoFechaInicio",
                "Debe ingresar la fecha de inicio.");
        }

        if (!CaCoFechaFin.HasValue)
        {
            ModelState.AddModelError(
                "CaCoFechaFin",
                "Debe ingresar la fecha de finalización.");
        }

        if (CaCoFechaInicio.HasValue &&
            CaCoFechaFin.HasValue &&
            CaCoFechaInicio.Value.Date > CaCoFechaFin.Value.Date)
        {
            ModelState.AddModelError(
                "CaCoFechaFin",
                "La fecha de finalización no puede ser anterior a la fecha de inicio.");
        }

        ModelState.Remove("CarreraCohortes");
        ModelState.Remove("CarrerasMaterias");

        if (ModelState.IsValid)
        {
            var carreraExistente = await _context.Carreras
                .Include(c => c.CarreraCohortes!)
                    .ThenInclude(cc => cc.Cohorte)
                .FirstOrDefaultAsync(c => c.CaId == carrera.CaId);

            if (carreraExistente == null)
                return NotFound();

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualizar la denominación de la carrera.
                carreraExistente.CaDenominacion =
                    carrera.CaDenominacion;

                var anioInt = int.Parse(CoAnio!);

                // Buscar la cohorte del año indicado.
                var cohorte = await _context.Cohortes
                    .FromSqlInterpolated(
                        $"SELECT * FROM Cohortes WITH (UPDLOCK, HOLDLOCK) WHERE CoAnio = {anioInt}")
                    .FirstOrDefaultAsync();

                // Crear la cohorte si todavía no existe.
                if (cohorte == null)
                {
                    var maxId = await _context.Database.SqlQuery<int>(
                        $"SELECT ISNULL(MAX(CoId), 0) AS Value FROM Cohortes WITH (UPDLOCK, HOLDLOCK)")
                        .FirstAsync();

                    cohorte = new Cohorte
                    {
                        CoId = maxId + 1,
                        CoAnio = anioInt,
                        CoEstado = true
                    };

                    _context.Cohortes.Add(cohorte);
                    await _context.SaveChangesAsync();
                }

                // Actualizar la relación entre carrera y cohorte.
                var relacion = carreraExistente.CarreraCohortes?
                    .OrderBy(cc => cc.CaCoId)
                    .FirstOrDefault();

                if (relacion == null)
                {
                    relacion = new CarreraCohorte
                    {
                        CaId = carreraExistente.CaId,
                        CoId = cohorte.CoId,
                        CaCoFechaInicio = CaCoFechaInicio!.Value.Date,
                        CaCoFechaFin = CaCoFechaFin!.Value.Date
                    };

                    _context.CarreraCohortes.Add(relacion);
                }
                else
                {
                    relacion.CoId = cohorte.CoId;
                    relacion.CaCoFechaInicio =
                        CaCoFechaInicio!.Value.Date;
                    relacion.CaCoFechaFin =
                        CaCoFechaFin!.Value.Date;
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["SuccessMessage"] =
                    "Carrera modificada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo modificar la carrera y su período académico. " +
                    "Verificá los datos e intentá nuevamente.");
            }
        }

        // Mantener los datos ingresados cuando existen errores.
        ViewData["CoAnio"] = CoAnio ?? "";

        ViewData["CaCoFechaInicio"] =
            CaCoFechaInicio?.ToString("yyyy-MM-dd") ?? "";

        ViewData["CaCoFechaFin"] =
            CaCoFechaFin?.ToString("yyyy-MM-dd") ?? "";

        return View(carrera);
    }

    // GET: CARRERAS/Delete/5
    public async Task<IActionResult> Delete(int? CaId)
    {
        if (CaId == null)
            return NotFound();

        var carrera = await _context.Carreras
            .FirstOrDefaultAsync(c => c.CaId == CaId);

        if (carrera == null)
            return NotFound();

        await CargarResumenBorradoCarrera(CaId.Value);

        return View(carrera);
    }

    // POST: CARRERAS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int? CaId,
        bool ConfirmarEliminacion)
    {
        if (CaId == null)
            return NotFound();

        if (!ConfirmarEliminacion)
        {
            TempData["Error"] =
                "Debés confirmar explícitamente la eliminación de la carrera.";

            return RedirectToAction(nameof(Delete), new { CaId });
        }

        var carrera = await _context.Carreras
            .FindAsync(CaId.Value);

        if (carrera == null)
            return NotFound();

        try
        {
            _context.Carreras.Remove(carrera);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Carrera eliminada correctamente.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "No se pudo eliminar la carrera. Puede tener datos relacionados que lo impiden.";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool CarreraExists(int? CaId)
    {
        return _context.Carreras.Any(e => e.CaId == CaId);
    }

    private async Task CargarResumenBorradoCarrera(int caId)
    {
        var cohortesQuery = _context.CarreraCohortes
            .Where(cc => cc.CaId == caId);

        var caCoIdsQuery = cohortesQuery
            .Select(cc => cc.CaCoId);

        var carreraMateriasQuery = _context.CarreraMaterias
            .Where(cm =>
                cm.CaCoId.HasValue &&
                caCoIdsQuery.Contains(cm.CaCoId.Value));

        var caMaIdsQuery = carreraMateriasQuery
            .Select(cm => cm.CaMaId);

        ViewData["CantidadCohortes"] =
            await cohortesQuery.CountAsync();

        ViewData["CantidadAsignaciones"] =
            await carreraMateriasQuery.CountAsync();

        ViewData["CantidadAlumnos"] =
            await _context.Inscripciones
                .Where(i => caMaIdsQuery.Contains(i.CaMaId))
                .Select(i => i.UsId)
                .Distinct()
                .CountAsync();

        ViewData["CantidadAsistencias"] =
            await _context.Asistencias
                .Where(a =>
                    a.CaMaId.HasValue &&
                    caMaIdsQuery.Contains(a.CaMaId.Value))
                .CountAsync();
    }
}

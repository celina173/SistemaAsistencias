using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin,Dirección")]
public class MateriasController : Controller
{
    private readonly InstitutoDbContext _context;

    public MateriasController(InstitutoDbContext context)
    {
        _context = context;
    }

    // GET: MATERIAS
    public async Task<IActionResult> Index()
    {
        return View(await _context.Materias.Include(m => m.CarreraMaterias).ToListAsync());
    }

    // GET: MATERIAS/Details/5
    public async Task<IActionResult> Details(int? MaId)
    {
        if (MaId == null)
            return NotFound();

        var materia = await _context.Materias
            .Include(m => m.CarreraMaterias!)
                .ThenInclude(cm => cm.CarreraCohorte!)
                    .ThenInclude(cc => cc.Carrera)
            .Include(m => m.CarreraMaterias!)
                .ThenInclude(cm => cm.CarreraCohorte!)
                    .ThenInclude(cc => cc.Cohorte)
            .Include(m => m.CarreraMaterias!)
                .ThenInclude(cm => cm.DiasCursada)
            .FirstOrDefaultAsync(m => m.MaId == MaId.Value);

        if (materia == null)
            return NotFound();

        return View(materia);
    }

    private async Task CarreraCohortesSelectListAsync(object? selected = null)
    {
        var relaciones = await _context.CarreraCohortes
            .Include(cc => cc.Carrera)
            .Include(cc => cc.Cohorte)
            .OrderBy(cc => cc.Carrera!.CaDenominacion)
            .ThenBy(cc => cc.Cohorte!.CoAnio)
            .ToListAsync();

        var opciones = relaciones.Select(cc => new
        {
            cc.CaCoId,
            Descripcion =
                $"{cc.Carrera!.CaDenominacion} - {cc.Cohorte!.CoAnio}"
        });

        ViewData["CaCoId"] = new SelectList(
            opciones,
            "CaCoId",
            "Descripcion",
            selected
        );
    }


    // GET: MATERIAS/Create
    public async Task<IActionResult> Create()
    {
        await CarreraCohortesSelectListAsync();

        ViewData["MaId"] = new SelectList(
            _context.Materias,
            "MaId",
            "MaDenominacion");

        ViewData["DiasCursadaSeleccionados"] = new List<int>();

        return View();
    }
    // POST: MATERIAS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaDenominacion,MaModalidad,MaCantModulos")] Materia materia, int? CaCoId, List<DayOfWeek>? DiasCursada)
    {
        // La asignación a Carrera-Cohorte es necesaria
        // para poder guardar los días de cursada.
        if (!CaCoId.HasValue)
        {
            ModelState.AddModelError(
                "CaCoId",
                "Debe seleccionar una carrera y cohorte."
            );
        }
        else
        {
            var carreraCohorteExiste = await _context.CarreraCohortes
                .AnyAsync(cc => cc.CaCoId == CaCoId.Value);

            if (!carreraCohorteExiste)
            {
                ModelState.AddModelError(
                    "CaCoId",
                    "La carrera y cohorte seleccionadas no son válidas."
                );
            }
        }

        // Solo se permiten días de lunes a viernes.
        var diasValidos = new HashSet<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        if (DiasCursada == null || DiasCursada.Count == 0)
        {
            ModelState.AddModelError(
                "DiasCursada",
                "Seleccione al menos un día de cursada.");
        }
        else if (DiasCursada.Any(dia => !diasValidos.Contains(dia)))
        {
            ModelState.AddModelError(
                "DiasCursada",
                "Los días de cursada seleccionados no son válidos.");
        }

        // Evita intentar guardar dos veces el mismo día.
        var diasSeleccionados = (DiasCursada ?? new List<DayOfWeek>())
            .Distinct()
            .ToList();

        if (ModelState.IsValid)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Crear la materia.
                _context.Materias.Add(materia);
                await _context.SaveChangesAsync();

                // 2. Crear su asignación a la carrera y cohorte.
                var relacion = new CarreraMateria
                {
                    CaCoId = CaCoId!.Value,
                    MaId = materia.MaId
                };

                _context.CarreraMaterias.Add(relacion);
                await _context.SaveChangesAsync();

                // 3. Guardar los días asociados a esa asignación.
                foreach (var dia in diasSeleccionados)
                {
                    _context.CarreraMateriaDiasCursada.Add(
                        new CarreraMateriaDiaCursada
                        {
                            CaMaId = relacion.CaMaId,
                            DiaSemana = dia
                        }
                    );
                }

                await _context.SaveChangesAsync();

                // Confirmar todos los cambios juntos.
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Materia '{materia.MaDenominacion}' creada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar la materia, su asignación o los días de cursada. " + "Verificá los datos e intentá nuevamente."
                );
            }
        }

        // Recuperar los datos de los selectores si hay errores.
        await CarreraCohortesSelectListAsync(CaCoId);
        ViewData["MaId"] = new SelectList(
            _context.Materias,
            "MaId",
            "MaDenominacion",
            materia.MaId);

        ViewData["DiasCursadaSeleccionados"] = diasSeleccionados
            .Select(dia => (int)dia)
            .ToList();

        return View(materia);
    }

    // GET: MATERIAS/Edit/5
    public async Task<IActionResult> Edit(int? MaId)
    {
        if (MaId == null)
            return NotFound();

        var materia = await _context.Materias
            .Include(m => m.CarreraMaterias!)
                .ThenInclude(cm => cm.CarreraCohorte!)
                    .ThenInclude(cc => cc.Cohorte)
            .Include(m => m.CarreraMaterias!)
                .ThenInclude(cm => cm.DiasCursada)
            .FirstOrDefaultAsync(m => m.MaId == MaId.Value);

        if (materia == null)
            return NotFound();

        var asignacion = materia.CarreraMaterias?
            .OrderBy(cm => cm.CaMaId)
            .FirstOrDefault();

        await CarreraCohortesSelectListAsync(asignacion?.CaCoId);
        ViewData["DiasCursadaSeleccionados"] =
            asignacion?.DiasCursada?
                .Select(d => (int)d.DiaSemana)
                .ToList()
            ?? new List<int>();

        return View(materia);
    }

    // POST: MATERIAS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? MaId,
        [Bind("MaId,MaDenominacion,MaModalidad,MaCantModulos")] Materia materia,
        int? CaCoId,
        List<DayOfWeek>? DiasCursada)
    {
        if (MaId == null || MaId.Value != materia.MaId)
            return NotFound();

        // Validar la asignación Carrera-Cohorte.
        if (!CaCoId.HasValue ||
            !await _context.CarreraCohortes
                .AnyAsync(cc => cc.CaCoId == CaCoId.Value))
        {
            ModelState.AddModelError(
                "CaCoId",
                "Debe seleccionar una carrera y cohorte válidas.");
        }

        // Solo se permiten días de cursada de lunes a viernes.
        DiasCursada ??= new List<DayOfWeek>();

        if (DiasCursada.Any(d =>
                d < DayOfWeek.Monday || d > DayOfWeek.Friday))
        {
            ModelState.AddModelError(
                "DiasCursada",
                "Seleccione únicamente días de lunes a viernes.");
        }

        DiasCursada = DiasCursada.Distinct().ToList();

        if (!DiasCursada.Any())
        {
            ModelState.AddModelError(
                "DiasCursada",
                "Debe seleccionar al menos un día de cursada.");
        }

        if (ModelState.IsValid)
        {
            var materiaExistente = await _context.Materias
                .Include(m => m.CarreraMaterias!)
                    .ThenInclude(cm => cm.DiasCursada)
                .FirstOrDefaultAsync(m => m.MaId == materia.MaId);

            if (materiaExistente == null)
                return NotFound();

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualizar los datos propios de la materia.
                materiaExistente.MaDenominacion = materia.MaDenominacion;
                materiaExistente.MaModalidad = materia.MaModalidad;
                materiaExistente.MaCantModulos = materia.MaCantModulos;

                // La creación actual genera una asignación por materia.
                var asignacion = materiaExistente.CarreraMaterias?
                    .OrderBy(cm => cm.CaMaId)
                    .FirstOrDefault();

                if (asignacion == null)
                {
                    asignacion = new CarreraMateria
                    {
                        MaId = materiaExistente.MaId,
                        CaCoId = CaCoId.Value
                    };

                    _context.CarreraMaterias.Add(asignacion);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    asignacion.CaCoId = CaCoId.Value;
                }

                // Reemplazar los días de cursada de la asignación.
                if (asignacion.DiasCursada != null &&
                    asignacion.DiasCursada.Any())
                {
                    _context.RemoveRange(asignacion.DiasCursada);
                    asignacion.DiasCursada.Clear();
                }

                foreach (var dia in DiasCursada)
                {
                    asignacion.DiasCursada ??=
                        new List<CarreraMateriaDiaCursada>();

                    asignacion.DiasCursada.Add(
                        new CarreraMateriaDiaCursada
                        {
                            CaMaId = asignacion.CaMaId,
                            DiaSemana = dia
                        });
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["SuccessMessage"] =
                    "Materia modificada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo modificar la materia y su asignación. " +
                    "Verificá los datos e intentá nuevamente.");
            }
        }


        // Repoblar los datos necesarios para volver a mostrar el formulario.
        await CarreraCohortesSelectListAsync(CaCoId);
        ViewData["DiasCursadaSeleccionados"] =
            DiasCursada.Select(d => (int)d).ToList();

        return View(materia);
    }
    // GET: MATERIAS/Delete/5
    public async Task<IActionResult> Delete(int? MaId)
    {
        if (MaId == null)
            return NotFound();

        var materia = await _context.Materias
            .Include(m => m.CarreraMaterias)
            .FirstOrDefaultAsync(m => m.MaId == MaId);

        if (materia == null)
            return NotFound();

        await CargarResumenBorradoMateria(MaId.Value);

        return View(materia);
    }

    // POST: MATERIAS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
    int? MaId,
    bool ConfirmarEliminacion)
    {
        if (MaId == null)
            return NotFound();

        if (!ConfirmarEliminacion)
        {
            TempData["Error"] =
                "Debés confirmar explícitamente la eliminación de la materia.";

            return RedirectToAction(nameof(Delete), new { MaId });
        }

        var materia = await _context.Materias
            .FindAsync(MaId.Value);

        if (materia == null)
            return NotFound();

        try
        {
            _context.Materias.Remove(materia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Materia eliminada correctamente.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "No se pudo eliminar la materia. Puede tener datos relacionados (cátedras, asistencias) que lo impiden.";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool MateriaExists(int? MaId)
    {
        return _context.Materias.Any(e => e.MaId == MaId);
    }

    private async Task CargarResumenBorradoMateria(int maId)
    {
        var caMaIdsQuery = _context.CarreraMaterias
            .Where(cm => cm.MaId == maId)
            .Select(cm => cm.CaMaId);

        ViewData["CantidadAsignaciones"] =
            await _context.CarreraMaterias
                .CountAsync(cm => cm.MaId == maId);

        ViewData["CantidadAlumnos"] =
            await _context.Inscripciones
                .Where(i => caMaIdsQuery.Contains(i.CaMaId))
                .Select(i => i.UsId)
                .Distinct()
                .CountAsync();

        ViewData["CantidadAsistencias"] =
            await _context.Asistencias
                .Where(a => a.MaId == maId ||
                            (a.CaMaId.HasValue && caMaIdsQuery.Contains(a.CaMaId.Value)))
                .CountAsync();
    }
}

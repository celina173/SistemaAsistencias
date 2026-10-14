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
        {
            return NotFound();
        }
        var materia = await _context.Materias
            .Include(m => m.CarreraMaterias)
            .FirstOrDefaultAsync(m => m.MaId == MaId);
        if (materia == null)
        {
            return NotFound();
        }
        return View(materia);
    }

    private async Task<SelectList> CarreraCohortesSelectListAsync(object? selected = null)
    {
        var items = await _context.CarreraCohortes
            .Include(cc => cc.Carrera)
            .Include(cc => cc.Cohorte)
            .Select(cc => new
            {
                cc.CaCoId,
                Denominacion = cc.Carrera!.CaDenominacion + " - " + cc.Cohorte!.CoAnio,
            })
            .ToListAsync();
        return new SelectList(items, "CaCoId", "Denominacion", selected);
    }

    // GET: MATERIAS/Create
    public async Task<IActionResult> Create()
    {
        // Una materia se asigna directo a una Carrera-Cohorte (no solo a la Carrera):
        // "Inglés I" de la cohorte 2025 es una cátedra distinta a la de la cohorte 2026.
        ViewData["CaCoId"] = await CarreraCohortesSelectListAsync();
        ViewData["MaId"] = new SelectList(_context.Materias, "MaId", "MaDenominacion");
        return View();
    }

    // POST: MATERIAS/Create

    // POST: MATERIAS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("MaDenominacion,MaModalidad,MaCantModulos")] Materia materia,
        int? CaCoId)
    {
        if (ModelState.IsValid)
        {
            // Validar que la carrera-cohorte seleccionada exista.
            if (CaCoId.HasValue)
            {
                var carreraCohorteExiste = await _context.CarreraCohortes
                    .AnyAsync(cc => cc.CaCoId == CaCoId.Value);

                if (!carreraCohorteExiste)
                {
                    ModelState.AddModelError(
                        "CaCoId",
                        "La carrera y cohorte seleccionadas no son válidas.");
                }
            }

            if (ModelState.IsValid)
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. Guardar la materia y obtener el MaId generado.
                    _context.Materias.Add(materia);
                    await _context.SaveChangesAsync();

                    // 2. Crear la relación usando el MaId real.
                    if (CaCoId.HasValue)
                    {
                        var rel = new CarreraMateria
                        {
                            CaCoId = CaCoId.Value,
                            MaId = materia.MaId
                        };

                        _context.CarreraMaterias.Add(rel);
                        await _context.SaveChangesAsync();
                    }

                    // Confirmar ambas operaciones.
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] =
                        $"Materia '{materia.MaDenominacion}' creada correctamente.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar la materia o su asignación. " +
                        "Verificá los datos e intentá nuevamente.");
                }
            }
        }

        // Repoblar el selector si hay errores de validación o guardado.
        ViewData["CaCoId"] =
            await CarreraCohortesSelectListAsync(CaCoId);

        ViewData["MaId"] =
            new SelectList(
                _context.Materias,
                "MaId",
                "MaDenominacion",
                materia.MaId);

        return View(materia);
    }


    // GET: MATERIAS/Edit/5
    public async Task<IActionResult> Edit(int? MaId)
    {
        if (MaId == null)
        {
            return NotFound();
        }

        var materia = await _context.Materias.FindAsync(MaId);
        if (materia == null)
        {
            return NotFound();
        }

        return View(materia);
    }

    // POST: MATERIAS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? MaId,
        [Bind("MaId,MaDenominacion, MaModalidad , MaCantModulos")] Materia materia
    )
    {
        if (MaId != materia.MaId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // POST: Edit — después del SaveChangesAsync y antes del redirect
            try
            {
                _context.Update(materia);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MateriaExists(materia.MaId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData["SuccessMessage"] = $"Materia '{materia.MaDenominacion}' modificada correctamente.";
            return RedirectToAction(nameof(Index));
        }
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

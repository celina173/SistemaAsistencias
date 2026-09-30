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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("MaDenominacion,MaModalidad,MaCantModulos")] Materia materia, int? CaCoId)
    {
        materia.MaDenominacion = materia.MaDenominacion?.Trim();

        // 1) Carrera-Cohorte obligatoria y existente (el mensaje sale bajo el desplegable de Carrera)
        CarreraCohorte? carreraCohorte = null;
        if (!CaCoId.HasValue)
        {
            ModelState.AddModelError("CaCoId", "Debe seleccionar una Carrera.");
        }
        else
        {
            carreraCohorte = await _context.CarreraCohortes
                .FirstOrDefaultAsync(cc => cc.CaCoId == CaCoId.Value);

            if (carreraCohorte == null)
                ModelState.AddModelError("CaCoId", "La Carrera seleccionada no existe.");
        }

        // 2) El nombre solo puede repetirse en OTRA carrera
        if (carreraCohorte != null && !string.IsNullOrEmpty(materia.MaDenominacion))
        {
            bool repetidaEnLaCarrera = await (
                from cm in _context.CarreraMaterias
                join m in _context.Materias on cm.MaId equals m.MaId
                join cc in _context.CarreraCohortes on cm.CaCoId equals cc.CaCoId
                where cc.CaId == carreraCohorte.CaId
                      && m.MaDenominacion == materia.MaDenominacion
                select cm.MaId
            ).AnyAsync();

            if (repetidaEnLaCarrera)
                ModelState.AddModelError("MaDenominacion",
                    "Esa Carrera ya tiene una materia con ese nombre.");
        }

        if (ModelState.IsValid)
        {
            // Un solo SaveChanges: Materia y CarreraMateria se guardan juntas o no se guarda nada
            materia.CarreraMaterias.Add(new CarreraMateria { CaCoId = CaCoId!.Value });
            _context.Add(materia);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // repoblar selects en caso de error
        ViewData["CaCoId"] = await CarreraCohortesSelectListAsync(CaCoId);
        ViewData["MaId"] = new SelectList(_context.Materias, "MaId", "MaDenominacion", materia.MaId);
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
            return RedirectToAction(nameof(Index));
        }
        return View(materia);
    }

    // GET: MATERIAS/Delete/5
    public async Task<IActionResult> Delete(int? MaId)
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

    // POST: MATERIAS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? MaId)
    {
        var materia = await _context.Materias.FindAsync(MaId);
        if (materia != null)
        {
            _context.Materias.Remove(materia);
        }

        // FIX: sin try/catch, un borrado que choque contra una FK (p.ej. Asistencia.CaMaId
        // es Restrict contra CarreraMateria) tiraba una DbUpdateException sin capturar y
        // reventaba la request con un error 500 en vez de avisarle al usuario. Se sigue el
        // mismo criterio que ya usa AdminController para sus borrados.
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "No se pudo eliminar la materia. Puede tener datos relacionados (cátedras, asistencias) que lo impiden.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }

    private bool MateriaExists(int? MaId)
    {
        return _context.Materias.Any(e => e.MaId == MaId);
    }
}

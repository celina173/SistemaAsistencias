using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ISFDyT124.Models;
using ISFDyT124.Data;

public class MateriasController : Controller
{
    private readonly AsistenciaContext _context;

    public MateriasController(AsistenciaContext context)
    {
        _context = context;
    }

    // GET: MATERIAS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Materias.ToListAsync());
    }

    // GET: MATERIAS/Details/5
    public async Task<IActionResult> Details(int? MaId)
    {
        if (MaId == null)
        {
            return NotFound();
        }

        var materia = await _context.Materias
            .FirstOrDefaultAsync(m => m.MaId == MaId);
        if (materia == null)
        {
            return NotFound();
        }

        return View(materia);
    }

    // GET: MATERIAS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: MATERIAS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaId,MaDenominacion,MaModalidad,MaCantModulos")] Materia materia)
    {
        if (ModelState.IsValid)
        {
            _context.Add(materia);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
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
    public async Task<IActionResult> Edit(int? MaId, [Bind("MaId,MaDenominacion,MaModalidad,MaCantModulos")] Materia materia)
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

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool MateriaExists(int? MaId)
    {
        return _context.Materias.Any(e => e.MaId == MaId);
    }
}

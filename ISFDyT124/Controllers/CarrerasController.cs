using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ISFDyT124.Models;
using ISFDyT124.Data;

[Authorize(Roles = "Admin")]
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
        return View(await _context.Carreras.ToListAsync());
    }

    // GET: CARRERAS/Details/5
    public async Task<IActionResult> Details(int? CaId)
    {
        if (CaId == null)
        {
            return NotFound();
        }

        var carrera = await _context.Carreras
            .FirstOrDefaultAsync(c => c.CaId == CaId);
        if (carrera == null)
        {
            return NotFound();
        }

        return View(carrera);
    }

    // GET: CARRERAS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CARRERAS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CaId,CaDenominacion,CarrerasCohortes,CarrerasMaterias")] Carrera carrera)
    {
        if (ModelState.IsValid)
        {
            _context.Add(carrera);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(carrera);
    }

    // GET: CARRERAS/Edit/5
    public async Task<IActionResult> Edit(int? CaId)
    {
        if (CaId == null)
        {
            return NotFound();
        }

        var carrera = await _context.Carreras.FindAsync(CaId);
        if (carrera == null)
        {
            return NotFound();
        }
        return View(carrera);
    }

    // POST: CARRERAS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? CaId, [Bind("CaId,CaDenominacion,CarrerasCohortes,CarrerasMaterias")] Carrera carrera)
    {
        if (CaId != carrera.CaId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(carrera);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarreraExists(carrera.CaId))
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
        return View(carrera);
    }

    // GET: CARRERAS/Delete/5
    public async Task<IActionResult> Delete(int? CaId)
    {
        if (CaId == null)
        {
            return NotFound();
        }

        var carrera = await _context.Carreras
            .FirstOrDefaultAsync(c => c.CaId == CaId);
        if (carrera == null)
        {
            return NotFound();
        }

        return View(carrera);
    }

    // POST: CARRERAS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? CaId)
    {
        var carrera = await _context.Carreras.FindAsync(CaId);
        if (carrera != null)
        {
            _context.Carreras.Remove(carrera);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CarreraExists(int? CaId)
    {
        return _context.Carreras.Any(e => e.CaId == CaId);
    }
}
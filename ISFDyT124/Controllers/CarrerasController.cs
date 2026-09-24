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
        return View(await _context.Carreras.ToListAsync());
    }

    // GET: CARRERAS/Details/5
    public async Task<IActionResult> Details(int? CaId)
    {
        if (CaId == null)
        {
            return NotFound();
        }

        var carrera = await _context.Carreras.FirstOrDefaultAsync(c => c.CaId == CaId);
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
    public async Task<IActionResult> Create([Bind("CaDenominacion")] Carrera carrera, [FromForm] string? CoAnio)
    {
        // Validación del año de cohorte: obligatorio, numérico y exactamente 4 dígitos
        if (string.IsNullOrWhiteSpace(CoAnio) || !Regex.IsMatch(CoAnio, "^\\d{4}$"))
        {
            ModelState.AddModelError("CoAnio", "Debe ingresar un año de cohorte válido de 4 dígitos.");
        }
        else
        {
            if (!int.TryParse(CoAnio, out var anio) || anio < 2000 || anio > 2100)
            {
                ModelState.AddModelError("CoAnio", "Ingrese un año de cohorte entre 2000 y 2100.");
            }
        }

        if (ModelState.IsValid)
        {
            // Guardar la carrera
            _context.Add(carrera);
            await _context.SaveChangesAsync();

            // Buscar o crear la cohorte
            var anioInt = int.Parse(CoAnio!);
            var cohorte = await _context.Cohortes.FirstOrDefaultAsync(c => c.CoAnio == anioInt);
            if (cohorte == null)
            {
                // CoId is configured como ValueGeneratedNever in the DbContext: assign next ID manually
                var maxId = await _context.Cohortes.MaxAsync(c => (int?)c.CoId) ?? 0;
                cohorte = new Cohorte { CoId = maxId + 1, CoAnio = anioInt, CoEstado = true };
                _context.Cohortes.Add(cohorte);
                await _context.SaveChangesAsync();
            }

            // Crear la relación CarreraCohorte
            var caCo = new CarreraCohorte { CaId = carrera.CaId, CoId = cohorte.CoId };
            _context.CarreraCohortes.Add(caCo);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Carrera agregada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Repoblar CoAnio para la vista en caso de error
        ViewData["CoAnio"] = CoAnio;
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
    public async Task<IActionResult> Edit(
        int? CaId,
        [Bind("CaId,CaDenominacion,CarrerasCohortes,CarrerasMaterias")] Carrera carrera
    )
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

        var carrera = await _context.Carreras.FirstOrDefaultAsync(c => c.CaId == CaId);
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
        TempData["SuccessMessage"] = "Carrera eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private bool CarreraExists(int? CaId)
    {
        return _context.Carreras.Any(e => e.CaId == CaId);
    }
}

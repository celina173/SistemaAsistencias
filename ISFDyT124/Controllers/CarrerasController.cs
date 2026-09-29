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

            // Buscar o crear la cohorte. CoId no es IDENTITY (ValueGeneratedNever en el
            // DbContext) -- se calcula a mano como MAX+1. Todo este bloque va con lock
            // (UPDLOCK, HOLDLOCK) para que dos altas de carrera simultáneas con el mismo año
            // nuevo no calculen el mismo CoId ni creen dos cohortes distintas para el mismo
            // año (ticket 6.13).
            var anioInt = int.Parse(CoAnio!);
            using var transaccionCohorte = await _context.Database.BeginTransactionAsync();

            var cohorte = await _context
                .Cohortes.FromSqlInterpolated(
                    $"SELECT * FROM Cohortes WITH (UPDLOCK, HOLDLOCK) WHERE CoAnio = {anioInt}"
                )
                .FirstOrDefaultAsync();

            if (cohorte == null)
            {
                var maxId = await _context
                    .Database.SqlQuery<int>(
                        $"SELECT ISNULL(MAX(CoId), 0) AS Value FROM Cohortes WITH (UPDLOCK, HOLDLOCK)"
                    )
                    .FirstAsync();
                cohorte = new Cohorte { CoId = maxId + 1, CoAnio = anioInt, CoEstado = true };
                _context.Cohortes.Add(cohorte);
                await _context.SaveChangesAsync();
            }

            // Crear la relación CarreraCohorte
            var caCo = new CarreraCohorte { CaId = carrera.CaId, CoId = cohorte.CoId };
            _context.CarreraCohortes.Add(caCo);
            await _context.SaveChangesAsync();

            await transaccionCohorte.CommitAsync();

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

        // FIX: sin try/catch, cualquier choque de FK al borrar (cascada a CarreraCohorte /
        // CarreraMateria con datos relacionados) tiraba una DbUpdateException sin capturar
        // y reventaba la request con un error 500 en vez de avisarle al usuario. Mismo
        // criterio que ya usa AdminController para sus borrados.
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "No se pudo eliminar la carrera. Puede tener datos relacionados que lo impiden.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Carrera eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private bool CarreraExists(int? CaId)
    {
        return _context.Carreras.Any(e => e.CaId == CaId);
    }
}

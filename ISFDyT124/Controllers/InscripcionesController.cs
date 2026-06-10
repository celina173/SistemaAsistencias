
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using ISFDyT124.Models;
using ISFDyT124.Data;

public class InscripcionesController : Controller
{
    private readonly SiAsContext _context;

    public InscripcionesController(SiAsContext context)
    {
        _context = context;
    }

    // GET: INSCRIPCIONESS
    public async Task<IActionResult> Index()
    {
        return View(await _context.Inscripciones.ToListAsync());
    }

    // GET: INSCRIPCIONESS/Details/5
    public async Task<IActionResult> Details(int? inid)
    {
        if (inid == null)
        {
            return NotFound();
        }

        var inscripciones = await _context.Inscripciones
            .FirstOrDefaultAsync(m => m.InId == inid);
        if (inscripciones == null)
        {
            return NotFound();
        }

        return View(inscripciones);
    }

    // GET: INSCRIPCIONESS/Create
    public async Task<IActionResult> Create()
    {
        // find role id for 'Estudiante' (case-insensitive)
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion.ToLower() == "estudiante");

        List<object> students;
        if (role != null)
        {
            students = await _context.Usuarios
                .Where(u => u.RoId == role.RoId)
                .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
                .Cast<object>()
                .ToListAsync();
        }
        else
        {
            // no role named 'Estudiante' found -> empty list
            students = new List<object>();
        }

        ViewData["UsId"] = new SelectList(students, "UsId", "FullName");

        // populate Carreras_Materias using CaMaDenominacion as the display text
        var cam = await _context.CarreraMaterias
            .Select(cm => new { cm.CaMaId, cm.CaMaDenominacion })
            .ToListAsync();
        ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "CaMaDenominacion");

        return View();
    }

    // POST: INSCRIPCIONESS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("InId,UsId,CaMaId")] Inscripciones inscripciones)
    {
        if (ModelState.IsValid)
        {
            _context.Add(inscripciones);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // repopulate selects when returning view on error
        var students = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
            .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
            .ToListAsync();
        ViewData["UsId"] = new SelectList(students, "UsId", "FullName", inscripciones.UsId);

        var cam = await _context.CarreraMaterias
            .Select(cm => new { cm.CaMaId, cm.CaMaDenominacion })
            .ToListAsync();
        ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "CaMaDenominacion", inscripciones.CaMaId);

        return View(inscripciones);
    }

    // GET: INSCRIPCIONESS/Edit/5
    public async Task<IActionResult> Edit(int? inid)
    {
        if (inid == null)
        {
            return NotFound();
        }

        var inscripciones = await _context.Inscripciones.FindAsync(inid);
        if (inscripciones == null)
        {
            return NotFound();
        }
        // populate selects for edit view
        var students = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
            .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
            .ToListAsync();
        ViewData["UsId"] = new SelectList(students, "UsId", "FullName", inscripciones.UsId);

        var cam = await _context.CarreraMaterias
            .Select(cm => new { cm.CaMaId, cm.CaMaDenominacion })
            .ToListAsync();
        ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "CaMaDenominacion", inscripciones.CaMaId);

        return View(inscripciones);
    }

    // POST: INSCRIPCIONESS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? inid, [Bind("InId,UsId,CaMaId,Usuarios,Carreras_Materias")] Inscripciones inscripciones)
    {
        if (inid != inscripciones.InId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(inscripciones);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InscripcionesExists(inscripciones.InId))
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
        // repopulate selects when returning view on error
        var students = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Rol != null && u.Rol.RoDenominacion == "Estudiante")
            .Select(u => new { u.UsId, FullName = ((u.UsApellido ?? "") + " " + (u.UsNombre ?? "")).Trim() })
            .ToListAsync();
        ViewData["UsId"] = new SelectList(students, "UsId", "FullName", inscripciones.UsId);

        var cam = await _context.CarreraMaterias
            .Select(cm => new { cm.CaMaId, cm.CaMaDenominacion })
            .ToListAsync();
        ViewData["CaMaId"] = new SelectList(cam, "CaMaId", "CaMaDenominacion", inscripciones.CaMaId);

        return View(inscripciones);
    }

    // GET: INSCRIPCIONESS/Delete/5
    public async Task<IActionResult> Delete(int? inid)
    {
        if (inid == null)
        {
            return NotFound();
        }

        var inscripciones = await _context.Inscripciones
            .FirstOrDefaultAsync(m => m.InId == inid);
        if (inscripciones == null)
        {
            return NotFound();
        }

        return View(inscripciones);
    }

    // POST: INSCRIPCIONESS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? inid)
    {
        var inscripciones = await _context.Inscripciones.FindAsync(inid);
        if (inscripciones != null)
        {
            _context.Inscripciones.Remove(inscripciones);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool InscripcionesExists(int? inid)
    {
        return _context.Inscripciones.Any(e => e.InId == inid);
    }
}

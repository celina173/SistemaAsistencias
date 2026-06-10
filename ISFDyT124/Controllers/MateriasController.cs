
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ISFDyT124.Models;
using ISFDyT124.Data;
using ISFDyT124.DTO;

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
        var materias = await _context.Materias
            .Select(m => new MateriaDetalleDto
            {
                MaId = m.MaId,
                MaDenominacion = m.MaDenominacion,
                MaModalidad = m.MaModalidad,
                MaCantModulos = m.MaCantModulos,
                CarreraMateriasCount = m.CarreraMaterias != null ? m.CarreraMaterias.Count : 0
            })
            .ToListAsync();

        return View(materias);
    }

    // GET: MATERIAS/Details/5
    public async Task<IActionResult> Details(int? MaId)
    {
        if (MaId == null)
        {
            return NotFound();
        }

        var materia = await _context.Materias
            .Where(m => m.MaId == MaId)
            .Select(m => new MateriaDetalleDto
            {
                MaId = m.MaId,
                MaDenominacion = m.MaDenominacion,
                MaModalidad = m.MaModalidad,
                MaCantModulos = m.MaCantModulos,
                CarreraMateriasCount = m.CarreraMaterias != null ? m.CarreraMaterias.Count : 0
            })
            .FirstOrDefaultAsync();

        if (materia == null)
        {
            return NotFound();
        }

        return View(materia);
    }

    // GET: MATERIAS/Create
    public IActionResult Create()
    {
        return View(new MateriaCrearDto());
    }

    // POST: MATERIAS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MateriaCrearDto materiaDto)
    {
        if (ModelState.IsValid)
        {
            var materia = new Materia
            {
                MaDenominacion = materiaDto.MaDenominacion ?? string.Empty,
                MaModalidad = materiaDto.MaModalidad,
                MaCantModulos = materiaDto.MaCantModulos
            };

            _context.Add(materia);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(materiaDto);
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

        var materiaDto = new MateriaCrearDto
        {
            MaId = materia.MaId,
            MaDenominacion = materia.MaDenominacion,
            MaModalidad = materia.MaModalidad,
            MaCantModulos = materia.MaCantModulos
        };

        return View(materiaDto);
    }

    // POST: MATERIAS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? MaId, MateriaCrearDto materiaDto)
    {
        if (MaId != materiaDto.MaId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(materiaDto);
        }

        var materia = await _context.Materias.FindAsync(MaId);
        if (materia == null)
        {
            return NotFound();
        }

        materia.MaDenominacion = materiaDto.MaDenominacion ?? string.Empty;
        materia.MaModalidad = materiaDto.MaModalidad;
        materia.MaCantModulos = materiaDto.MaCantModulos;

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

    // GET: MATERIAS/Delete/5
    public async Task<IActionResult> Delete(int? MaId)
    {
        if (MaId == null)
        {
            return NotFound();
        }

        var materia = await _context.Materias
            .Where(m => m.MaId == MaId)
            .Select(m => new MateriaDetalleDto
            {
                MaId = m.MaId,
                MaDenominacion = m.MaDenominacion,
                MaModalidad = m.MaModalidad,
                MaCantModulos = m.MaCantModulos,
                CarreraMateriasCount = m.CarreraMaterias != null ? m.CarreraMaterias.Count : 0
            })
            .FirstOrDefaultAsync();

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
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool MateriaExists(int? MaId)
    {
        return _context.Materias.Any(e => e.MaId == MaId);
    }
}

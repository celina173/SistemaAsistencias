using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly InstitutoDbContext _context;

        public HomeController(InstitutoDbContext context)
        {
            _context = context;
        }

        // Esta es la pantalla de Inicio (Carrera y Materia) -> /Home/Index
        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexDto();

            // Un Docente solo debe ver las carreras/materias de sus propias cátedras
            // asignadas (CarreraMaterias) — antes se listaban TODAS sin importar el rol.
            // Admin/Dirección siguen viendo el listado completo.
            if (User.IsInRole("Docente"))
            {
                var docenteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(docenteIdClaim, out int docenteId))
                    return Unauthorized();

                // No se filtra por año de cohorte: la cohorte de una cátedra puede
                // representar el año de ingreso de esa camada (ej. una materia de 2do año
                // de la carrera queda atada a la cohorte del año anterior), así que se
                // listan todas las cátedras asignadas al docente sin importar el año.
                var catedras = await _context
                    .Usuarios.Where(u => u.UsId == docenteId)
                    .SelectMany(u => u.CarreraMaterias)
                    .Where(cm => cm.CarreraCohorte != null)
                    .Select(cm => new
                    {
                        cm.MaId,
                        CaId = cm.CarreraCohorte != null ? cm.CarreraCohorte.CaId : (int?)null,
                    })
                    .ToListAsync();

                var maIds = catedras.Select(c => c.MaId).Distinct().ToList();
                var caIds = catedras
                    .Where(c => c.CaId.HasValue)
                    .Select(c => c.CaId!.Value)
                    .Distinct()
                    .ToList();

                model.Carreras = await _context
                    .Carreras.Where(c => caIds.Contains(c.CaId))
                    .Select(c => new CarreraDetalleDto
                    {
                        CaId = c.CaId,
                        CaDenominacion = c.CaDenominacion,
                    })
                    .ToListAsync();

                model.Materias = await _context
                    .Materias.Where(m => maIds.Contains(m.MaId))
                    .Select(m => new MateriaDetalleDto
                    {
                        MaId = m.MaId,
                        MaDenominacion = m.MaDenominacion,
                    })
                    .ToListAsync();
            }
            else
            {
                model.Carreras = await _context
                    .Carreras.Select(c => new CarreraDetalleDto
                    {
                        CaId = c.CaId,
                        CaDenominacion = c.CaDenominacion,
                    })
                    .ToListAsync();
                model.Materias = await _context
                    .Materias.Select(m => new MateriaDetalleDto
                    {
                        MaId = m.MaId,
                        MaDenominacion = m.MaDenominacion,
                    })
                    .ToListAsync();
            }

            return View(model);


        }

        [HttpGet]
        [AllowAnonymous] // Permite mostrar el aviso de denegado sin causar bucles de redirección
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Route("Home/Error")]
        [AllowAnonymous] // Permite capturar códigos de error y excepciones globalmente de forma segura
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode == 404)
            {
                return View("NotFound");
            }
            else if (statusCode == 403)
            {
                return View("AccessDenied");
            }

            // Error general 500 u otras excepciones no controladas
            return View();
        }
    }
}


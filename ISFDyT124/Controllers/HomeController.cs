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
            var model = new HomeIndexDto
            {
                Carreras = await _context.Carreras
                    .Select(c => new CarreraDetalleDto
                    {
                        CaId = c.CaId,
                        CaDenominacion = c.CaDenominacion
                    })
                    .ToListAsync(),
                Materias = await _context.Materias
                    .Select(m => new MateriaDetalleDto
                    {
                        MaId = m.MaId,
                        MaDenominacion = m.MaDenominacion
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        // Esta es la de tomar asistencia -> /Home/Asistencia
        public IActionResult Asistencia()
        {
            return View();
        }

        // Esta es la global -> /Home/AsistenciaGlobal
        public IActionResult AsistenciaGlobal()
        {
            return View();
        }
    }
}

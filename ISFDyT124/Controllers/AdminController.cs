using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly InstitutoDbContext _context;

        public AdminController(InstitutoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalAlumnos = await _context.Usuarios
                .Where(u => u.RoId == 3)
                .CountAsync();

            ViewBag.TotalDocentes = await _context.Usuarios
                .Where(u => u.RoId == 2)
                .CountAsync();

            ViewBag.TotalMaterias = await _context.Materias.CountAsync();
            ViewBag.TotalCarreras = await _context.Carreras.CountAsync();
            ViewBag.TotalAsistencias = await _context.Asistencias.CountAsync();

            var hoy = DateTime.Today;
            ViewBag.AsistenciasHoy = await _context.Asistencias
                .Where(a => a.AsFecha != null && a.AsFecha.Value.Date == hoy)
                .CountAsync();

            return View();
        }

        public async Task<IActionResult> UsuariosABM()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.CarreraCohorte).ThenInclude(cc => cc.Carrera)
                .Include(u => u.CarreraCohorte).ThenInclude(cc => cc.Cohorte)
                .Include(u => u.CarreraMaterias).ThenInclude(cm => cm.Carrera)
                .Include(u => u.CarreraMaterias).ThenInclude(cm => cm.Materia)
                .Select(u => new UsuarioDetalleDto
                {
                    UsId = u.UsId,
                    UsApellido = u.UsApellido,
                    UsNombre = u.UsNombre,
                    UsEmail = u.UsEmail,
                    UsDni = u.UsDni,
                    RoId = u.RoId,
                    RoDenominacion = u.Rol != null ? u.Rol.RoDenominacion : null,
                    CaCoId = u.CaCoId,
                    CarreraCohorteDenominacion = u.CaCoId != null && u.CarreraCohorte != null
                        ? u.CarreraCohorte.Carrera.CaDenominacion + " - " + u.CarreraCohorte.Cohorte.CoAnio
                        : null,
                    MateriasDenominacion = u.CarreraMaterias.Any()
                        ? string.Join(", ", u.CarreraMaterias.Select(cm => cm.Carrera.CaDenominacion + " / " + cm.Materia.MaDenominacion))
                        : null
                })
                .ToListAsync();

            return View(usuarios);
        }

        [HttpGet]
        public async Task<IActionResult> UsuarioAgregar()
        {
            ViewBag.RolesList = await _context.Roles.ToListAsync();
            ViewBag.CarreraCohortesList = await _context.CarreraCohortes
                .Include(cc => cc.Carrera)
                .Include(cc => cc.Cohorte)
                .Select(cc => new
                {
                    cc.CaCoId,
                    Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio
                })
                .ToListAsync();
            ViewBag.CarreraMateriasList = await _context.CarreraMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    cm.CaMaId,
                    Denominacion = cm.Carrera.CaDenominacion + " / " + cm.Materia.MaDenominacion
                })
                .ToListAsync();
            return View(new UsuarioCrearDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioAgregar(UsuarioCrearDto model, int selectedRoleId)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RolesList = await _context.Roles.ToListAsync();
                return View(model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni))
            {
                ModelState.AddModelError("UsDni", "El DNI ya se encuentra registrado.");
                ViewBag.RolesList = await _context.Roles.ToListAsync();
                return View(model);
            }

            int nuevoUsId = _context.Usuarios.Any()
                ? await _context.Usuarios.MaxAsync(u => u.UsId) + 1 : 1;

            var usuario = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDni = model.UsDni,
                UsEmail = model.UsEmail,
                UsContrasena = model.UsDni.ToString(),
                RoId = selectedRoleId,
                CaCoId = selectedRoleId == 3 ? model.CaCoId : null
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            if (selectedRoleId == 2 && model.SelectedCaMaIds != null)
            {
                var materias = await _context.CarreraMaterias
                    .Where(cm => model.SelectedCaMaIds.Contains(cm.CaMaId))
                    .ToListAsync();
                foreach (var cm in materias)
                {
                    usuario.CarreraMaterias.Add(cm);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(UsuariosABM));
        }

        [HttpGet]
        public async Task<IActionResult> UsuarioEditar(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null)
                return NotFound();

            ViewBag.RolesList = await _context.Roles.ToListAsync();
            ViewBag.CarreraCohortesList = await _context.CarreraCohortes
                .Include(cc => cc.Carrera)
                .Include(cc => cc.Cohorte)
                .Select(cc => new
                {
                    cc.CaCoId,
                    Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio
                })
                .ToListAsync();
            ViewBag.CarreraMateriasList = await _context.CarreraMaterias
                .Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    cm.CaMaId,
                    Denominacion = cm.Carrera.CaDenominacion + " / " + cm.Materia.MaDenominacion
                })
                .ToListAsync();

            var dto = new UsuarioDetalleDto
            {
                UsId = usuario.UsId,
                UsApellido = usuario.UsApellido,
                UsNombre = usuario.UsNombre,
                UsEmail = usuario.UsEmail,
                UsDni = usuario.UsDni,
                RoId = usuario.RoId,
                RoDenominacion = usuario.Rol?.RoDenominacion,
                CaCoId = usuario.CaCoId,
                MateriasDenominacion = string.Join(",", usuario.CarreraMaterias.Select(cm => cm.CaMaId))
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEditar(int id, UsuarioDetalleDto model, int selectedRoleId, List<int>? selectedCaMaIds)
        {
            if (id != model.UsId)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.RolesList = await _context.Roles.ToListAsync();
                return View(model);
            }

            var usuario = await _context.Usuarios
                .Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null)
                return NotFound();

            usuario.UsApellido = model.UsApellido;
            usuario.UsNombre = model.UsNombre;
            usuario.UsDni = model.UsDni;
            usuario.UsEmail = model.UsEmail;
            usuario.RoId = selectedRoleId;
            usuario.CaCoId = selectedRoleId == 3 ? model.CaCoId : null;

            if (selectedRoleId == 2)
            {
                usuario.CarreraMaterias.Clear();
                if (selectedCaMaIds != null)
                {
                    var materias = await _context.CarreraMaterias
                        .Where(cm => selectedCaMaIds.Contains(cm.CaMaId))
                        .ToListAsync();
                    foreach (var cm in materias)
                    {
                        usuario.CarreraMaterias.Add(cm);
                    }
                }
            }
            else
            {
                usuario.CarreraMaterias.Clear();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(UsuariosABM));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEliminar(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                .Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario != null)
            {
                usuario.CarreraMaterias.Clear();
                _context.UsuarioRoles.RemoveRange(usuario.UsuarioRoles);
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(UsuariosABM));
        }
    }
}

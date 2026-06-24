using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly InstitutoDbContext _context;

        public AdminController(InstitutoDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
                    UsDni = u.UsDNI,
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

        // ALUMNOS - ABM accesible por Admin y Profesor
        [Authorize(Roles = "Admin,Profesor")]
        public async Task<IActionResult> AlumnosABM()
        {
            var alumnos = await _context.Usuarios
                .Where(u => u.RoId == 3)
                .Include(u => u.Rol)
                .Include(u => u.CarreraCohorte).ThenInclude(cc => cc.Carrera)
                .Include(u => u.CarreraCohorte).ThenInclude(cc => cc.Cohorte)
                .Select(u => new UsuarioDetalleDto
                {
                    UsId = u.UsId,
                    UsApellido = u.UsApellido,
                    UsNombre = u.UsNombre,
                    UsEmail = u.UsEmail,
                    UsDni = u.UsDNI,
                    RoId = u.RoId,
                    RoDenominacion = u.Rol != null ? u.Rol.RoDenominacion : null,
                    CaCoId = u.CaCoId,
                    CarreraCohorteDenominacion = u.CaCoId != null && u.CarreraCohorte != null
                        ? u.CarreraCohorte.Carrera.CaDenominacion + " - " + u.CarreraCohorte.Cohorte.CoAnio
                        : null
                })
                .ToListAsync();

            return View(alumnos);
        }

        [Authorize(Roles = "Admin,Profesor")]
        [HttpGet]
        public async Task<IActionResult> AlumnoAgregar()
        {
            ViewBag.CarreraCohortesList = await _context.CarreraCohortes
                .Include(cc => cc.Carrera)
                .Include(cc => cc.Cohorte)
                .Select(cc => new
                {
                    cc.CaCoId,
                    Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio
                })
                .ToListAsync();

            return View(new UsuarioCrearDto());
        }

        [Authorize(Roles = "Admin,Profesor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlumnoAgregar(UsuarioCrearDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CarreraCohortesList = await _context.CarreraCohortes
                    .Include(cc => cc.Carrera)
                    .Include(cc => cc.Cohorte)
                    .Select(cc => new
                    {
                        cc.CaCoId,
                        Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio
                    })
                    .ToListAsync();
                return View(model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.UsDNI == model.UsDni))
            {
                ModelState.AddModelError("UsDni", "El DNI ya se encuentra registrado.");
                ViewBag.CarreraCohortesList = await _context.CarreraCohortes
                    .Include(cc => cc.Carrera)
                    .Include(cc => cc.Cohorte)
                    .Select(cc => new
                    {
                        cc.CaCoId,
                        Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio
                    })
                    .ToListAsync();
                return View(model);
            }

            int nuevoUsId = _context.Usuarios.Any()
                ? await _context.Usuarios.MaxAsync(u => u.UsId) + 1 : 1;

            var usuario = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDNI = model.UsDni,
                UsEmail = model.UsEmail,
                UsContrasena = model.UsDni.ToString(),
                RoId = 3,
                CaCoId = model.CaCoId
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AlumnosABM));
        }

        [Authorize(Roles = "Admin,Profesor")]
        [HttpGet]
        public async Task<IActionResult> AlumnoEditar(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null || usuario.RoId != 3)
                return NotFound();

            ViewBag.CarreraCohortesList = await _context.CarreraCohortes
                .Include(cc => cc.Carrera)
                .Include(cc => cc.Cohorte)
                .Select(cc => new
                {
                    cc.CaCoId,
                    Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio
                })
                .ToListAsync();

            var dto = new UsuarioDetalleDto
            {
                UsId = usuario.UsId,
                UsApellido = usuario.UsApellido,
                UsNombre = usuario.UsNombre,
                UsEmail = usuario.UsEmail,
                UsDni = usuario.UsDNI,
                RoId = usuario.RoId,
                RoDenominacion = usuario.Rol?.RoDenominacion,
                CaCoId = usuario.CaCoId
            };

            return View(dto);
        }

        [Authorize(Roles = "Admin,Profesor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlumnoEditar(int id, UsuarioDetalleDto model)
        {
            if (id != model.UsId)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.CarreraCohortesList = await _context.CarreraCohortes
                    .Include(cc => cc.Carrera)
                    .Include(cc => cc.Cohorte)
                    .Select(cc => new
                    {
                        cc.CaCoId,
                        Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio
                    })
                    .ToListAsync();
                return View(model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsId == id && u.RoId == 3);

            if (usuario == null)
                return NotFound();

            usuario.UsApellido = model.UsApellido;
            usuario.UsNombre = model.UsNombre;
            usuario.UsDNI = model.UsDni;
            usuario.UsEmail = model.UsEmail;
            usuario.CaCoId = model.CaCoId;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AlumnosABM));
        }

        [Authorize(Roles = "Admin,Profesor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlumnoEliminar(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id && u.RoId == 3);

            if (usuario != null)
            {
                usuario.CarreraMaterias.Clear();
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AlumnosABM));
        }

        // Métodos generales de administración (solo Admin)
        [Authorize(Roles = "Admin")]
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
            ViewBag.CarreraMateriasList = await _context.CarrerasMaterias
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioAgregar(UsuarioCrearDto model, int selectedRoleId)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RolesList = await _context.Roles.ToListAsync();
                return View(model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.UsDNI == model.UsDni))
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
                UsDNI = model.UsDni,
                UsEmail = model.UsEmail,
                UsContrasena = model.UsDni.ToString(),
                RoId = selectedRoleId,
                CaCoId = selectedRoleId == 3 ? model.CaCoId : null
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            if (selectedRoleId == 2 && model.SelectedCaMaIds != null)
            {
                var materias = await _context.CarrerasMaterias
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

        [Authorize(Roles = "Admin")]
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
            ViewBag.CarreraMateriasList = await _context.CarrerasMaterias
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
                UsDni = usuario.UsDNI,
                RoId = usuario.RoId,
                RoDenominacion = usuario.Rol?.RoDenominacion,
                CaCoId = usuario.CaCoId,
                MateriasDenominacion = string.Join(",", usuario.CarreraMaterias.Select(cm => cm.CaMaId))
            };

            return View(dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEditar(int id, UsuarioDetalleDto model, List<int>? selectedCaMaIds, int selectedRoleId)
        {
            if (id != model.UsId)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var usuario = await _context.Usuarios
                .Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null)
                return NotFound();

            usuario.UsApellido = model.UsApellido;
            usuario.UsNombre = model.UsNombre;
            usuario.UsDNI = model.UsDni;
            usuario.UsEmail = model.UsEmail;
            usuario.RoId = selectedRoleId;
            usuario.CaCoId = selectedRoleId == 3 ? model.CaCoId : null;

            if (selectedRoleId == 2)
            {
                usuario.CarreraMaterias.Clear();
                if (selectedCaMaIds != null)
                {
                    var materias = await _context.CarrerasMaterias
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

        [Authorize(Roles = "Admin")]
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

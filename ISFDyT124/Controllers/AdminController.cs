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

        // Inyección de dependencias para usar el contexto de la base de datos
        public AdminController(InstitutoDbContext context)
        {
            _context = context;
        }

        // BLOQUE: DASHBOARD / ESTADÍSTICAS
        // Preparamos datos rápidos (contadores) que se mostrarán en la pantalla principal del admin
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

        // BLOQUE: LISTADO DE USUARIOS (ABM)
        // Usamos 'Include' para traer todos los datos relacionados (Roles, Carreras, Materias) en una sola consulta
        // Luego usamos 'Select' para convertir el modelo a un DTO (objeto plano) para mostrarlo seguro en la vista
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

        // BLOQUE: CARGA DE FORMULARIO (GET)
        // Buscamos todas las opciones disponibles (Roles, Carreras, Materias) para llenar los desplegables del formulario
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

        // BLOQUE: GUARDADO DE NUEVO USUARIO (POST)
        // Validamos que el modelo esté bien y que el DNI no exista. 
        // Si es profesor, vinculamos las materias seleccionadas manualmente.
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

            // Calculamos el ID manualmente porque no es autoincremental en la base
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

            // Si es docente (rol 2), asignamos las materias mediante la tabla intermedia
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

        // BLOQUE: CARGA DE EDICIÓN (GET)
        // Cargamos los datos del usuario actual y las listas para el formulario de edición
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

        // BLOQUE: GUARDADO DE EDICIÓN (POST)
        // Actualizamos los datos del usuario. Si es docente, usamos la estrategia de borrar lo anterior (.Clear())
        // y volver a insertar las materias elegidas para evitar duplicados o errores.
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
                usuario.CarreraMaterias.Clear(); // Limpiamos relaciones previas
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
                usuario.CarreraMaterias.Clear(); // Si dejó de ser docente, borramos sus materias
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(UsuariosABM));
        }

        // BLOQUE: ELIMINACIÓN
        // Limpiamos las tablas relacionadas antes de borrar el usuario para que no queden datos "huérfanos"
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
<<<<<<< HEAD
}
=======

    [Authorize(Roles = "Admin")]
    public class AlumnosController : Controller
    {
        private readonly InstitutoDbContext _context;

        public AlumnosController(InstitutoDbContext context)
        {
            _context = context;
        }

        // Listado de Alumnos (Rol ID = 3)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var alumnos = await _context.Usuarios
                .Where(u => u.RoId == 3)
                .Include(u => u.CarreraCohorte).ThenInclude(cc => cc.Carrera)
                .Include(u => u.CarreraCohorte).ThenInclude(cc => cc.Cohorte)
                .Select(u => new UsuarioDetalleDto
                {
                    UsId = u.UsId,
                    UsApellido = u.UsApellido,
                    UsNombre = u.UsNombre,
                    UsEmail = u.UsEmail,
                    UsDni = u.UsDni,
                    RoId = u.RoId,
                    RoDenominacion = "Alumno",
                    CaCoId = u.CaCoId,
                    CarreraCohorteDenominacion = u.CaCoId != null && u.CarreraCohorte != null
                        ? u.CarreraCohorte.Carrera.CaDenominacion + " - " + u.CarreraCohorte.Cohorte.CoAnio
                        : "Sin Asignar"
                })
                .ToListAsync();

            return View(alumnos);
        }

        // GET: Alta de Alumno
        [HttpGet]
        public async Task<IActionResult> Create()
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

        // POST: Alta de Alumno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCrearDto model)
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

            // Validar DNI duplicado
            if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni))
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

            // Calcular IDs manuales secuenciales
            int nuevoUsId = _context.Usuarios.Any() ? await _context.Usuarios.MaxAsync(u => u.UsId) + 1 : 1;
            int nuevoUsRoId = _context.UsuarioRoles.Any() ? await _context.UsuarioRoles.MaxAsync(ur => ur.UsRoId) + 1 : 1;

            var usuario = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDni = model.UsDni,
                UsEmail = model.UsEmail,
                UsContrasena = model.UsDni.ToString(), // Contraseña por defecto (DNI)
                RoId = 3, // Alumno
                CaCoId = model.CaCoId
            };

            var usuarioRol = new UsuarioRol
            {
                UsRoId = nuevoUsRoId,
                UsId = nuevoUsId,
                RoId = 3
            };

            _context.Usuarios.Add(usuario);
            _context.UsuarioRoles.Add(usuarioRol);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Alumno registrado con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Modificación de Alumno
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var alumno = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsId == id && u.RoId == 3);

            if (alumno == null)
            {
                return NotFound();
            }

            var model = new UsuarioDetalleDto
            {
                UsId = alumno.UsId,
                UsApellido = alumno.UsApellido,
                UsNombre = alumno.UsNombre,
                UsDni = alumno.UsDni,
                UsEmail = alumno.UsEmail,
                CaCoId = alumno.CaCoId
            };

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

        // POST: Modificación de Alumno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioDetalleDto model)
        {
            if (id != model.UsId)
            {
                return BadRequest();
            }

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

            var alumno = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsId == id && u.RoId == 3);

            if (alumno == null)
            {
                return NotFound();
            }

            // Validar DNI duplicado
            if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni && u.UsId != id))
            {
                ModelState.AddModelError("UsDni", "El DNI ya se encuentra registrado en otro usuario.");
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

            alumno.UsApellido = model.UsApellido;
            alumno.UsNombre = model.UsNombre;
            alumno.UsDni = model.UsDni;
            alumno.UsEmail = model.UsEmail;
            alumno.CaCoId = model.CaCoId;

            _context.Update(alumno);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Datos del alumno actualizados con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Eliminación de Alumno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var alumno = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsId == id && u.RoId == 3);

            if (alumno != null)
            {
                var roles = _context.UsuarioRoles.Where(ur => ur.UsId == id);
                _context.UsuarioRoles.RemoveRange(roles);

                _context.Usuarios.Remove(alumno);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Alumno eliminado con éxito.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
>>>>>>> Grupo-2

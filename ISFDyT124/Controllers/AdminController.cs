using ISFDyT124.Data;
using ISFDyT124.Models;
using ISFDyT124.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    /// <summary>
    /// Controlador responsable de las acciones administrativas del sistema.
    /// Solo los usuarios con rol de Administrador o Preceptor deberían acceder a estas funciones.
    /// </summary>
    public class AdminController : Controller
    {
        private readonly InstitutoDbContext _context;

        // Constructor del controlador con inyección de dependencias del DbContext de la aplicación
        public AdminController(InstitutoDbContext context)
        {
            _context = context;
        }

        #region SECTION 1: DASHBOARD ADMINISTRATIVO (MÉTRICAS)

        /// <summary>
        /// Vista principal del panel de administración.
        /// Muestra estadísticas generales de la institución basadas en la base de datos SQL.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // 1. Obtener la cantidad total de alumnos (usuarios con el rol de Alumno - ID 3)
            ViewBag.TotalAlumnos = await _context.Usuarios
                .Where(u => u.RoId == 3)
                .CountAsync();

            // 2. Obtener la cantidad total de docentes (usuarios con el rol de Docente - ID 2)
            ViewBag.TotalDocentes = await _context.Usuarios
                .Where(u => u.RoId == 2)
                .CountAsync();

            // 3. Cantidad total de materias activas (Simulado, al no existir modelo Materia)
            ViewBag.TotalMaterias = 0;

            // 4. Cantidad total de carreras dictadas (Simulado, al no existir modelo Carrera)
            ViewBag.TotalCarreras = 0;

            // 5. Asistencias totales registradas (Simulado, al no existir modelo Asistencia)
            ViewBag.TotalAsistencias = 0;

            // 6. Asistencias del día actual (Simulado)
            ViewBag.AsistenciasHoy = 0;

            return View();
        }

        #endregion

        #region SECTION 2: GESTIÓN DE USUARIOS (ABM / CRUD)

        /// <summary>
        /// Muestra el listado completo de usuarios registrados en el sistema,
        /// mapeados a UsuarioDetalleDto.
        /// </summary>
        public async Task<IActionResult> UsuariosABM()
        {
            // Consultamos todos los usuarios cargando eager-loading sus roles para evitar consultas N+1
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Select(u => new UsuarioDetalleDto
                {
                    UsId = u.UsId,
                    UsApellido = u.UsApellido,
                    UsNombre = u.UsNombre,
                    UsEmail = u.UsEmail,
                    UsDni = u.UsDni,
                    RoId = u.RoId,
                    RoDenominacion = u.Rol != null ? u.Rol.RoDenominacion : null
                })
                .ToListAsync();

            return View(usuarios);
        }

        /// <summary>
        /// Acción GET que renderiza el formulario para registrar un nuevo usuario en la base de datos.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UsuarioAgregar()
        {
            // Cargamos la lista de roles disponibles de la base de datos para mostrarlos en un Dropdown en la vista
            ViewBag.RolesList = await _context.Roles.ToListAsync();
            return View();
        }

        /// <summary>
        /// Acción POST que procesa la creación de un nuevo usuario usando UsuarioCrearDto.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioAgregar(UsuarioCrearDto dto)
        {
            if (ModelState.IsValid)
            {
                // Verificar que el DNI no esté duplicado en el sistema
                var dniExiste = await _context.Usuarios.AnyAsync(u => u.UsDni == dto.UsDni);
                if (dniExiste)
                {
                    ModelState.AddModelError("UsDni", "El DNI ingresado ya se encuentra registrado.");
                    ViewBag.RolesList = await _context.Roles.ToListAsync();
                    return View(dto);
                }

                // CÁLCULO DE PRIMARY KEYS MANUALES (Ya que no son autoincrementales en el script SQL):
                int nuevoUsId = _context.Usuarios.Any() ? await _context.Usuarios.MaxAsync(u => u.UsId) + 1 : 1;

                var usuario = new Usuario
                {
                    UsId = nuevoUsId,
                    UsApellido = dto.UsApellido ?? string.Empty,
                    UsNombre = dto.UsNombre ?? string.Empty,
                    UsEmail = dto.UsEmail,
                    UsDni = dto.UsDni,
                    RoId = dto.RoId
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(UsuariosABM));
            }

            // Si falla la validación del modelo, recargamos la lista de roles
            ViewBag.RolesList = await _context.Roles.ToListAsync();
            return View(dto);
        }

        /// <summary>
        /// GET para editar los datos personales de un usuario por su UsId.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UsuarioEditar(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null)
            {
                return NotFound();
            }

            var dto = new UsuarioDetalleDto
            {
                UsId = usuario.UsId,
                UsApellido = usuario.UsApellido,
                UsNombre = usuario.UsNombre,
                UsEmail = usuario.UsEmail,
                UsDni = usuario.UsDni,
                RoId = usuario.RoId,
                RoDenominacion = usuario.Rol != null ? usuario.Rol.RoDenominacion : null
            };

            // Pasar roles y el rol actual del usuario a la vista
            ViewBag.RolesList = await _context.Roles.ToListAsync();
            ViewBag.CurrentRoleId = usuario.RoId;

            return View(dto);
        }

        /// <summary>
        /// POST que procesa la actualización de un usuario existente.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEditar(int id, UsuarioDetalleDto dto)
        {
            if (id != dto.UsId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var usuario = await _context.Usuarios.FindAsync(id);
                    if (usuario == null)
                    {
                        return NotFound();
                    }

                    // Verificar DNI duplicado
                    var dniExisteEnOtro = await _context.Usuarios.AnyAsync(u => u.UsDni == dto.UsDni && u.UsId != dto.UsId);
                    if (dniExisteEnOtro)
                    {
                        ModelState.AddModelError("UsDni", "El DNI ingresado ya se encuentra registrado en otro usuario.");
                        ViewBag.RolesList = await _context.Roles.ToListAsync();
                        ViewBag.CurrentRoleId = usuario.RoId;
                        return View(dto);
                    }

                    usuario.UsApellido = dto.UsApellido ?? string.Empty;
                    usuario.UsNombre = dto.UsNombre ?? string.Empty;
                    usuario.UsEmail = dto.UsEmail;
                    usuario.UsDni = dto.UsDni;
                    usuario.RoId = dto.RoId;

                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Usuarios.Any(u => u.UsId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(UsuariosABM));
            }

            ViewBag.RolesList = await _context.Roles.ToListAsync();
            ViewBag.CurrentRoleId = dto.RoId;
            return View(dto);
        }

        /// <summary>
        /// POST para eliminar físicamente un usuario.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEliminar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(UsuariosABM));
        }

        #endregion
    }
}

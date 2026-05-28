// using ISFDyT124.Models;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.CodeAnalysis.Scripting;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
//
// namespace ISFDyT124.Controllers
// {
//     /// <summary>
//     /// Controlador responsable de las acciones administrativas del sistema.
//     /// Solo los usuarios con rol de Administrador o Preceptor deberían acceder a estas funciones.
//     /// </summary>
//     public class AdminController : Controller
//     {
//         private readonly InstitutoDbContext _context;
//
//         // Constructor del controlador con inyección de dependencias del DbContext de la aplicación
//         public AdminController(InstitutoDbContext context)
//         {
//             _context = context;
//         }
//
//         #region SECTION 1: DASHBOARD ADMINISTRATIVO (MÉTRICAS)
//
//         /// <summary>
//         /// Vista principal del panel de administración.
//         /// Muestra estadísticas generales de la institución basadas en la base de datos SQL.
//         /// </summary>
//         public async Task<IActionResult> Index()
//         {
//             // 1. Obtener la cantidad total de alumnos (usuarios que tienen el rol de Alumno - ID 3)
//             ViewBag.TotalAlumnos = await _context.UsuarioRoles
//                 .Where(ur => ur.RoId == 3)
//                 .Select(ur => ur.UsId)
//                 .Distinct()
//                 .CountAsync();
//
//             // 2. Obtener la cantidad total de docentes (usuarios con el rol de Docente - ID 2)
//             ViewBag.TotalDocentes = await _context.UsuarioRoles
//                 .Where(ur => ur.RoId == 2)
//                 .Select(ur => ur.UsId)
//                 .Distinct()
//                 .CountAsync();
//
//             // 3. Cantidad total de materias activas creadas en el sistema
//             ViewBag.TotalMaterias = await _context.Materias.CountAsync();
//
//             // 4. Cantidad total de carreras dictadas en el instituto
//             ViewBag.TotalCarreras = await _context.Carreras.CountAsync();
//
//             // 5. Asistencias totales registradas en el sistema para estadísticas de ausentismo
//             ViewBag.TotalAsistencias = await _context.Asistencias.CountAsync();
//
//             // 6. Asistencias del día actual
//             var hoy = DateTime.Today;
//             ViewBag.AsistenciasHoy = await _context.Asistencias
//                 .Where(a => a.AsFecha.Date == hoy)
//                 .CountAsync();
//
//             return View();
//         }
//
//         #endregion
//
//         #region SECTION 2: GESTIÓN DE USUARIOS (ABM / CRUD)
//
//         /// <summary>
//         /// Muestra el listado completo de usuarios registrados en el sistema,
//         /// incluyendo sus respectivos roles asociados mediante la tabla intermedia.
//         /// </summary>
//         public async Task<IActionResult> UsuariosABM()
//         {
//             // Consultamos todos los usuarios cargando eager-loading sus roles para evitar consultas N+1
//             var usuarios = await _context.Usuarios
//                 .Include(u => u.UsuarioRoles)
//                     .ThenInclude(ur => ur.Rol)
//                 .ToListAsync();
//
//             return View(usuarios);
//         }
//
//         /// <summary>
//         /// Acción GET que renderiza el formulario para registrar un nuevo usuario en la base de datos.
//         /// </summary>
//         [HttpGet]
//         public async Task<IActionResult> UsuarioAgregar()
//         {
//             // Cargamos la lista de roles disponibles de la base de datos para mostrarlos en un Dropdown en la vista
//             ViewBag.RolesList = await _context.Roles.ToListAsync();
//             return View();
//         }
//
//         /// <summary>
//         /// Acción POST que procesa la creación de un nuevo usuario, su asignación de rol inicial y credenciales opcionales.
//         /// Respeta las propiedades físicas del esquema SQL (UsId, UsApellido, UsNombre, UsDNI).
//         /// </summary>
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> UsuarioAgregar(Usuario usuario, int selectedRoleId, string userPassword)
//         {
//             if (ModelState.IsValid)
//             {
//                 // Verificar que el DNI no esté duplicado en el sistema
//                 var dniExiste = await _context.Usuarios.AnyAsync(u => u.UsDNI == usuario.UsDNI);
//                 if (dniExiste)
//                 {
//                     ModelState.AddModelError("UsDNI", "El DNI ingresado ya se encuentra registrado.");
//                     ViewBag.RolesList = await _context.Roles.ToListAsync();
//                     return View(usuario);
//                 }
//
//                 // CÁLCULO DE PRIMARY KEYS MANUALES (Ya que no son autoincrementales en el script SQL):
//                 // 1. Obtener el siguiente ID para la tabla USUARIOS
//                 int nuevoUsId = _context.Usuarios.Any() ? await _context.Usuarios.MaxAsync(u => u.UsId) + 1 : 1;
//                 usuario.UsId = nuevoUsId;
//
//                 // Agregar el usuario principal
//                 _context.Usuarios.Add(usuario);
//
//                 // 2. Crear la relación en la tabla intermedia USUARIOS_ROLES
//                 int nuevoUsRoId = _context.UsuarioRoles.Any() ? await _context.UsuarioRoles.MaxAsync(ur => ur.UsRoId) + 1 : 1;
//                 var usuarioRol = new UsuarioRol
//                 {
//                     UsRoId = nuevoUsRoId,
//                     UsId = nuevoUsId,
//                     RoId = selectedRoleId
//                 };
//                 _context.UsuarioRoles.Add(usuarioRol);
//
//                 // 3. Si se proporciona contraseña, creamos sus credenciales en la tabla LOGIN
//                 if (!string.IsNullOrEmpty(userPassword))
//                 {
//                     int nuevoLoId = _context.Logins.Any() ? await _context.Logins.MaxAsync(l => l.LoId) + 1 : 1;
//                     var login = new Login
//                     {
//                         LoId = nuevoLoId,
//                         LoUser = nuevoUsId,
//                         LoPass = BCrypt.Net.BCrypt.HashPassword(userPassword) // Cifrado seguro de la contraseña
//                     };
//                     _context.Logins.Add(login);
//                 }
//
//                 // Guardar todos los cambios transaccionales en lote
//                 await _context.SaveChangesAsync();
//                 return RedirectToAction(nameof(UsuariosABM));
//             }
//
//             // Si falla la validación del modelo, recargamos la lista de roles
//             ViewBag.RolesList = await _context.Roles.ToListAsync();
//             return View(usuario);
//         }
//
//         /// <summary>
//         /// GET para editar los datos personales de un usuario por su UsId.
//         /// </summary>
//         [HttpGet]
//         public async Task<IActionResult> UsuarioEditar(int id)
//         {
//             var usuario = await _context.Usuarios
//                 .Include(u => u.UsuarioRoles)
//                 .FirstOrDefaultAsync(u => u.UsId == id);
//
//             if (usuario == null)
//             {
//                 return NotFound();
//             }
//
//             // Pasar roles y el rol actual del usuario a la vista
//             ViewBag.RolesList = await _context.Roles.ToListAsync();
//             ViewBag.CurrentRoleId = usuario.UsuarioRoles.FirstOrDefault()?.RoId;
//
//             return View(usuario);
//         }
//
//         /// <summary>
//         /// POST que procesa la actualización de un usuario existente.
//         /// </summary>
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> UsuarioEditar(int id, Usuario usuario, int selectedRoleId)
//         {
//             if (id != usuario.UsId)
//             {
//                 return BadRequest();
//             }
//
//             if (ModelState.IsValid)
//             {
//                 try
//                 {
//                     _context.Update(usuario);
//
//                     // Actualizar también su asignación de rol en USUARIOS_ROLES si este cambió
//                     var actualRolRel = await _context.UsuarioRoles
//                         .FirstOrDefaultAsync(ur => ur.UsId == id);
//
//                     if (actualRolRel != null)
//                     {
//                         if (actualRolRel.RoId != selectedRoleId)
//                         {
//                             actualRolRel.RoId = selectedRoleId;
//                             _context.Update(actualRolRel);
//                         }
//                     }
//                     else
//                     {
//                         // En caso de que el usuario no tuviera rol asociado, le creamos uno
//                         int nuevoUsRoId = _context.UsuarioRoles.Any() ? await _context.UsuarioRoles.MaxAsync(ur => ur.UsRoId) + 1 : 1;
//                         _context.UsuarioRoles.Add(new UsuarioRol
//                         {
//                             UsRoId = nuevoUsRoId,
//                             UsId = id,
//                             RoId = selectedRoleId
//                         });
//                     }
//
//                     await _context.SaveChangesAsync();
//                 }
//                 catch (DbUpdateConcurrencyException)
//                 {
//                     if (!_context.Usuarios.Any(u => u.UsId == id))
//                     {
//                         return NotFound();
//                     }
//                     else
//                     {
//                         throw;
//                     }
//                 }
//                 return RedirectToAction(nameof(UsuariosABM));
//             }
//
//             ViewBag.RolesList = await _context.Roles.ToListAsync();
//             return View(usuario);
//         }
//
//         /// <summary>
//         /// POST para eliminar físicamente un usuario, removiendo sus claves foráneas.
//         /// </summary>
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> UsuarioEliminar(int id)
//         {
//             var usuario = await _context.Usuarios.FindAsync(id);
//             if (usuario != null)
//             {
//                 // EF Core se encargará de cascada si está configurado,
//                 // sino removemos manualmente registros dependientes de LOGIN y USUARIOS_ROLES
//                 var logins = _context.Logins.Where(l => l.LoUser == id);
//                 _context.Logins.RemoveRange(logins);
//
//                 var roles = _context.UsuarioRoles.Where(ur => ur.UsId == id);
//                 _context.UsuarioRoles.RemoveRange(roles);
//
//                 _context.Usuarios.Remove(usuario);
//                 await _context.SaveChangesAsync();
//             }
//             return RedirectToAction(nameof(UsuariosABM));
//         }
//
//         #endregion
//
//         #region SECTION 3: JUSTIFICACIÓN DE INASISTENCIAS
//
//         /// <summary>
//         /// Acción POST administrativa que permite a un preceptor o directivo justificar una inasistencia (AsJustificacion = true).
//         /// Esto se realiza a partir de un certificado presentado por el alumno.
//         /// </summary>
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> JustificarAsistencia(int asistenciaId, bool justificar)
//         {
//             var asistencia = await _context.Asistencias.FindAsync(asistenciaId);
//             if (asistencia == null)
//             {
//                 return NotFound("Registro de asistencia no encontrado.");
//             }
//
//             // Cambiamos el estado de justificación (AsJustificacion en el modelo SQL)
//             asistencia.AsJustificacion = justificar;
//
//             _context.Update(asistencia);
//             await _context.SaveChangesAsync();
//
//             // Retornamos un mensaje de éxito para manejar en el frontend o redirigimos al historial
//             TempData["SuccessMessage"] = justificar ? "Inasistencia justificada correctamente." : "Se removió la justificación de la inasistencia.";
//
//             return RedirectToAction("HistorialAsistencias", "Profesor");
//         }
//
//         #endregion
//     }
// }


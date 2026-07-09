using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ISFDyT124.Controllers
{
    public class AccountController : Controller
    {
        // 1. BASE DE DATOS: Declaramos la conexión. Mantuve InstitutoDbContext, modificalo si usás SiAsContext.
        private readonly InstitutoDbContext _context;

        public AccountController(InstitutoDbContext context)
        {
            _context = context;
        }

        // 2. VISTA GET: Devuelve la pantalla de login inicial.
        public IActionResult Login()
        {
            return View();
        }

        // 3. RECIBIR DATOS POST: Se ejecuta al enviar el formulario. Usamos el DTO por buenas prácticas.
        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 4. BUSCAR AL USUARIO: Buscamos por email e incluimos el Rol.
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsEmail == model.Usuario);

            if (!int.TryParse(model.Usuario, out int dniEntero))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El usuario ingresado debe ser un número de DNI válido."
                );
                return View(model);
            }

            var usuarioBD = await _context
                .Usuarios.Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsDni == dniEntero && u.UsContrasena == model.Contrasena);


            // 5. VALIDAR CREDENCIALES: Verificamos que el usuario exista Y que la contraseña coincida.
            if (usuario == null || usuario.UsContrasena != model.Contrasena)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return View(model);
            }

            // 6. CREAR CLAIMS (Tarjeta de identificación): Combinamos los datos de ambos códigos.
            var claims = new List<Claim>
            {

                new Claim(ClaimTypes.NameIdentifier, $"{usuario.UsId}"),
                new Claim(ClaimTypes.Name, $"{usuario.UsNombre} {usuario.UsApellido}"),
                new Claim(ClaimTypes.Email, $"{usuario.UsEmail}"),
                new Claim(ClaimTypes.Role, $"{usuario.Rol?.RoDenominacion}"),

            };

            // 7. INICIAR SESIÓN: Creamos la cookie segura.
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 8. REDIRECCIÓN PARTE A: Si la contraseña es igual al DNI, forzamos el cambio.
            if (usuario.UsDni.ToString() == usuario.UsContrasena)
                return RedirectToAction("CambiarContrasena");

            // 9. REDIRECCIÓN PARTE B: Si no tuvo que cambiar la clave, lo mandamos a su panel según el rol.
            switch (usuario.Rol?.RoDenominacion?.ToUpper())
            {
                case "ADMIN":
                    return RedirectToAction("Index", "Admin");
                case "PROFESOR":
                    return RedirectToAction("Index", "Profesor");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        // -----------------------------------------------------------
        // MÉTODOS MANTENIDOS DEL PRIMER CONTROLADOR
        // -----------------------------------------------------------

        [Authorize]
        public IActionResult CambiarContrasena()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(string nuevaContrasena, string confirmarContrasena)
        {
            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length < 6)
            {
                ModelState.AddModelError("", "La contraseña debe tener al menos 6 caracteres.");
                return View();
            }

            if (nuevaContrasena != confirmarContrasena)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View();
            }

            // Buscamos el usuario logueado usando el Claim del ID
            var usuario = await _context.Usuarios.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
            if (usuario == null)
                return RedirectToAction("Salir");

            usuario.UsContrasena = nuevaContrasena;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}

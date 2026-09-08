using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    public class AccountController : Controller
    {
        // BASE DE DATOS: Declaramos la conexión. Mantuve InstitutoDbContext, modificalo si usás SiAsContext.
        private readonly InstitutoDbContext _context;

        public AccountController(InstitutoDbContext context)
        {
            _context = context;
        }

        // VISTA GET: Devuelve la pantalla de login inicial.
        public IActionResult Login()
        {
            return View();
        }

        // RECIBIR DATOS POST: Se ejecuta al enviar el formulario. Usamos el DTO por buenas prácticas.
        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // VALIDAR FORMATO: el usuario se identifica con su número de DNI.
            if (!int.TryParse(model.Usuario, out int dniEntero))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El usuario ingresado debe ser un número de DNI válido."
                );
                return View(model);
            }

            // CAMBIO: antes se comparaba la contraseña en texto plano dentro del propio
            // WHERE de la consulta. Con contraseñas hasheadas eso ya no es posible: cada
            // hash tiene un salt aleatorio distinto, así que dos contraseñas iguales dan
            // hashes distintos y SQL no puede compararlos directamente. Por eso ahora se
            // busca solo por DNI y la contraseña se verifica después, en memoria.
            // BUSCAR CREDENCIALES: por DNI, incluyendo el Rol.
            var usuario = await _context
                .Usuarios.Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsDni == dniEntero);

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return View(model);
            }

            // CAMBIO (TRANSITORIO): migración automática de contraseñas viejas.
            // La base todavía tiene usuarios con la contraseña en texto plano, de antes
            // de implementar el hasheo. Para esos casos VerifyPassword da false (el texto
            // plano no es un hash válido), así que se compara como texto plano por única
            // vez: si coincide, se acepta el login y en ese mismo momento se reemplaza el
            // valor por su hash real. Así cada usuario se auto-migra la primera vez que
            // entra, sin necesidad de correr ningún script sobre la base compartida.
            // Cuando ya no queden contraseñas en texto plano, este bloque se puede borrar.
            if (!PasswordService.VerifyPassword(model.Contrasena, usuario.UsContrasena))
            {
                if (usuario.UsContrasena == model.Contrasena)
                {
                    usuario.UsContrasena = PasswordService.HashPassword(model.Contrasena);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                    return View(model);
                }
            }

            // CREAR CLAIMS (Tarjeta de identificación): Combinamos los datos de ambos códigos.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, $"{usuario.UsId}"),
                new Claim(ClaimTypes.Name, $"{usuario.UsNombre} {usuario.UsApellido}"),
                new Claim(ClaimTypes.Email, $"{usuario.UsEmail}"),
                new Claim(ClaimTypes.Role, $"{usuario.Rol?.RoDenominacion}"),
            };

            // INICIAR SESIÓN: Creamos la cookie segura.
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            // CAMBIO: ya no se puede comparar el DNI contra el hash guardado como strings
            // (el hash del DNI no es igual al DNI). En su lugar, se verifica si el DNI
            // "verifica" contra el hash almacenado, es decir, si la contraseña actual del
            // usuario sigue siendo la default (su propio DNI) asignada al crear la cuenta.
            // REDIRECCIÓN PARTE A: Si la contraseña sigue siendo el DNI, forzamos el cambio.
            if (PasswordService.VerifyPassword(usuario.UsDni.ToString(), usuario.UsContrasena))
                return RedirectToAction("CambiarContrasena");

            // REDIRECCIÓN PARTE B: Si no tuvo que cambiar la clave, lo mandamos a su panel según el rol.
            switch (usuario.Rol?.RoDenominacion?.ToUpper())
            {
                case "ADMIN":
                    return RedirectToAction("Index", "Admin");
                case "DOCENTE":
                    return RedirectToAction("Index", "Profesor");
                case "DIRECCIÓN":
                    return RedirectToAction("Index", "Admin"); //Por ahora; hasta que definamos si tendrá una vista aparte, o permisos especiales.
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
        public async Task<IActionResult> CambiarContrasena(
            string nuevaContrasena,
            string confirmarContrasena
        )
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
            var usuario = await _context.Usuarios.FindAsync(
                int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
            );
            if (usuario == null)
                return RedirectToAction("Salir");

            // CAMBIO: la contraseña nueva se guarda hasheada, nunca en texto plano.
            usuario.UsContrasena = PasswordService.HashPassword(nuevaContrasena);
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

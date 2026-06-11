using ISFDyT124.Data;
using ISFDyT124.DTO;
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
        private readonly InstitutoDbContext _context;

        public AccountController(InstitutoDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsEmail == model.Usuario);

            if (usuario == null || usuario.UsContrasena != model.Contrasena)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsId.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.UsNombre} {usuario.UsApellido}"),
                new Claim(ClaimTypes.Email, usuario.UsEmail ?? ""),
                new Claim(ClaimTypes.Role, usuario.Rol?.RoDenominacion ?? ""),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (usuario.UsDni.ToString() == usuario.UsContrasena)
                return RedirectToAction("CambiarContrasena");

            return RedirectToAction("Index", "Home");
        }

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

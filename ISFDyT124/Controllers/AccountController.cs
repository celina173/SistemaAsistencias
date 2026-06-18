using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using ISFDyT124.Models;

namespace ISFDyT124.Controllers
{
    public class AccountController : Controller
    {
        private readonly InstitutoDbContext _context;

        public AccountController(InstitutoDbContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!int.TryParse(model.Usuario, out int dniEntero))
            {
                ModelState.AddModelError(string.Empty, "El usuario ingresado debe ser un número de DNI válido.");
                return View(model);
            }


            var usuarioBD = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsDni == dniEntero && u.UsPassword == model.Contrasena);

            if (usuarioBD != null)
            {

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuarioBD.UsId.ToString()),
                    new Claim(ClaimTypes.Name, $"{usuarioBD.UsNombre} {usuarioBD.UsApellido}"),
                    new Claim(ClaimTypes.Email, usuarioBD.UsEmail ?? "")
                };

                if (usuarioBD.Rol != null && !string.IsNullOrEmpty(usuarioBD.Rol.RoDenominacion))
                {
                    claims.Add(new Claim(ClaimTypes.Role, usuarioBD.Rol.RoDenominacion));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);


                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));


                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "El DNI o la contraseña son incorrectos.");
            return View(model);
        }

        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}

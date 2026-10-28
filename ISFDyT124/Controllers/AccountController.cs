using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using ISFDyT124.Models.ViewModels;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting; // Necesario para EnableRateLimiting
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace ISFDyT124.Controllers
{
    public class AccountController : Controller
    {
        private readonly InstitutoDbContext _context;
        private readonly IEmailRateLimiter _emailRateLimiter; // Servicio de limitación por correo

        private const string TokenBloqueado = "tokenbloqueado";
        private const int MinutosVigenciaToken = 30;

        public AccountController(
            InstitutoDbContext context,
            IEmailRateLimiter emailRateLimiter)
        {
            _context = context;
            _emailRateLimiter = emailRateLimiter;
        }

        // VISTA GET: Devuelve la pantalla de login inicial.
        // Recibe el flag cuando el middleware de RateLimiting redirige por exceder los intentos.
        [HttpGet]
        public IActionResult Login([FromQuery] bool limiteExcedido = false)
        {
            if (limiteExcedido)
            {
                ViewBag.Error = "Superaste el límite de intentos. Por favor, espera unos minutos antes de volver a intentar.";
            }

            return View();
        }

        // RECIBIR DATOS POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!int.TryParse(model.Usuario, out int dniEntero))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El usuario ingresado debe ser un número de DNI válido."
                );
                return View(model);
            }

            var usuario = await _context
                .Usuarios.Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsDni == dniEntero);

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return View(model);
            }

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

            // El Estudiante no tiene ninguna pantalla propia en el sistema: hasta ahora el
            // login no miraba el rol y lo dejaba entrar, y después cada pantalla lo mandaba a
            // /Home/AccessDenied (que no existe), así que veía un error en cualquier botón.
            // Se rechaza con el MISMO mensaje genérico de credenciales incorrectas, y recién
            // después de validar la contraseña: así quien no la conoce no puede usar este
            // login para descubrir qué DNI son de estudiantes. Va antes de crear la cookie,
            // para que tampoco llegue a CambiarContrasena (que exige sesión abierta).
            if (usuario.RoId == RolId.Estudiante)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, $"{usuario.UsId}"),
                new Claim(ClaimTypes.Name, $"{usuario.UsNombre} {usuario.UsApellido}"),
                new Claim(ClaimTypes.Email, $"{usuario.UsEmail}"),
                new Claim(ClaimTypes.Role, $"{usuario.Rol?.RoDenominacion}"),
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true }
            );

            if (PasswordService.VerifyPassword(usuario.UsDni.ToString(), usuario.UsContrasena))
                return RedirectToAction("CambiarContrasena");

            return RedirectPorRol(usuario.Rol?.RoDenominacion);
        }

        private IActionResult RedirectPorRol(string? roDenominacion)
        {
            switch (roDenominacion?.ToUpper())
            {
                case "ADMIN":
                    return RedirectToAction("Index", "Admin");
                case "DOCENTE":
                    return RedirectToAction("Index", "Profesor");
                case "DIRECCIÓN":
                    return RedirectToAction("Index", "Admin");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        public IActionResult CambiarContrasena()
        {
            return View(new CambiarContrasenaDto());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(
            CambiarContrasenaDto model,
            string contrasenaActual
        )
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = await _context
                .Usuarios.Include(u => u.Rol)
                .FirstOrDefaultAsync(u =>
                    u.UsId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                );

            if (usuario == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            if (!PasswordService.VerifyPassword(contrasenaActual, usuario.UsContrasena))
            {
                ModelState.AddModelError("", "La contraseña actual no es correcta.");
                return View(model);
            }

            usuario.UsContrasena = PasswordService.HashPassword(model.NuevaContrasena);
            await _context.SaveChangesAsync();

            return RedirectPorRol(usuario.Rol?.RoDenominacion);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ===========================================================
        // RECUPERACIÓN DE CONTRASEÑA (MITIGACIÓN QA-07 Y QA-16)
        // ===========================================================

        [HttpGet]
        public ActionResult StartRecovery([FromQuery] bool limiteExcedido = false)
        {
            if (limiteExcedido)
            {
                ViewBag.Error = "Límite de intentos superados. Por favor, aguarda unos minutos antes de volver a intentar.";
            }

            RecoveryViewModel model = new RecoveryViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("ForgotPasswordLimiter")] // QA-16: Rate limiting por IP
        public async Task<IActionResult> StartRecovery(RecoveryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // QA-16: Verifica cupo para esta casilla
            bool emailPermitido = _emailRateLimiter.IsAllowed(model.UsEmail);

            // Al superar el límite: no redirige al login, permanece en la vista con mensaje de error
            if (!emailPermitido)
            {
                ViewBag.Error = "Límite de intentos superados. Por favor, aguarda unos minutos antes de volver a intentar.";
                return View(model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsEmail == model.UsEmail);

            // Si existe el usuario, generamos token y despachamos email
            if (usuario != null)
            {
                var token = Guid.NewGuid().ToString("N");

                usuario.UsTokenRecovery = token;
                usuario.UsTokenRecoveryVencimiento = DateTime.UtcNow.AddMinutes(MinutosVigenciaToken);
                _context.Entry(usuario).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                try
                {
                    Sendemail(usuario.UsEmail, token);
                }
                catch
                {
                    // Se captura cualquier excepción SMTP para que no delate la existencia
                    // del usuario ni rompa la respuesta al cliente.
                }
            }

            // QA-07: Mensaje neutral y redirección al login solo cuando no se superó el límite
            TempData["MensajeExito"] = "Si el correo electrónico está registrado en el sistema, recibirás un enlace de recuperación en los próximos minutos.";
            return RedirectToAction("Login");
        }

        private async Task<Usuario?> BuscarUsuarioPorTokenVigenteAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token) || token == TokenBloqueado)
                return null;

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsTokenRecovery == token
                                       && u.UsTokenRecoveryVencimiento != null);

            if (usuario is null || usuario.UsTokenRecoveryVencimiento < DateTime.UtcNow)
                return null;

            return usuario;
        }

        [HttpGet]
        public async Task<IActionResult> Recovery(string token)
        {
            var usuario = await BuscarUsuarioPorTokenVigenteAsync(token);

            if (usuario is null)
            {
                TempData["Error"] = "El enlace de recuperación es inválido o ha expirado.";
                return RedirectToAction("StartRecovery");
            }

            var model = new RecoveryPasswordViewModel
            {
                UsTokenRecovery = token
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recovery(RecoveryPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.UsContrasena != model.UsContrasena2)
            {
                ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
                return View(model);
            }

            var usuario = await BuscarUsuarioPorTokenVigenteAsync(model.UsTokenRecovery);

            if (usuario is null)
            {
                TempData["Error"] = "Token inválido o expirado. Solicite un nuevo enlace de recuperación.";
                return RedirectToAction("StartRecovery");
            }

            usuario.UsContrasena = PasswordService.HashPassword(model.UsContrasena!);
            usuario.UsTokenRecovery = TokenBloqueado;
            usuario.UsTokenRecoveryVencimiento = null;

            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "Contraseña modificada con éxito. Ya puede iniciar sesión.";
            return RedirectToAction("Login");
        }

        private void Sendemail(string EmailDestino, string token)
        {
            var url = Url.Action("Recovery", "Account", new { token = token }, Request.Scheme);
            var urlEscaped = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(url ?? string.Empty);

            var oMailMessage = new MailMessage(
                "softwareinstituto@gmail.com",
                EmailDestino,
                "Restablecimiento de contraseña – Sistema de asistencias - ISFDyT124",
                $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 650px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #004d80;'>Solicitud de restablecimiento de contraseña</h2>
                        <p>Estimado/a usuario/a:</p>
                        <p>Recibimos una solicitud para restablecer la contraseña de su cuenta en el <strong>Sistema de control de asistencias del instituto ISFDyT124</strong>.</p>
                        <p>Si usted realizó esta solicitud, haga clic en el siguiente enlace para crear una nueva contraseña:</p>
                        <div style='text-align: center; margin: 25px 0;'>
                            <a href='{urlEscaped}'
                               style='display: inline-block; padding: 12px 24px; background-color: #004d80; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                Restablecer mi contraseña
                            </a>
                        </div>
                        <p>Este enlace es válido por una sola vez y expirará en 30 minutos.</p>
                        <p><strong>¿No solicitó este cambio?</strong> Si usted no ha solicitado restablecer su contraseña, por favor ignore este mensaje. Su cuenta permanecerá segura.</p>
                        <p>Para cualquier duda o asistencia adicional, no dude en contactar al equipo de soporte técnico.</p>
                        <hr style='border: 0; border-top: 1px solid #eee; margin: 30px 0;' />
                        <p style='font-size: 0.9em; color: #666;'>
                            Sistema creado por estudiantes de Tecnicatura Superior en Desarrollo de Software – 2026<br>
                            <em>Instituto Superior de Formación Docente y Técnica N.º 124</em>
                        </p>
                    </div>"
            );

            oMailMessage.IsBodyHtml = true;

            using var oSmtpClient = new SmtpClient("smtp.gmail.com")
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Port = 587,
                Credentials = new System.Net.NetworkCredential("softwareinstituto@gmail.com", "bdyfkwgqtxfahslt")
            };

            oSmtpClient.Send(oMailMessage);
        }
    }
}

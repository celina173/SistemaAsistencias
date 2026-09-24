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
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace ISFDyT124.Controllers
{
    public class AccountController : Controller
    {
        // BASE DE DATOS: Declaramos la conexión. Mantuve InstitutoDbContext, modificalo si usás SiAsContext.
        private readonly InstitutoDbContext _context;

        // Valor centinela que se guarda en UsTokenRecovery cuando NO hay una recuperación
        // pendiente: es el default del modelo y también lo que se escribe al consumir un token.
        private const string TokenBloqueado = "tokenbloqueado";

        // Cuánto vale un enlace de recuperación desde que se emite. Si se cambia,
        // hay que actualizar también el texto del mail en Sendemail.
        private const int MinutosVigenciaToken = 30;

        public AccountController(InstitutoDbContext context)
        {
            _context = context;
        }

        // VISTA GET: Devuelve la pantalla de login inicial.
        public IActionResult Login()
        {
            return View();
        }

        // VISTA GET: "Olvidé mi contraseña" — el flujo es manual vía Admin (no hay envío
        // de mails configurado en el proyecto), así que esta vista solo explica el paso a
        // seguir en vez de simular un envío que no pasa a ningún lado.
        /*public IActionResult RecuperoContrasena()
        {
            return View();
        }
        */

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

            // IsPersistent: sin esto la cookie es "de sesión de navegador" y Android la borra
            // apenas el docente cierra la PWA, obligándolo a loguearse de nuevo al reabrirla.
            // En un aula sin señal eso lo dejaba afuera del sistema (no se puede validar la
            // contraseña sin llegar al servidor). Con la cookie persistida, la sesión sobrevive
            // al cierre de la app y puede tomar asistencia sin conexión.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true }
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




        //Recuperación de contraseña

        [HttpGet]
        public ActionResult StartRecovery()
        {
            RecoveryViewModel model = new RecoveryViewModel(); // Crea un modelo vacío para el formulario de recuperación
            return View(model); // Retorna la vista con el modelo para que se muestre el formulario de recuperación
        }

        [HttpPost] // Método que recibe datos del formulario (POST) para iniciar recuperación
        public async Task<IActionResult> StartRecovery(RecoveryViewModel model)
        {
            if (!ModelState.IsValid) // Valida la información recibida del formulario
            {
                return View(model); // Si hay errores en el modelo, regresa la vista con errores
            }

            // Busca en la base de datos un usuario con el correo electrónico ingresado
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsEmail == model.UsEmail);

            if (usuario == null) // Si no encontró usuario con ese correo
            {
                ViewBag.Error = "No se encontró una cuenta asociada a ese correo electrónico."; // Mensaje de error
                return View(model); // Retorna la vista para que el usuario intente otra vez
            }

            // Genera un token único para la recuperación de contraseña
            var token = Guid.NewGuid().ToString("N");

            // Asigna el token de recuperación al usuario. Al pisar la columna, cualquier
            // token anterior de este usuario queda invalidado: hay uno solo vigente por vez.
            usuario.UsTokenRecovery = token;
            usuario.UsTokenRecoveryVencimiento = DateTime.UtcNow.AddMinutes(MinutosVigenciaToken);
            _context.Entry(usuario).State = EntityState.Modified; // Marca la entidad como modificada
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

            // Envía el correo con el token para recuperación
            Sendemail(usuario.UsEmail, token);

            // Mensaje temporal para informar éxito
            TempData["MensajeExito"] = "El enlace de recuperación se ha enviado a su correo registrado correctamente.";
            return RedirectToAction("Login"); // Redirige a la vista de login
        }

        // Devuelve el usuario dueño del token sólo si el token es real y sigue vigente.
        // null significa cualquiera de estos casos: vino vacío, es el centinela
        // "tokenbloqueado", no lo tiene nadie, o ya venció. La validación vive acá y no
        // duplicada en cada acción, para que el GET y el POST no puedan divergir.
        private async Task<Usuario?> BuscarUsuarioPorTokenVigenteAsync(string? token)
        {
            // Sin esto, /Account/Recovery?token=tokenbloqueado matchearía contra el primer
            // usuario que nunca pidió recuperación o que ya consumió la suya.
            if (string.IsNullOrWhiteSpace(token) || token == TokenBloqueado)
                return null;

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsTokenRecovery == token
                                       && u.UsTokenRecoveryVencimiento != null);

            // El vencimiento se escribe y se compara siempre en UTC.
            if (usuario is null || usuario.UsTokenRecoveryVencimiento < DateTime.UtcNow)
                return null;

            return usuario;
        }

        [HttpGet] // Solicitud GET para acceder a la vista de recuperación con un token
        public async Task<IActionResult> Recovery(string token)
        {
            var usuario = await BuscarUsuarioPorTokenVigenteAsync(token);

            if (usuario is null) // Token ausente, centinela, inexistente o vencido
            {
                TempData["Error"] = "El enlace de recuperación es inválido o ha expirado."; // Mensaje de error
                return RedirectToAction("StartRecovery");
            }

            var model = new RecoveryPasswordViewModel
            {
                UsTokenRecovery = token
            }; // Crea modelo con el token para la vista
            return View(model); // Muestra la vista para ingresar nueva contraseña
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

            // Se revalida el token acá y no sólo en el GET: entre que se abrió el formulario
            // y se envió pudieron pasar horas, y nadie más vuelve a mirar el vencimiento.
            var usuario = await BuscarUsuarioPorTokenVigenteAsync(model.UsTokenRecovery);

            if (usuario is null) // Token ausente, centinela, inexistente o vencido
            {
                TempData["Error"] = "Token inválido o expirado. Solicite un nuevo enlace de recuperación.";
                return RedirectToAction("StartRecovery");
            }

            // Actualiza la contraseña del usuario con la nueva contraseña hasheada
            usuario.UsContrasena = PasswordService.HashPassword(model.UsContrasena!);
            usuario.UsTokenRecovery = TokenBloqueado; // Marca el token como usado para que no se reutilice
            usuario.UsTokenRecoveryVencimiento = null; // Lo saca del universo de tokens vigentes


            _context.Entry(usuario).State = EntityState.Modified; // Marca entidad modificada
            await _context.SaveChangesAsync(); // Guarda cambios en la DB

            TempData["MensajeExito"] = "Contraseña modificada con éxito. Ya puede iniciar sesión.";
            return RedirectToAction("Login"); // Redirige a login
        }

        // Método privado para enviar un correo de restablecimiento de contraseña
        private void Sendemail(string EmailDestino, string token)
        {
            // Dirección base del sitio para construir el link de recuperación
            string urlDomain = "https://localhost:7054/";
            var url = Url.Action("Recovery", "Account", new { token = token }, Request.Scheme);
            // Opcional: encodear para HTML
            var urlEscaped = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(url);

            // Construcción del mensaje con cuerpo HTML
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

            oMailMessage.IsBodyHtml = true; // Especifica que el cuerpo es HTML

            // Configuración del cliente SMTP para enviar el correo vía Gmail(puerto 587, SSL)
            using var oSmtpClient = new SmtpClient("smtp.gmail.com")
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Port = 587,
                Credentials = new System.Net.NetworkCredential("softwareinstituto@gmail.com", "bdyfkwgqtxfahslt")
            };

            oSmtpClient.Send(oMailMessage); // Enviar el correo
        }

    }
}

using ISFDyT124.Data; // Importa el espacio de nombres para el contexto de la base de datos
using ISFDyT124.Models; // Importa los modelos
using ISFDyT124.Services; // Importa PasswordService para hashear la contraseña sembrada
using Microsoft.AspNetCore.HttpOverrides; // Necesario para ForwardedHeadersOptions (deploy detrás del proxy de Railway)
using Microsoft.EntityFrameworkCore; // Importa Entity Framework Core para acceso a base de datos

//using ISFDyT124.DTOs; // Importa objetos de transferencia de datos

var builder = WebApplication.CreateBuilder(args); // Crea el constructor del builder de la aplicaci�n web

// Configura la conexin a la base de datos SQL Server usando el contexto InstitutoDbContext
builder.Services.AddDbContext<InstitutoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DBSI")
            ?? throw new InvalidOperationException("Connection string 'DBSI' not found.")
    )
);

// Aade controladores con vistas para MVC
builder.Services.AddControllersWithViews();

// El antiforgery clásico espera el token en un campo de formulario HTML
// (__RequestVerificationToken), pensado para submits normales. La cola offline
// sincroniza vía fetch() con JSON, así que en vez de eso el token viaja en este
// header — [ValidateAntiForgeryToken] lo sigue validando igual, solo cambia de dónde
// lo lee.
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// Configura la autenticaci�n basada en cookies
builder
    .Services.AddAuthentication("Cookies") // Define el esquema de autenticaci�n llamado "Cookies"
    .AddCookie(
        "Cookies",
        options => // Configura opciones para autenticaci�n por cookies
        {
            options.LoginPath = "/Account/Login"; // Ruta a la p�gina de login para redirecci�n en caso de no autenticado
            options.LogoutPath = "/Account/Salir"; // Ruta para cerrar sesi�n
            // 30 días (antes 30 minutos): ahora que la cookie se persiste, este es el tiempo
            // real que el docente puede estar sin llegar al servidor y seguir logueado. Con 30
            // minutos, alguien que se logueaba con wifi a la mañana y llegaba al aula sin señal
            // un par de horas después ya aparecía deslogueado y no podía tomar asistencia.
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true; // Renueva el tiempo de expiraci�n al solicitar recursos si el usuario est� activo
            options.AccessDeniedPath = "/Home/Privacy"; // Ruta a la que redirige si el usuario no tiene permisos

            // Sin esto, un fetch() a /api/* con la cookie vencida recibe un 302 a
            // /Account/Login (HTML) en vez de un 401 — el JS de sincronización no
            // puede reaccionar bien a eso. Para rutas /api devolvemos el status code
            // crudo; el resto de la app sigue redirigiendo como siempre.
            options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                },
            };
        }
    );

var app = builder.Build(); // Construye la aplicaci�n con la configuraci�n realizada

// Seed: crear roles y usuario admin si no existen
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InstitutoDbContext>();

    var rolAdmin = await context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion == "Admin");
    if (rolAdmin == null && !await context.Roles.AnyAsync(r => r.RoId == 1))
    {
        rolAdmin = new Rol { RoId = 1, RoDenominacion = "Admin" };
        context.Roles.Add(rolAdmin);
    }

    // Roles del negocio: se busca por RoId (PK fija) y se corrige la denominación si cambió,
    // en vez de buscar por nombre (que en bases con el seed viejo insertaba una fila duplicada
    // y rompía el arranque). Denominaciones definitivas: Docente / Estudiante / Dirección.
    var rolDocente = await context.Roles.FindAsync(2);
    if (rolDocente == null)
        context.Roles.Add(new Rol { RoId = 2, RoDenominacion = "Docente" });
    else if (rolDocente.RoDenominacion != "Docente")
        rolDocente.RoDenominacion = "Docente";

    var rolEstudiante = await context.Roles.FindAsync(3);
    if (rolEstudiante == null)
        context.Roles.Add(new Rol { RoId = 3, RoDenominacion = "Estudiante" });
    else if (rolEstudiante.RoDenominacion != "Estudiante")
        rolEstudiante.RoDenominacion = "Estudiante";

    if (!await context.Roles.AnyAsync(r => r.RoId == 4))
        context.Roles.Add(new Rol { RoId = 4, RoDenominacion = "Dirección" });

    await context.SaveChangesAsync();
}

// Railway (y cualquier proxy inverso) termina el HTTPS en su borde y reenvía la request al
// contenedor por HTTP simple — sin esto, UseHttpsRedirection/UseHsts ven cada request como HTTP
// y la vuelven a mandar a HTTPS, generando un loop de redirects infinito para el visitante.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configuraciones para ambientes que NO son de desarrollo
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Manejo global de excepciones, lleva a la p�gina de error
    app.UseHsts(); // Usa HTTP Strict Transport Security para proteger la app en producci�n
}

//Middleware
app.UseHttpsRedirection(); // Redirige solicitudes HTTP a HTTPS
app.UseStaticFiles(); // Habilita servir archivos est�ticos (CSS, JS, im�genes)
app.UseRouting(); // Habilita el enrutamiento de solicitudes HTTP
app.UseAuthentication(); // Habilita la autenticaci�n en middleware para validar usuarios
app.UseAuthorization(); // Habilita autorizaci�n para acceso a recursos // Define la ruta por defecto para las peticiones MVC: controlador, acci�n y par�metro opcional id
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run(); // Ejecuta la aplicaci�n web

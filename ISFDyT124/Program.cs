using ISFDyT124.Data; // Importa el espacio de nombres para el contexto de la base de datos
using ISFDyT124.Models; // Importa los modelos
using ISFDyT124.Services; // Importa PasswordService para hashear la contraseña sembrada
using Microsoft.AspNetCore.HttpOverrides; // Necesario para ForwardedHeadersOptions (deploy detrás del proxy de Railway)
using Microsoft.AspNetCore.Http.Features;
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
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ExcelImportService.MaximoBytes + (256 * 1024);
    options.ValueCountLimit = 50_000;
});

// El antiforgery clásico espera el token en un campo de formulario HTML
// (__RequestVerificationToken), pensado para submits normales. La cola offline
// sincroniza vía fetch() con JSON, así que en vez de eso el token viaja en este
// header — [ValidateAntiForgeryToken] lo sigue validando igual, solo cambia de dónde
// lo lee.
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// Configura la autenticacion basada en cookies
builder
    .Services.AddAuthentication("Cookies") // Define el esquema de autenticación llamado "Cookies"
    .AddCookie(
        "Cookies",
        options => // Configura opciones para autenticacion por cookies
        {
            options.LoginPath = "/Account/Login"; // Ruta a la pagina de login para redireccion en caso de no autenticado
            options.LogoutPath = "/Account/Salir"; // Ruta para cerrar sesión
            // un par de horas después ya aparecía deslogueado y no podía tomar asistencia.
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true; // Renueva el tiempo de expiración al solicitar recursos si el usuario está activo

            // Solución QA-11: Ruta propia para acceso no autorizado en lugar de /Home/Privacy
            options.AccessDeniedPath = "/Home/AccessDenied";

            // Mantiene el manejo especial para rutas /api (para que el fetch() de sincronización
            // reciba un 401/403 en crudo en vez de una redirección HTML a la página de Login).
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

var app = builder.Build(); // Construye la aplicacion con la configuracion realizada

// Seed: crear roles y usuario admin si no existen
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InstitutoDbContext>();

    var rolAdmin = await context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion == "Admin");
    if (rolAdmin == null && !await context.Roles.AnyAsync(r => r.RoId == RolId.Admin))
    {
        rolAdmin = new Rol { RoId = RolId.Admin, RoDenominacion = "Admin" };
        context.Roles.Add(rolAdmin);
    }

    var rolDocente = await context.Roles.FindAsync(RolId.Docente);
    if (rolDocente == null)
        context.Roles.Add(new Rol { RoId = RolId.Docente, RoDenominacion = "Docente" });
    else if (rolDocente.RoDenominacion != "Docente")
        rolDocente.RoDenominacion = "Docente";

    var rolEstudiante = await context.Roles.FindAsync(RolId.Estudiante);
    if (rolEstudiante == null)
        context.Roles.Add(new Rol { RoId = RolId.Estudiante, RoDenominacion = "Estudiante" });
    else if (rolEstudiante.RoDenominacion != "Estudiante")
        rolEstudiante.RoDenominacion = "Estudiante";

    if (!await context.Roles.AnyAsync(r => r.RoId == RolId.Direccion))
        context.Roles.Add(new Rol { RoId = RolId.Direccion, RoDenominacion = "Dirección" });

    await context.SaveChangesAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configuraciones para ambientes que NO son de desarrollo
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Manejo global de excepciones, lleva a la página de error
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

    app.UseHsts(); // Usa HTTP Strict Transport Security para proteger la app en producción
}

//Middleware
app.UseHttpsRedirection(); // Redirige solicitudes HTTP a HTTPS
app.UseStaticFiles(); // Habilita servir archivos esteticos
app.UseRouting(); // Habilita el enrutamiento de solicitudes HTTP
app.UseAuthentication(); // Habilita la autenticacion en middleware para validar usuarios
app.UseAuthorization(); // Habilita autorizacion para acceso a recursos 
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run(); // Ejecuta la aplicacion web
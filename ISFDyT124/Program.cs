using System.Threading.RateLimiting; // Necesario para Rate Limiting
using ISFDyT124.Data; // Importa el espacio de nombres para el contexto de la base de datos
using ISFDyT124.Models; // Importa los modelos
using ISFDyT124.Services; // Importa PasswordService y servicios auxiliares
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides; // Necesario para ForwardedHeadersOptions (deploy detrás del proxy de Railway)
using Microsoft.AspNetCore.RateLimiting; // Necesario para Rate Limiting
using Microsoft.EntityFrameworkCore; // Importa Entity Framework Core para acceso a base de datos

var builder = WebApplication.CreateBuilder(args);

// Configura la conexión a la base de datos SQL Server usando el contexto InstitutoDbContext
builder.Services.AddDbContext<InstitutoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DBSI")
            ?? throw new InvalidOperationException("Connection string 'DBSI' not found.")
    )
);

// Habilita IMemoryCache para el rate limiting por correo
builder.Services.AddMemoryCache();

// Registra el servicio que limita intentos por dirección de email
builder.Services.AddSingleton<IEmailRateLimiter, EmailRateLimiter>();

// Configura Rate Limiting nativo de ASP.NET Core (por IP)
builder.Services.AddRateLimiter(options =>
{
    // Manejador cuando un usuario supera el límite por IP
    options.OnRejected = async (context, cancellationToken) =>
    {
        // Si el bloqueo ocurrió intentando recuperar contraseña
        if (context.HttpContext.Request.Path.StartsWithSegments("/Account/StartRecovery"))
        {
            // Redirige al Login con el flag para mostrar la advertencia
            context.HttpContext.Response.Redirect("/Account/Login?limiteExcedido=true");
            return;
        }

        // Demasiados intentos de CUIL en "Directivos": vuelve a la pantalla con el aviso
        if (context.HttpContext.Request.Path.StartsWithSegments("/Profesor/Directivos"))
        {
            context.HttpContext.Response.Redirect("/Profesor/Directivos?limiteExcedido=true");
            return;
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Demasiadas solicitudes. Inténtalo más tarde.", cancellationToken);
    };

    // Política para la verificación de CUIL de "Directivos": por usuario logueado, para frenar la adivinanza
    options.AddPolicy("DirectivosLimiter", httpContext =>
    {
        var clave = httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clave,
            factory: key => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                         // Máximo 5 intentos por usuario
                Window = TimeSpan.FromMinutes(15),       // Cada 15 minutos
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Política específica para el endpoint de recuperación de contraseña
    options.AddPolicy("ForgotPasswordLimiter", httpContext =>
    {
        // Al estar activo UseForwardedHeaders, RemoteIpAddress contiene la IP pública del cliente
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: key => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                         // Máximo 5 peticiones por IP
                Window = TimeSpan.FromMinutes(15),       // Cada 15 minutos
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0                           // Sin cola de espera: rechaza de inmediato
            });
    });
});

// Añade controladores con vistas para MVC
builder.Services.AddControllersWithViews();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ExcelImportService.MaximoBytes + (256 * 1024);
    options.ValueCountLimit = 50_000;
});

// Antiforgery para peticiones normales y fetch()
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// Configura la autenticación basada en cookies
builder
    .Services.AddAuthentication("Cookies")
    .AddCookie(
        "Cookies",
        options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Salir";
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.AccessDeniedPath = "/Home/AccessDenied";

            options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
            {
                // El Estudiante no tiene pantallas propias y el login ya no lo deja entrar,
                // pero una cookie emitida ANTES de ese cambio dura hasta 30 días (y se renueva
                // sola mientras se use). Se revisa en cada pedido, sin ir a la base: el rol
                // viaja dentro de la cookie. Si es Estudiante se descarta la sesión y se cierra.
                OnValidatePrincipal = async context =>
                {
                    if (context.Principal?.IsInRole("Estudiante") == true)
                    {
                        context.RejectPrincipal();
                        await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions
                            .SignOutAsync(context.HttpContext, "Cookies");
                    }
                },
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

var app = builder.Build();

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

// Muy importante: ForwardedHeaders DEBE ejecutarse primero para resolver la IP real antes del Rate Limiter
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configuraciones para ambientes que NO son de desarrollo
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
    app.UseHsts();
}

// Pipeline de Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// RateLimiter debe ir OBLIGATORIAMENTE después de UseRouting y antes de UseAuthentication/UseAuthorization
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

// Expone Program a ISFDyT124.Tests (WebApplicationFactory).
public partial class Program { }

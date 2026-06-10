
using ISFDyT124.Data; // Importa el espacio de nombres para el contexto de la base de datos
using ISFDyT124.Models; // Importa los modelos
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

// Configura la autenticaci�n basada en cookies
builder
    .Services.AddAuthentication("Cookies") // Define el esquema de autenticaci�n llamado "Cookies"
    .AddCookie(
        "Cookies",
        options => // Configura opciones para autenticaci�n por cookies
        {
            options.LoginPath = "/Account/Login"; // Ruta a la p�gina de login para redirecci�n en caso de no autenticado
            options.LogoutPath = "/Account/Salir"; // Ruta para cerrar sesi�n
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Tiempo de expiraci�n de la cookie (30 minutos)
            options.SlidingExpiration = true; // Renueva el tiempo de expiraci�n al solicitar recursos si el usuario est� activo
            options.AccessDeniedPath = "/Home/Privacy"; // Ruta a la que redirige si el usuario no tiene permisos
        }
    );

var app = builder.Build(); // Construye la aplicaci�n con la configuraci�n realizada

// Seed: crear roles y usuario admin si no existen
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InstitutoDbContext>();

    var rolAdmin = await context.Roles.FirstOrDefaultAsync(r => r.RoDenominacion == "Admin");
    if (rolAdmin == null)
    {
        rolAdmin = new Rol { RoId = 1, RoDenominacion = "Admin" };
        context.Roles.Add(rolAdmin);
    }

    if (!await context.Roles.AnyAsync(r => r.RoDenominacion == "Profesor"))
        context.Roles.Add(new Rol { RoId = 2, RoDenominacion = "Profesor" });

    if (!await context.Roles.AnyAsync(r => r.RoDenominacion == "Alumno"))
        context.Roles.Add(new Rol { RoId = 3, RoDenominacion = "Alumno" });

    if (!await context.Usuarios.AnyAsync(u => u.UsEmail == "admin@instituto.edu.ar"))
    {
        context.Usuarios.Add(new Usuario
        {
            UsId = 1,
            UsNombre = "Admin",
            UsApellido = "Sistema",
            UsDni = 12345678,
            UsEmail = "admin@instituto.edu.ar",
            UsContrasena = "12345678",
            RoId = rolAdmin.RoId
        });
    }

    await context.SaveChangesAsync();
}

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

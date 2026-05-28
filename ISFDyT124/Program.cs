using ISFDyT124.Data;
using Microsoft.EntityFrameworkCore; // Importa Entity Framework Core para acceso a base de datos

//using ISFDyT124.DTOs; // Importa objetos de transferencia de datos

var builder = WebApplication.CreateBuilder(args); // Crea el constructor del builder de la aplicaci�n web

// Configura la conexi�n a la base de datos SQL Server usando el contexto SiAsContext
builder.Services.AddDbContext<SiAsContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DBSI")
            ?? throw new InvalidOperationException("Connection string 'DBSI' not found.")
    )
);

//A�ade el servicio UsuarioDTO con duraci�n por alcance (scoped)
builder.Services.AddScoped<UsuarioCrearDto>();
builder.Services.AddScoped<UsuarioDetalleDto>();

// A�ade controladores con vistas para MVC
builder.Services.AddControllersWithViews();

// Configura la autenticaci�n basada en cookies
builder
    .Services.AddAuthentication("Cookies") // Define el esquema de autenticaci�n llamado "Cookies"
    .AddCookie(
        "Cookies",
        options => // Configura opciones para autenticaci�n por cookies
        {
            options.LoginPath = "/Access/Login"; // Ruta a la p�gina de login para redirecci�n en caso de no autenticado
            options.LogoutPath = "/Access/Salir"; // Ruta para cerrar sesi�n
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Tiempo de expiraci�n de la cookie (30 minutos)
            options.SlidingExpiration = true; // Renueva el tiempo de expiraci�n al solicitar recursos si el usuario est� activo
            options.AccessDeniedPath = "/Home/Privacy"; // Ruta a la que redirige si el usuario no tiene permisos
        }
    );

var app = builder.Build(); // Construye la aplicaci�n con la configuraci�n realizada

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

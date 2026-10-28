using System.Net;
using System.Text.RegularExpressions;
using ISFDyT124.Data;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Tests;

/// <summary>
/// Caracteriza el alta de usuarios desde el panel de Admin: la contraseña inicial que escribe el admin
/// se guarda y es la que sirve para iniciar sesión (antes el campo se ignoraba y quedaba el DNI).
/// No se prueba el alta SIN contraseña porque esa rama envía un correo de activación por SMTP.
/// </summary>
public class AltaUsuarioTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;

    public AltaUsuarioTests(AppFactory factory) => _factory = factory;

    private HttpClient NuevoCliente() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
    });

    private static async Task<string> TokenAsync(HttpClient cliente, string ruta)
    {
        var html = await (await cliente.GetAsync(ruta)).Content.ReadAsStringAsync();
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(m.Success, $"No se encontró la credencial antifalsificación en {ruta}");
        return m.Groups[1].Value;
    }

    private async Task<HttpResponseMessage> LoginAsync(HttpClient cliente, string dni, string contrasena) =>
        await cliente.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Usuario"] = dni,
            ["Contrasena"] = contrasena,
        }));

    private void SembrarAdmin()
    {
        var opciones = new DbContextOptionsBuilder<InstitutoDbContext>().UseSqlServer(_factory.CadenaConexion).Options;
        using var contexto = new InstitutoDbContext(opciones);
        if (contexto.Usuarios.Any(u => u.UsDni == 30000002)) return;
        contexto.Usuarios.Add(new Usuario
        {
            UsId = 1,
            UsApellido = "Admin",
            UsNombre = "Prueba",
            UsDni = 30000002,
            UsEmail = "admin@prueba.local",
            UsContrasena = PasswordService.HashPassword("Prueba123"),
            RoId = RolId.Admin,
        });
        contexto.SaveChanges();
    }

    [Fact]
    public async Task Usuario_creado_con_contrasena_inicial_inicia_sesion_con_ella()
    {
        _ = _factory.Server; // arranca el host (crea los roles)
        SembrarAdmin();
        var admin = NuevoCliente();
        var loginAdmin = await LoginAsync(admin, "30000002", "Prueba123");
        Assert.Equal(HttpStatusCode.Redirect, loginAdmin.StatusCode);

        var token = await TokenAsync(admin, "/Admin/UsuarioAgregar");
        var alta = await admin.PostAsync("/Admin/UsuarioAgregar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["UsApellido"] = "Lescano",
            ["UsNombre"] = "Barbara",
            ["UsEmail"] = "barbara@prueba.local",
            ["UsDni"] = "35250151",
            ["selectedRoleId"] = RolId.Direccion.ToString(),
            ["userPassword"] = "ClaveNueva#1",
        }));
        Assert.Equal(HttpStatusCode.Redirect, alta.StatusCode);
        Assert.Contains("/Admin/UsuariosABM", alta.Headers.Location!.OriginalString);

        var opciones = new DbContextOptionsBuilder<InstitutoDbContext>().UseSqlServer(_factory.CadenaConexion).Options;
        using (var contexto = new InstitutoDbContext(opciones))
        {
            var guardado = contexto.Usuarios.Single(u => u.UsDni == 35250151);
            Assert.True(PasswordService.VerifyPassword("ClaveNueva#1", guardado.UsContrasena));
            Assert.False(PasswordService.VerifyPassword("35250151", guardado.UsContrasena));
        }

        var login = await LoginAsync(NuevoCliente(), "35250151", "ClaveNueva#1");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.DoesNotContain("CambiarContrasena", login.Headers.Location!.OriginalString);

        // El DNI ya no abre la cuenta cuando el admin fijó otra contraseña.
        var conDni = await LoginAsync(NuevoCliente(), "35250151", "35250151");
        Assert.Equal(HttpStatusCode.OK, conDni.StatusCode);
    }
}

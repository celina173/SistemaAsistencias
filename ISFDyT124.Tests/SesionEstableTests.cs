using System.Net;
using System.Text.RegularExpressions;
using ISFDyT124.Data;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Tests;

/// <summary>
/// Sesión estable (D6): las claves de protección de datos viven en la base, así que la cookie de sesión y la
/// credencial antifalsificación emitidas por una instancia las acepta otra instancia (otra réplica) y la misma
/// app después de reiniciarse. Cada "instancia" es un host distinto sobre la MISMA base.
/// </summary>
public class SesionEstableTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;

    public SesionEstableTests(AppFactory factory) => _factory = factory;

    private static HttpClient Cliente(WebApplicationFactory<Program> f) => f.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = false, // las cookies se pasan a mano de una instancia a otra
    });

    private static string Cookies(HttpResponseMessage r) =>
        string.Join("; ", r.Headers.TryGetValues("Set-Cookie", out var v)
            ? v.Select(c => c.Split(';')[0]) : Array.Empty<string>());

    private static string Token(string html)
    {
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(m.Success, "No se encontró la credencial antifalsificación");
        return m.Groups[1].Value;
    }

    private void SembrarAdmin()
    {
        _factory.CreateClient().Dispose(); // arrancar la app siembra los roles
        var opciones = new DbContextOptionsBuilder<InstitutoDbContext>().UseSqlServer(_factory.CadenaConexion).Options;
        using var contexto = new InstitutoDbContext(opciones);
        if (contexto.Usuarios.Any(u => u.UsDni == 30000003)) return;
        contexto.Usuarios.Add(new Usuario
        {
            UsId = 2, UsApellido = "Admin", UsNombre = "Sesion", UsDni = 30000003,
            UsEmail = "sesion@prueba.local", UsContrasena = PasswordService.HashPassword("Prueba123"),
            RoId = RolId.Admin,
        });
        contexto.SaveChanges();
    }

    private static async Task<string> IniciarSesionAsync(HttpClient c)
    {
        var login = await c.GetAsync("/Account/Login");
        var cookiesAntiforgery = Cookies(login);
        var token = Token(await login.Content.ReadAsStringAsync());
        var pedido = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Usuario"] = "30000003", ["Contrasena"] = "Prueba123", ["__RequestVerificationToken"] = token,
            }),
        };
        pedido.Headers.Add("Cookie", cookiesAntiforgery);
        var r = await c.SendAsync(pedido);
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return Cookies(r);
    }

    private static async Task<HttpStatusCode> PedirAsync(HttpClient c, string ruta, string cookie)
    {
        var p = new HttpRequestMessage(HttpMethod.Get, ruta);
        p.Headers.Add("Cookie", cookie);
        return (await c.SendAsync(p)).StatusCode;
    }

    [SoloConLocalDbFact]
    public async Task La_cookie_de_una_instancia_la_acepta_otra()
    {
        SembrarAdmin();
        using var a = _factory.WithWebHostBuilder(_ => { });
        using var b = _factory.WithWebHostBuilder(_ => { });
        var ca = Cliente(a);
        var cb = Cliente(b);

        var cookie = await IniciarSesionAsync(ca);
        Assert.Equal(HttpStatusCode.OK, await PedirAsync(ca, "/Admin", cookie));
        Assert.Equal(HttpStatusCode.OK, await PedirAsync(cb, "/Admin", cookie));
    }

    [SoloConLocalDbFact]
    public async Task La_credencial_antifalsificacion_de_una_instancia_la_acepta_otra()
    {
        SembrarAdmin();
        using var a = _factory.WithWebHostBuilder(_ => { });
        using var b = _factory.WithWebHostBuilder(_ => { });

        var login = await Cliente(a).GetAsync("/Account/Login");
        var cookies = Cookies(login);
        var token = Token(await login.Content.ReadAsStringAsync());

        var pedido = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Usuario"] = "30000003", ["Contrasena"] = "Prueba123", ["__RequestVerificationToken"] = token,
            }),
        };
        pedido.Headers.Add("Cookie", cookies);
        var r = await Cliente(b).SendAsync(pedido);
        Assert.NotEqual(HttpStatusCode.BadRequest, r.StatusCode); // 400 = credencial rechazada
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
    }

    [SoloConLocalDbFact]
    public async Task La_sesion_sobrevive_a_un_reinicio_de_la_app()
    {
        SembrarAdmin();
        string cookie;
        using (var antes = _factory.WithWebHostBuilder(_ => { }))
            cookie = await IniciarSesionAsync(Cliente(antes));

        // En Windows las claves por defecto viven en el perfil del usuario y compartirían el resultado aunque no se
        // guardaran en la base: se comprueba que sí están en la tabla.
        var opciones = new DbContextOptionsBuilder<InstitutoDbContext>().UseSqlServer(_factory.CadenaConexion).Options;
        using (var contexto = new InstitutoDbContext(opciones))
            Assert.True(contexto.DataProtectionKeys.Any(), "Las claves de protección de datos no se guardaron en la base");

        // Host nuevo, sin memoria del anterior: sólo comparte la base.
        using var despues = _factory.WithWebHostBuilder(_ => { });
        Assert.Equal(HttpStatusCode.OK, await PedirAsync(Cliente(despues), "/Admin", cookie));
    }
}

using System.Net;
using System.Text.RegularExpressions;
using ISFDyT124.Data;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Tests;

/// <summary>
/// Acceso "Directivos" (ticket 3.46): un docente con CUIL cargado lo escribe y la cookie se vuelve a firmar con el rol
/// Dirección. Se prueba lo que el PR no probó en ejecución: CUIL correcto eleva, incorrecto no, sin CUIL cargado no
/// eleva, y pasados 5 intentos se bloquea.
/// </summary>
public class DirectivosTests : IClassFixture<AppFactory>
{
    private const long CuilDocente = 20300000043;
    private readonly AppFactory _factory;

    public DirectivosTests(AppFactory factory) => _factory = factory;

    private DbContextOptions<InstitutoDbContext> Opciones =>
        new DbContextOptionsBuilder<InstitutoDbContext>().UseSqlServer(_factory.CadenaConexion).Options;

    private HttpClient Cliente() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private void SembrarDocente(int usId, int dni, long? cuil)
    {
        _factory.CreateClient().Dispose(); // arrancar la app siembra los roles
        using var db = new InstitutoDbContext(Opciones);
        if (db.Usuarios.Any(u => u.UsDni == dni)) return;
        db.Usuarios.Add(new Usuario
        {
            UsId = usId, UsApellido = "Docente", UsNombre = "Directivo" + usId, UsDni = dni,
            UsEmail = $"directivo{usId}@prueba.local", UsContrasena = PasswordService.HashPassword("Prueba123"),
            RoId = RolId.Docente, UsActivo = true, UsCuil = cuil,
        });
        db.SaveChanges();
    }

    private static string Token(string html)
    {
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(m.Success, "No se encontró la credencial antifalsificación");
        return m.Groups[1].Value;
    }

    private static async Task IniciarSesionAsync(HttpClient c, int dni)
    {
        var token = Token(await c.GetStringAsync("/Account/Login"));
        var r = await c.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Usuario"] = dni.ToString(), ["Contrasena"] = "Prueba123", ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
    }

    private static async Task<HttpResponseMessage> EnviarCuilAsync(HttpClient c, string cuil)
    {
        var token = Token(await c.GetStringAsync("/Profesor/Directivos"));
        return await c.PostAsync("/Profesor/Directivos", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["cuil"] = cuil, ["__RequestVerificationToken"] = token,
        }));
    }

    private static async Task<HttpStatusCode> PanelDireccionAsync(HttpClient c) => (await c.GetAsync("/Admin")).StatusCode;

    [SoloConLocalDbFact]
    public async Task Con_el_CUIL_correcto_el_docente_accede_al_panel_de_Direccion()
    {
        SembrarDocente(20, 30000020, CuilDocente);
        var c = Cliente();
        await IniciarSesionAsync(c, 30000020);

        Assert.NotEqual(HttpStatusCode.OK, await PanelDireccionAsync(c)); // antes de elevarse no entra

        var r = await EnviarCuilAsync(c, CuilDocente.ToString());
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Admin", r.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, await PanelDireccionAsync(c));
    }

    [SoloConLocalDbFact]
    public async Task Con_un_CUIL_incorrecto_no_se_eleva()
    {
        SembrarDocente(21, 30000021, 20300000051);
        var c = Cliente();
        await IniciarSesionAsync(c, 30000021);

        var r = await EnviarCuilAsync(c, "20111111112");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode); // vuelve a la pantalla con el error
        Assert.Contains("no es correcto", await r.Content.ReadAsStringAsync());
        Assert.NotEqual(HttpStatusCode.OK, await PanelDireccionAsync(c));
    }

    [SoloConLocalDbFact]
    public async Task Un_docente_sin_CUIL_cargado_no_puede_elevarse()
    {
        SembrarDocente(22, 30000022, null);
        var c = Cliente();
        await IniciarSesionAsync(c, 30000022);

        var r = await EnviarCuilAsync(c, "20300000043");
        Assert.Contains("no es correcto", await r.Content.ReadAsStringAsync());
        Assert.NotEqual(HttpStatusCode.OK, await PanelDireccionAsync(c));
    }

    [SoloConLocalDbFact]
    public async Task Con_el_formato_invalido_o_con_guiones_se_rechaza()
    {
        SembrarDocente(23, 30000023, 20300000060);
        var c = Cliente();
        await IniciarSesionAsync(c, 30000023);

        var r = await EnviarCuilAsync(c, "20-30000006-0");
        Assert.Contains("11 dígitos", await r.Content.ReadAsStringAsync());
        Assert.NotEqual(HttpStatusCode.OK, await PanelDireccionAsync(c));
    }

    [SoloConLocalDbFact]
    public async Task Pasados_cinco_intentos_se_bloquea_aun_con_el_CUIL_correcto()
    {
        SembrarDocente(24, 30000024, 20300000078);
        var c = Cliente();
        await IniciarSesionAsync(c, 30000024);

        for (var i = 0; i < 5; i++) await EnviarCuilAsync(c, "20111111112");

        var r = await EnviarCuilAsync(c, "20300000078");
        // El límite redirige a la pantalla con el aviso y no eleva.
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("limiteExcedido", r.Headers.Location!.ToString());
        Assert.NotEqual(HttpStatusCode.OK, await PanelDireccionAsync(c));
    }
}

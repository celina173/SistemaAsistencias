using System.Net;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISFDyT124.Data;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Tests;

/// <summary>
/// Pruebas de contrato (D8): fijan lo que la PWA necesita de las pantallas de asistencia que mantienen otros
/// integrantes. La app guarda la planilla real y reenvía el formulario real, así que si alguien renombra un campo,
/// la action o los enlaces, estas pruebas fallan antes de llegar a producción.
/// Cómo se distingue el resultado de un POST: el servidor redirige; si hubo rechazo, la página de destino trae
/// el aviso con id "operationError" (TempData["OperationErrorMessage"]); si no, no hay aviso.
/// </summary>
public class ContratoAsistenciaTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;

    public ContratoAsistenciaTests(AppFactory factory) => _factory = factory;

    private DbContextOptions<InstitutoDbContext> Opciones =>
        new DbContextOptionsBuilder<InstitutoDbContext>().UseSqlServer(_factory.CadenaConexion).Options;

    private HttpClient NuevoCliente() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static async Task<IDocument> AbrirAsync(HttpResponseMessage r)
    {
        var html = await r.Content.ReadAsStringAsync();
        return await BrowsingContext.New(Configuration.Default).OpenAsync(x => x.Content(html));
    }

    /// <summary>Sigue las redirecciones como el navegador.</summary>
    private static async Task<HttpResponseMessage> SeguirAsync(HttpClient c, HttpResponseMessage r)
    {
        for (var i = 0; i < 5 && (int)r.StatusCode is >= 300 and < 400 && r.Headers.Location != null; i++)
            r = await c.GetAsync(r.Headers.Location);
        return r;
    }

    private int SembrarCatedraConAlumno()
    {
        _factory.CreateClient().Dispose(); // arrancar la app siembra los roles
        using var db = new InstitutoDbContext(Opciones);
        if (!db.Usuarios.Any(u => u.UsDni == 30000004))
        {
            db.Usuarios.Add(new Usuario
            {
                UsId = 10, UsApellido = "Docente", UsNombre = "Contrato", UsDni = 30000004,
                UsEmail = "docente.contrato@prueba.local", UsContrasena = PasswordService.HashPassword("Prueba123"),
                RoId = RolId.Docente, UsActivo = true,
            });
            db.Usuarios.Add(new Usuario
            {
                UsId = 11, UsApellido = "Alumno", UsNombre = "Contrato", UsDni = 40000001,
                UsEmail = "alumno.contrato@prueba.local", UsContrasena = PasswordService.HashPassword("Prueba123"),
                RoId = RolId.Estudiante, UsActivo = true,
            });
            db.SaveChanges();
        }
        var existente = db.CarreraMaterias.Select(c => (int?)c.CaMaId).FirstOrDefault();
        if (existente != null) return existente.Value;

        var carrera = new Carrera { CaDenominacion = "Carrera Contrato" };
        var cohorte = new Cohorte { CoId = (db.Cohortes.Max(c => (int?)c.CoId) ?? 0) + 1, CoAnio = 2026, CoEstado = true };
        var materia = new Materia { MaDenominacion = "Materia Contrato", MaModalidad = "Presencial", MaCantModulos = 2 };
        db.AddRange(carrera, cohorte, materia);
        db.SaveChanges();
        var cc = new CarreraCohorte { CaId = carrera.CaId, CoId = cohorte.CoId };
        db.CarreraCohortes.Add(cc);
        db.SaveChanges();
        var cm = new CarreraMateria { CaCoId = cc.CaCoId, MaId = materia.MaId };
        db.CarreraMaterias.Add(cm);
        db.SaveChanges();
        db.Inscripciones.Add(new Inscripciones { UsId = 11, CaMaId = cm.CaMaId, InFechaInscripcion = DateTime.Today.AddDays(-30) });
        db.Usuarios.Include(u => u.CarreraMaterias).First(u => u.UsId == 10).CarreraMaterias.Add(cm); // el docente tiene la cátedra asignada
        db.SaveChanges();
        return cm.CaMaId;
    }

    private async Task<HttpClient> DocenteAsync()
    {
        var c = NuevoCliente();
        var doc = await AbrirAsync(await c.GetAsync("/Account/Login"));
        var token = doc.QuerySelector("input[name=__RequestVerificationToken]")!.GetAttribute("value")!;
        var r = await c.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Usuario"] = "30000004", ["Contrasena"] = "Prueba123", ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return c;
    }

    private static List<KeyValuePair<string, string>> Valores(IElement formulario)
    {
        var valores = new List<KeyValuePair<string, string>>();
        foreach (var campo in formulario.QuerySelectorAll("input, select, textarea"))
        {
            var nombre = campo.GetAttribute("name");
            if (string.IsNullOrEmpty(nombre) || campo.HasAttribute("disabled")) continue;
            if (campo is IHtmlSelectElement lista)
            {
                var elegida = lista.Options.FirstOrDefault(o => o.IsSelected && !string.IsNullOrEmpty(o.Value))
                              ?? lista.Options.FirstOrDefault(o => !string.IsNullOrEmpty(o.Value));
                valores.Add(new(nombre, elegida?.Value ?? ""));
            }
            else if (campo is IHtmlInputElement entrada)
            {
                if (entrada.Type is "checkbox" or "radio") { if (entrada.IsChecked) valores.Add(new(nombre, entrada.Value)); }
                else valores.Add(new(nombre, entrada.Value));
            }
            else if (campo is IHtmlTextAreaElement area) valores.Add(new(nombre, area.Value));
        }
        return valores;
    }

    [SoloConLocalDbFact]
    public async Task La_lista_es_un_selector_y_cada_combinacion_redirige_a_la_planilla_y_a_la_global()
    {
        var caMaId = SembrarCatedraConAlumno();
        var c = await DocenteAsync();

        // La lista NO trae enlaces con CaMaId: la precarga de la PWA descubre las cátedras con estos selectores.
        var doc = await AbrirAsync(await c.GetAsync("/Asistencias"));
        var carreras = doc.QuerySelectorAll("select[name=SelectedCarreraId] option[value]").Select(o => o.GetAttribute("value")!).Where(v => v != "").ToList();
        var materias = doc.QuerySelectorAll("select[name=SelectedMateriaId] option[value]").Select(o => o.GetAttribute("value")!).Where(v => v != "").ToList();
        Assert.NotEmpty(carreras);
        Assert.NotEmpty(materias);

        var q = $"?selectedCarreraId={carreras[0]}&selectedMateriaId={materias[0]}";
        var plan = await c.GetAsync("/Asistencias/Index" + q);
        Assert.Equal(HttpStatusCode.Redirect, plan.StatusCode);
        Assert.Contains($"CaMaId={caMaId}", plan.Headers.Location!.ToString());

        var global = await c.GetAsync("/Asistencias/Index" + q + "&_global=1");
        Assert.Equal(HttpStatusCode.Redirect, global.StatusCode);
        Assert.Contains("AsistenciaGlobal", global.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [SoloConLocalDbFact]
    public async Task La_planilla_trae_el_formulario_la_fecha_los_campos_de_filas_y_la_credencial()
    {
        var caMaId = SembrarCatedraConAlumno();
        var c = await DocenteAsync();

        var doc = await AbrirAsync(await c.GetAsync($"/Asistencias/Asistencia?CaMaId={caMaId}"));
        var form = doc.QuerySelector("form#formAsistencia");
        Assert.NotNull(form);
        Assert.Equal("/Asistencias/Asistencia", new Uri(new Uri("https://localhost"), form!.GetAttribute("action")).AbsolutePath, ignoreCase: true);
        Assert.Equal("post", form.GetAttribute("method"), ignoreCase: true);
        Assert.NotNull(form.QuerySelector("input[name=__RequestVerificationToken]"));
        Assert.NotNull(form.QuerySelector("input[name=CaMaId]"));
        Assert.NotNull(form.QuerySelector("select[name=FechaAsistencia]"));
        Assert.NotEmpty(form.QuerySelectorAll("[name^='Rows['][name$='].UsId']"));
    }

    [SoloConLocalDbFact]
    public async Task Un_POST_valido_con_credencial_fresca_se_acepta_y_reenviarlo_no_duplica()
    {
        var caMaId = SembrarCatedraConAlumno();
        var c = await DocenteAsync();

        async Task<(IElement form, List<KeyValuePair<string, string>> valores)> Planilla()
        {
            var doc = await AbrirAsync(await c.GetAsync($"/Asistencias/Asistencia?CaMaId={caMaId}"));
            var form = doc.QuerySelector("form#formAsistencia")!;
            return (form, Valores(form));
        }

        var (form, valores) = await Planilla();
        Assert.Contains(valores, v => v.Key == "FechaAsistencia" && v.Value != "");
        var primera = await SeguirAsync(c, await c.PostAsync(form.GetAttribute("action"), new FormUrlEncodedContent(valores)));
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.DoesNotContain("id=\"operationError\"", await primera.Content.ReadAsStringAsync());
        using (var db = new InstitutoDbContext(Opciones))
            Assert.Equal(1, db.Asistencias.Count());

        // Reenvío de lo mismo (como hace la bandeja si la respuesta se perdió): el servidor NO lo duplica y lo
        // rechaza con el aviso "ya fue registrada" (redirección con id="operationError"). La PWA lo muestra como rechazo.
        var (form2, _) = await Planilla(); // credencial fresca
        var token = form2.QuerySelector("input[name=__RequestVerificationToken]")!.GetAttribute("value")!;
        var reenvio = valores.Where(v => v.Key != "__RequestVerificationToken").ToList();
        reenvio.Add(new("__RequestVerificationToken", token));
        var segunda = await SeguirAsync(c, await c.PostAsync(form2.GetAttribute("action"), new FormUrlEncodedContent(reenvio)));
        Assert.Contains("id=\"operationError\"", await segunda.Content.ReadAsStringAsync());
        Assert.Contains("ya fue registrada", await segunda.Content.ReadAsStringAsync());
        using (var db = new InstitutoDbContext(Opciones))
            Assert.Equal(1, db.Asistencias.Count());
    }

    [SoloConLocalDbFact]
    public async Task Un_POST_con_fecha_fuera_del_calendario_responde_400_con_el_motivo()
    {
        var caMaId = SembrarCatedraConAlumno();
        var c = await DocenteAsync();

        var doc = await AbrirAsync(await c.GetAsync($"/Asistencias/Asistencia?CaMaId={caMaId}"));
        var form = doc.QuerySelector("form#formAsistencia")!;
        var valores = Valores(form);
        valores.RemoveAll(v => v.Key == "FechaAsistencia");
        valores.Add(new("FechaAsistencia", "2001-01-01"));

        // Una fecha fuera del calendario responde 400 con el motivo en texto plano (no hay aviso con id operationError).
        var r = await c.PostAsync(form.GetAttribute("action"), new FormUrlEncodedContent(valores));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("La fecha no pertenece al calendario de cursada", await r.Content.ReadAsStringAsync());
    }
}

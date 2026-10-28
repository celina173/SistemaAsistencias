using System.Net;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ClosedXML.Excel;
using ISFDyT124.Data;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Tests;

/// <summary>
/// Recorre la carga masiva de alumnos de punta a punta con un Excel que imita los problemas de las planillas reales:
/// una primera pestaña con datos que no son alumnos, apellido y nombre en una sola columna, títulos repetidos,
/// títulos más abajo de la primera fila y columnas en distinto orden según la pestaña.
/// </summary>
public class CargaMasivaTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;

    public CargaMasivaTests(AppFactory factory) => _factory = factory;

    private DbContextOptions<InstitutoDbContext> Opciones =>
        new DbContextOptionsBuilder<InstitutoDbContext>().UseSqlServer(_factory.CadenaConexion).Options;

    private HttpClient NuevoCliente() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
    });

    // ---------- Excel de prueba ----------

    private static string CrearExcel()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"carga_masiva_test_{Guid.NewGuid():N}.xlsx");
        using var libro = new XLWorkbook();

        // Primera pestaña con datos, pero sin alumnos: antes el mapeo se armaba con ESTA pestaña.
        var categorias = libro.AddWorksheet("Categorías");
        categorias.Cell(1, 1).Value = "Espacios curriculares";
        categorias.Cell(1, 3).Value = "Trayectorias";
        categorias.Cell(1, 4).Value = "Situación final";
        categorias.Cell(2, 1).Value = "Análisis Matemático";
        categorias.Cell(2, 3).Value = "Regular";
        categorias.Cell(2, 4).Value = "REGULAR (A final)";

        // Una sola columna "Apellido y Nombres", títulos repetidos y DNI numérico.
        var matriculados = libro.AddWorksheet("Matriculados");
        string[] titulos = { "Marca temporal", "Dirección de correo electrónico", "Apellido y Nombres", "DNI", "Cohorte" };
        for (int i = 0; i < titulos.Length; i++) matriculados.Cell(1, i + 1).Value = titulos[i];
        for (int i = 6; i <= 10; i++) matriculados.Cell(1, i).Value = "Espacios curriculares de 1º año";
        object[][] alumnos =
        {
            new object[] { "10/03/2026", "ana@prueba.local", "ALBORNOZ ANA", 43866426, 2024 },
            new object[] { "10/03/2026", "luis@prueba.local", "MEDINA, LUIS ALBERTO", 45214316, 2024 },
            new object[] { "10/03/2026", "eva@prueba.local", "DISTASIO EVA", "38290907.0", 2024 }, // DNI como texto "38290907.0"
            new object[] { "10/03/2026", "sin-dni@prueba.local", "ROMERO SOFIA", "", 2024 },       // fila con error
        };
        for (int f = 0; f < alumnos.Length; f++)
            for (int c = 0; c < alumnos[f].Length; c++)
                matriculados.Cell(f + 2, c + 1).Value = XLCellValue.FromObject(alumnos[f][c]);

        // Otra pestaña: títulos en la fila 4, columnas en otro orden, y un alumno repetido de "Matriculados".
        var listado = libro.AddWorksheet("Listado2");
        listado.Cell(1, 1).Value = "Instituto Superior";
        listado.Cell(2, 1).Value = "Listado de alumnos";
        string[] titulos2 = { "N°", "DNI", "Apellido", "Nombre", "Mail" };
        for (int i = 0; i < titulos2.Length; i++) listado.Cell(4, i + 1).Value = titulos2[i];
        object[][] alumnos2 =
        {
            new object[] { 1, 43866426, "ALBORNOZ", "ANA", "ana@prueba.local" },
            new object[] { 2, 40111222, "GOMEZ", "MARTA", "marta@prueba.local" },
        };
        for (int f = 0; f < alumnos2.Length; f++)
            for (int c = 0; c < alumnos2[f].Length; c++)
                listado.Cell(f + 5, c + 1).Value = XLCellValue.FromObject(alumnos2[f][c]);

        libro.SaveAs(ruta);
        return ruta;
    }

    // ---------- Navegación como lo haría el navegador ----------

    private static async Task<IDocument> AbrirAsync(HttpResponseMessage respuesta)
    {
        var html = await respuesta.Content.ReadAsStringAsync();
        return await BrowsingContext.New(Configuration.Default).OpenAsync(r => r.Content(html));
    }

    private async Task LoginAdminAsync(HttpClient cliente)
    {
        _ = _factory.Server; // arranca el host (crea los roles)
        using (var contexto = new InstitutoDbContext(Opciones))
        {
            if (!contexto.Usuarios.Any(u => u.UsDni == 30000002))
            {
                contexto.Usuarios.Add(new Usuario
                {
                    UsId = 1, UsApellido = "Admin", UsNombre = "Prueba", UsDni = 30000002, UsEmail = "admin@prueba.local",
                    UsContrasena = PasswordService.HashPassword("Prueba123"), RoId = RolId.Admin,
                });
                contexto.SaveChanges();
            }
        }
        var login = await cliente.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Usuario"] = "30000002", ["Contrasena"] = "Prueba123",
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
    }

    private (int caCoId, int caMaId) SembrarCarrera()
    {
        using var contexto = new InstitutoDbContext(Opciones);
        var carrera = new Carrera { CaDenominacion = "Tecnicatura de Prueba" };
        // Cohortes.CoId no es autoincremental
        var cohorte = new Cohorte { CoId = (contexto.Cohortes.Max(c => (int?)c.CoId) ?? 0) + 1, CoAnio = 2026, CoEstado = true };
        var materia = new Materia { MaDenominacion = "Materia de Prueba", MaModalidad = "Presencial", MaCantModulos = 2 };
        contexto.AddRange(carrera, cohorte, materia);
        contexto.SaveChanges();
        var cc = new CarreraCohorte { CaId = carrera.CaId, CoId = cohorte.CoId };
        contexto.CarreraCohortes.Add(cc);
        contexto.SaveChanges();
        var cm = new CarreraMateria { CaCoId = cc.CaCoId, MaId = materia.MaId };
        contexto.CarreraMaterias.Add(cm);
        contexto.SaveChanges();
        return (cc.CaCoId, cm.CaMaId);
    }

    /// <summary>Arma el cuerpo de un formulario igual que el navegador: omite casillas sin marcar y toma lo elegido de cada lista.</summary>
    private static List<KeyValuePair<string, string>> ValoresDelFormulario(IElement formulario)
    {
        var valores = new List<KeyValuePair<string, string>>();
        foreach (var campo in formulario.QuerySelectorAll("input, select, textarea"))
        {
            var nombre = campo.GetAttribute("name");
            if (string.IsNullOrEmpty(nombre) || campo.HasAttribute("disabled")) continue;

            if (campo is IHtmlSelectElement lista)
            {
                foreach (var opcion in lista.Options.Where(o => o.IsSelected && !o.IsDisabled))
                    valores.Add(new(nombre, opcion.Value));
                if (!lista.IsMultiple && !lista.Options.Any(o => o.IsSelected) && lista.Options.Length > 0)
                    valores.Add(new(nombre, lista.Options[0].Value));
            }
            else if (campo is IHtmlInputElement entrada)
            {
                if (entrada.Type is "checkbox" or "radio")
                {
                    if (entrada.IsChecked) valores.Add(new(nombre, entrada.Value));
                }
                else valores.Add(new(nombre, entrada.Value));
            }
        }
        return valores;
    }

    private static void Poner(List<KeyValuePair<string, string>> valores, string nombre, string valor)
    {
        valores.RemoveAll(v => v.Key == nombre);
        valores.Add(new(nombre, valor));
    }

    private async Task<IDocument> SubirArchivoAsync(HttpClient cliente, string rutaExcel, int caCoId, int caMaId)
    {
        var paso1 = await AbrirAsync(await cliente.GetAsync("/Admin/CargaMasivaAlumnos"));
        var token = paso1.QuerySelector("input[name=__RequestVerificationToken]")!.GetAttribute("value")!;

        using var contenido = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(caCoId.ToString()), "CaCoId" },
            { new StringContent(caMaId.ToString()), "SelectedCaMaIds" },
        };
        var bytes = new ByteArrayContent(await File.ReadAllBytesAsync(rutaExcel));
        bytes.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        contenido.Add(bytes, "ArchivoExcel", "alumnos.xlsx");

        var respuesta = await cliente.PostAsync("/Admin/CargaMasivaMapear", contenido);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await AbrirAsync(respuesta);
    }

    // ---------- Pruebas ----------

    [SoloConLocalDbFact]
    public async Task Cada_pestana_reconoce_sus_propias_columnas_aunque_la_primera_sea_otra()
    {
        var excel = CrearExcel();
        try
        {
            var (caCoId, caMaId) = SembrarCarrera();
            var cliente = NuevoCliente();
            await LoginAdminAsync(cliente);

            var mapeo = await SubirArchivoAsync(cliente, excel, caCoId, caMaId);

            // Sólo se marcan las pestañas donde el sistema reconoció DNI, apellido, nombre y correo.
            bool Marcada(string hoja) =>
                ((IHtmlInputElement)mapeo.QuerySelector($".hoja-card[data-hoja='{hoja}'] .hoja-incluir")!).IsChecked;
            Assert.False(Marcada("Categorías"));
            Assert.True(Marcada("Matriculados"));
            Assert.True(Marcada("Listado2"));

            string Elegida(string hoja, string campo) =>
                ((IHtmlSelectElement)mapeo.QuerySelector($".hoja-card[data-hoja='{hoja}'] select[data-campo='{campo}']")!).Value;
            // Matriculados: correo en B, "Apellido y Nombres" en C, DNI en D
            Assert.Equal("4", Elegida("Matriculados", "Dni"));
            Assert.Equal("3", Elegida("Matriculados", "Apellido"));
            Assert.Equal("2", Elegida("Matriculados", "Email"));
            Assert.True(((IHtmlInputElement)mapeo.QuerySelector(".hoja-card[data-hoja='Matriculados'] .hoja-combinada")!).IsChecked);
            // Listado2: títulos en la fila 4, otro orden de columnas
            Assert.Equal("4", ((IHtmlInputElement)mapeo.QuerySelector(".hoja-card[data-hoja='Listado2'] .hoja-fila")!).Value);
            Assert.Equal("2", Elegida("Listado2", "Dni"));
            Assert.Equal("3", Elegida("Listado2", "Apellido"));
            Assert.Equal("4", Elegida("Listado2", "Nombre"));
            Assert.Equal("5", Elegida("Listado2", "Email"));
        }
        finally { File.Delete(excel); }
    }

    [SoloConLocalDbFact]
    public async Task Al_cambiar_la_fila_de_titulos_se_leen_las_columnas_de_esa_pestana()
    {
        var excel = CrearExcel();
        try
        {
            var (caCoId, caMaId) = SembrarCarrera();
            var cliente = NuevoCliente();
            await LoginAdminAsync(cliente);
            var mapeo = await SubirArchivoAsync(cliente, excel, caCoId, caMaId);
            var token = mapeo.QuerySelector("input[name=TempFileToken]")!.GetAttribute("value")!;

            var respuesta = await cliente.GetAsync($"/Admin/CargaMasivaColumnas?token={token}&hoja=Listado2&fila=4");
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(4, json.GetProperty("filaCabecera").GetInt32());
            Assert.Equal(new[] { "N°", "DNI", "Apellido", "Nombre", "Mail" },
                json.GetProperty("columnas").EnumerateArray().Select(c => c.GetString()).ToArray());
            Assert.Equal(2, json.GetProperty("sugerido").GetProperty("dni").GetInt32());

            // Una fila que no es la de títulos no reconoce nada, pero no rompe.
            var otra = await cliente.GetAsync($"/Admin/CargaMasivaColumnas?token={token}&hoja=Listado2&fila=1");
            var jsonOtra = JsonDocument.Parse(await otra.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(0, jsonOtra.GetProperty("sugerido").GetProperty("dni").GetInt32());

            // Otro usuario no puede leer el archivo temporal.
            var ajeno = NuevoCliente();
            var denegado = await ajeno.GetAsync($"/Admin/CargaMasivaColumnas?token={token}&hoja=Listado2");
            Assert.NotEqual(HttpStatusCode.OK, denegado.StatusCode);
        }
        finally { File.Delete(excel); }
    }

    [SoloConLocalDbFact]
    public async Task Flujo_completo_previsualiza_consolida_y_confirma_la_carga()
    {
        var excel = CrearExcel();
        try
        {
            var (caCoId, caMaId) = SembrarCarrera();
            var cliente = NuevoCliente();
            await LoginAdminAsync(cliente);

            var mapeo = await SubirArchivoAsync(cliente, excel, caCoId, caMaId);
            var formMapeo = mapeo.QuerySelector("#formMapeoColumnas")!;
            var previsualizacion = await AbrirAsync(await cliente.PostAsync("/Admin/CargaMasivaPrevisualizar",
                new FormUrlEncodedContent(ValoresDelFormulario(formMapeo))));

            // 3 de Matriculados con datos válidos + 1 de Listado2 nuevo + el repetido consolidado (ANA) = 4 alumnos
            // válidos, más la fila sin DNI que queda editable para corregirla.
            var filas = previsualizacion.QuerySelectorAll("#formPreview tbody tr").ToList();
            var dnis = filas.Select(f => ((IHtmlInputElement)f.QuerySelector("input[name$='.Dni']")!).Value).ToList();
            Assert.Contains("43866426", dnis);
            Assert.Contains("45214316", dnis);
            Assert.Contains("38290907", dnis); // "38290907.0" se lee como 38290907, no como 382909070
            Assert.Contains("40111222", dnis);
            Assert.Equal(1, dnis.Count(d => d == "43866426")); // ANA aparece en dos pestañas y se consolida
            Assert.Contains("", dnis);                          // la fila sin DNI

            // Apellido y nombre en una sola columna: se separan
            var luis = filas.Single(f => ((IHtmlInputElement)f.QuerySelector("input[name$='.Dni']")!).Value == "45214316");
            Assert.Equal("MEDINA", ((IHtmlInputElement)luis.QuerySelector("input[name$='.Apellido']")!).Value);
            Assert.Equal("LUIS ALBERTO", ((IHtmlInputElement)luis.QuerySelector("input[name$='.Nombre']")!).Value);

            // Se corrige la fila sin DNI y se confirma
            var valores = ValoresDelFormulario(previsualizacion.QuerySelector("#formPreview")!);
            var filaSinDni = filas.First(f => ((IHtmlInputElement)f.QuerySelector("input[name$='.Dni']")!).Value == "");
            var nombreCampoDni = ((IHtmlInputElement)filaSinDni.QuerySelector("input[name$='.Dni']")!).Name!;
            Poner(valores, nombreCampoDni, "41222333");

            var resultado = await cliente.PostAsync("/Admin/CargaMasivaConfirmar", new FormUrlEncodedContent(valores));
            Assert.True(resultado.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect, $"Confirmar devolvió {resultado.StatusCode}");

            using var contexto = new InstitutoDbContext(Opciones);
            var esperados = new[] { 43866426, 45214316, 38290907, 40111222, 41222333 };
            foreach (var dni in esperados)
            {
                var usuario = contexto.Usuarios.SingleOrDefault(u => u.UsDni == dni);
                Assert.NotNull(usuario);
                Assert.True(PasswordService.VerifyPassword(dni.ToString(), usuario!.UsContrasena));
                Assert.True(contexto.Inscripciones.Any(i => i.UsId == usuario.UsId && i.CaMaId == caMaId),
                    $"El alumno {dni} no quedó inscripto en la materia");
            }
        }
        finally { File.Delete(excel); }
    }

    [SoloConLocalDbFact]
    public async Task Sin_pestanas_marcadas_o_con_columnas_repetidas_vuelve_al_mapeo_con_el_motivo()
    {
        var excel = CrearExcel();
        try
        {
            var (caCoId, caMaId) = SembrarCarrera();
            var cliente = NuevoCliente();
            await LoginAdminAsync(cliente);
            var mapeo = await SubirArchivoAsync(cliente, excel, caCoId, caMaId);
            var form = mapeo.QuerySelector("#formMapeoColumnas")!;

            // Mismo número de columna para DNI y para correo en Listado2
            var valores = ValoresDelFormulario(form);
            var indice = mapeo.QuerySelectorAll(".hoja-card").ToList()
                .FindIndex(c => c.GetAttribute("data-hoja") == "Listado2");
            Poner(valores, $"Hojas[{indice}].ColEmail", "2");
            var repetidas = await AbrirAsync(await cliente.PostAsync("/Admin/CargaMasivaPrevisualizar", new FormUrlEncodedContent(valores)));
            Assert.NotNull(repetidas.QuerySelector("#formMapeoColumnas"));
            Assert.Contains("Listado2", repetidas.Body!.TextContent);
            Assert.Contains("columna distinta", repetidas.Body.TextContent);
            // Lo que había elegido se conserva
            Assert.Equal("2", ((IHtmlSelectElement)repetidas.QuerySelector(".hoja-card[data-hoja='Listado2'] select[data-campo='Email']")!).Value);

            // Ninguna pestaña marcada
            var ninguna = ValoresDelFormulario(form).Where(v => !v.Key.EndsWith(".Incluir") || v.Value != "true").ToList();
            var sinHojas = await AbrirAsync(await cliente.PostAsync("/Admin/CargaMasivaPrevisualizar", new FormUrlEncodedContent(ninguna)));
            Assert.Contains("al menos una pestaña", sinHojas.Body!.TextContent);
        }
        finally { File.Delete(excel); }
    }
}

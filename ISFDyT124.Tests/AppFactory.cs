using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ISFDyT124.Data;

using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Tests;

/// <summary>
/// Levanta la app contra una base LocalDB descartable: la crea con todas las migraciones
/// al construirse y la borra al liberarse. Nunca toca una base real.
/// </summary>
public class AppFactory : WebApplicationFactory<Program>
{
    private const string Servidor = @"(localdb)\MSSQLLocalDB";

    public string NombreBase { get; } = "ISFDyT124_Test_" + Guid.NewGuid().ToString("N");

    public string CadenaConexion =>
        $"Server={Servidor};Database={NombreBase};Trusted_Connection=True;TrustServerCertificate=True;";

    public AppFactory()
    {
        if (!OperatingSystem.IsWindows()) return; // sin LocalDB las pruebas se omiten (ver SoloConLocalDbFact)

        var opciones = new DbContextOptionsBuilder<InstitutoDbContext>()
            .UseSqlServer(CadenaConexion)
            .Options;
        using var contexto = new InstitutoDbContext(opciones);
        contexto.Database.Migrate();

        // Program lee la cadena al construir el host; la variable de entorno llega antes que cualquier otra fuente.
        Environment.SetEnvironmentVariable("ConnectionStrings__DBSI", CadenaConexion);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || !OperatingSystem.IsWindows()) return;

        var opciones = new DbContextOptionsBuilder<InstitutoDbContext>()
            .UseSqlServer(CadenaConexion)
            .Options;
        using var contexto = new InstitutoDbContext(opciones);
        contexto.Database.EnsureDeleted();
        Environment.SetEnvironmentVariable("ConnectionStrings__DBSI", null);
    }
}

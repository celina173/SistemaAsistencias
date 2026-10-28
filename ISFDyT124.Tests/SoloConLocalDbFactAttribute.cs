namespace ISFDyT124.Tests;

/// <summary>
/// Las pruebas de integración crean una base en SQL Server LocalDB, que sólo existe en Windows.
/// En el CI de GitHub (Linux) se omiten en lugar de fallar.
/// </summary>
public sealed class SoloConLocalDbFactAttribute : FactAttribute
{
    public SoloConLocalDbFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "Requiere SQL Server LocalDB (sólo Windows).";
    }
}

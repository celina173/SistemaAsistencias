# Por qué el proyecto pasó de .NET 10 a .NET 9

## Qué pasó

El commit `9cb19c2` ("Actualizar a .NET 10") de Agustín cambió el proyecto de
`net8.0` a `net10.0` (y todos los paquetes de EF Core a la versión 10.x
correspondiente). Al probarlo, aparecieron dos problemas que afectan a
**todo el equipo**, no solo a una máquina:

1. **El SDK de .NET 10 no viene instalado por defecto** — cualquiera que
   hiciera `git pull` y tratara de compilar (`dotnet build`/`dotnet run`)
   se encontraba con el error `NETSDK1045: El SDK de .NET actual no admite
   el destino .NET 10.0`, hasta instalar el SDK nuevo a mano.
2. **Visual Studio 2022 no puede compilar un proyecto `net10.0`, ni instalando
   el SDK.** El propio Visual Studio tira: *"La versión actual de Visual
   Studio no admite .NET 10.0 de destino. Use el destino .NET 9.0 o uno
   inferior, o use la versión 18.0 o una superior de Visual Studio."* — o
   sea, hace falta pasar a **Visual Studio 2026** (versión 18), que es una
   instalación grande y una actualización mayor del IDE, no un simple parche.

Si nos quedábamos en `net10.0`, **todo el equipo tendría que instalar Visual
Studio 2026** para poder seguir abriendo y compilando el proyecto en el IDE.

## Decisión

Se decidió pasar el proyecto a **.NET 9**.

## Cambios técnicos concretos

En `ISFDyT124.csproj`:

| Paquete | Antes (net10) | Ahora (net9) |
|---|---|---|
| `TargetFramework` | `net10.0` | `net9.0` |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.12` | `9.0.20` |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.12` | `9.0.20` |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.12` | `9.0.20` |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | `10.0.2` | `9.0.12` |
| `NuGet.ProjectModel` (fix de vulnerabilidad de Agustín) | `6.12.5` | `6.12.5` (sin cambios — no depende de la versión de .NET) |

Compilación verificada tras el cambio: 0 errores.

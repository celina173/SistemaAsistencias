# Cambios para hash de contraseña

## Situación previa

El proyecto guardaba las contraseñas **en texto plano** en la columna
`UsContrasena`. El login las comparaba directamente dentro de la consulta SQL,
y tanto el alta de usuarios como el usuario admin sembrado al arrancar
escribían la contraseña tal cual, sin ningún tipo de protección.

Se reutiliza `Services/PasswordService.cs` (utilizado en 2025 en la materia de Programación en otros sistemas similares) con un hash SHA256

## Por qué NO usamos SHA256

SHA256 es un algoritmo de *integridad de datos* (sirve para verificar que un
archivo no fue modificado), no un algoritmo de contraseñas. Usarlo para
contraseñas tiene dos problemas graves:


1. **No tiene salt. (Abajo de estos 2 puntos explico lo que es "salt" y por que es importante)** La misma contraseña siempre produce el mismo hash. Si dos
   docentes eligen la misma contraseña, en la base se ve idéntica. Eso permite
   atacar todos los usuarios a la vez con una *rainbow table* (una tabla
   precalculada de contraseñas comunes y sus hashes).

2. **Es rápido por diseño.** SHA256 está optimizado para calcularse millones de
   veces por segundo. Esa velocidad, que es una virtud para verificar archivos,
   es exactamente lo que hace viable un ataque de fuerza bruta si alguna vez se
   filtra un backup de la base de datos.


¿Que es Salt?
Un *salt* es un valor aleatorio que se genera para cada
contraseña y se mezcla con ella antes de hashearla, de modo que la misma
contraseña nunca produzca dos veces el mismo hash. SHA256 a secas no lo
usa, y el resultado es siempre idéntico:

   ```
   SHA256("1234") = 03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4
   ```

   Ese valor es el mismo en cualquier máquina del mundo. Entonces, si dos
   docentes eligen `1234`, en la base aparece exactamente el mismo texto en
   las dos filas. Eso habilita dos ataques:

   - Cualquiera que mire la tabla ve **qué usuarios comparten contraseña**,
     sin romper nada.
   - Un atacante precalcula **una sola vez** el hash de las contraseñas más
     comunes (una *rainbow table*) y la compara contra toda la tabla de
     usuarios de una pasada: no rompe una contraseña, las rompe todas juntas.

   Con salt, en cambio, a cada usuario le toca uno distinto:

   ```
   SHA256("1234" + "x7k2p9") = 3ae084298c076bef7f85ed4cb0e5f8047d54632c070d479dc25090821a502f9d
   SHA256("1234" + "m4q8w1") = 903e9c366091371c853adeedad7eda9ff260920231eec98d8238473faab6ef1d
   ```

   Misma contraseña, hashes completamente distintos. La rainbow table queda
   inservible, porque habría que construir una nueva por cada usuario.

   Un detalle importante: **el salt no es secreto**. Se guarda en la base
   junto al hash, a la vista. Su función no es esconder información, sino
   evitar que todos los hashes se puedan atacar en simultáneo. `PasswordHasher`

¿Por que no lo agregamos a la Base de Datos?
   genera un salt aleatorio de 128 bits por contraseña y lo guarda embebido
   dentro del mismo string del hash, por eso no hace falta una columna aparte.




## Por qué usamos PasswordHasher

`PasswordHasher<TUser>` es la clase que **Microsoft recomienda explícitamente**
en su documentación oficial para apps ASP.NET Core que guardan contraseñas.
La cita textual traducida es:

"KeyDerivation.Pbkdf2 no debería usarse en aplicaciones nuevas que admitan inicio de sesión con contraseña y que necesiten almacenar contraseñas hasheadas en un almacén de datos. Las aplicaciones nuevas deberían usar la clase PasswordHasher."
>
> — [Hash passwords in ASP.NET Core, Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing)

https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0

Es decir: Microsoft desaconseja incluso llamar directamente a su propia función
PBKDF2 de bajo nivel, y señala a `PasswordHasher` como la forma correcta de
hashear contraseñas para guardarlas. Los datos técnicos de la tabla siguiente
salen del [código fuente oficial de `PasswordHasher.cs`](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordHasher.cs),
en el repositorio `dotnet/aspnetcore`.

(ACÁ VAYAN JUGANDO CON EL TAMAÑO DE LA PANTALLA PARA PODER VISUALIZAR BIEN LA TABLA COMPARATIVA)

| Característica | SHA256 (anterior)     | PasswordHasher (actual)                      |

| Algoritmo      | SHA256 plano          | PBKDF2 + HMAC-SHA512                         |
| Salt           | No tiene              | Aleatorio, 128 bits, distinto por contraseña |
| Iteraciones    | 1                     | 100.000                                      |
| Misma contraseña 
en 2 usuarios    | Produce el mismo hash | Produce hashes distintos                     |
| Rainbow tables | Vulnerable            | No aplicable (el salt las invalida)          |
| Fuerza bruta   | Rápida                | Lenta a propósito                            |

Otras razones prácticas que decidieron la elección:

- **No requiere cambios en la base de datos.** El salt viaja incluido dentro
  del mismo string del hash, así que se sigue guardando en una sola columna.
  `UsContrasena` ya es `nvarchar(max)`, sin límite de longitud que estorbe.
- **No requiere instalar ningún paquete NuGet.** La clase vive en
  `Microsoft.Extensions.Identity.Core`, que ya viene incluido en el framework
  compartido de ASP.NET Core al usar `Sdk="Microsoft.NET.Sdk.Web"`.
- **No requiere adoptar todo ASP.NET Core Identity.** Su constructor es
  `PasswordHasher<TUser>(IOptions<PasswordHasherOptions>? optionsAccessor = null)`,
  o sea que `new PasswordHasher<Usuario>()` funciona solo, sin registrar nada
  en el contenedor de inyección de dependencias de `Program.cs`.

---

## Cambios realizados

### 1. `ISFDyT124/Services/PasswordService.cs`

**Antes**

```csharp
using System.Security.Cryptography;
using System.Text;

public static string HashPassword(string password)
{
    if (string.IsNullOrEmpty(password))
        throw new ArgumentException("La contraseña no puede estar vacía.");

    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}

public static bool VerifyPassword(string password, string hashedPassword)
{
    var hashOfInput = HashPassword(password);
    return hashOfInput == hashedPassword;
}
```

**Después**

```csharp
using ISFDyT124.Models;
using Microsoft.AspNetCore.Identity;

private static readonly PasswordHasher<Usuario> _hasher = new();

public static string HashPassword(string password)
{
    if (string.IsNullOrEmpty(password))
        throw new ArgumentException("La contraseña no puede estar vacía.");

    return _hasher.HashPassword(null!, password);
}

public static bool VerifyPassword(string password, string hashedPassword)
{
    var resultado = _hasher.VerifyHashedPassword(null!, hashedPassword, password);
    return resultado != PasswordVerificationResult.Failed;
}
```

**Por qué se modificó:** es el núcleo del cambio. `VerifyPassword` ya no puede
"hashear y comparar strings", porque con salt aleatorio la misma contraseña
nunca produce dos veces el mismo hash. `VerifyHashedPassword` extrae el salt
guardado dentro del hash almacenado, vuelve a derivar y compara.

> El parámetro `null!` es el `TUser`: la implementación por defecto de
> `PasswordHasher` no lo usa, solo existe como punto de extensión para hashers
> personalizados.

---

### 2. `ISFDyT124/Controllers/AccountController.cs`

Este archivo concentra tres cambios.

#### 2.a — Login: validación de credenciales

**Antes**

```csharp
var usuario = await _context
    .Usuarios.Include(u => u.Rol)
    .FirstOrDefaultAsync(u =>
        u.UsDni == dniEntero && u.UsContrasena == model.Contrasena
    );

if (usuario == null)
{
```

**Después**

```csharp
var usuario = await _context
    .Usuarios.Include(u => u.Rol)
    .FirstOrDefaultAsync(u => u.UsDni == dniEntero);

if (usuario == null || !PasswordService.VerifyPassword(model.Contrasena, usuario.UsContrasena))
{
```

**Por qué se modificó:** era imposible dejarlo como estaba. La comparación de
contraseña ocurría dentro del `WHERE` que SQL Server ejecuta, y SQL no puede
comparar un hash con salt contra la contraseña que escribió el usuario. Ahora
la búsqueda se hace **solo por DNI**, el usuario se trae a memoria, y la
verificación se hace en C# con `PasswordService.VerifyPassword`.

#### 2.b — Chequeo de "contraseña por defecto"

**Antes**

```csharp
if (usuario.UsDni.ToString() == usuario.UsContrasena)
    return RedirectToAction("CambiarContrasena");
```

**Después**

```csharp
if (PasswordService.VerifyPassword(usuario.UsDni.ToString(), usuario.UsContrasena))
    return RedirectToAction("CambiarContrasena");
```

**Por qué se modificó:** el sistema fuerza el cambio de contraseña cuando el
usuario todavía tiene la contraseña inicial (su propio DNI). Comparar el DNI
contra el valor guardado dejó de funcionar, porque lo guardado ya no es el DNI
sino su hash. Se resolvió preguntando "¿el DNI verifica contra este hash?" en
lugar de "¿el DNI es igual a este texto?".

Se eligió esta solución en vez de agregar una columna nueva
(`DebeCambiarContrasena`) justamente para **no tener que modificar el esquema
de la base ni generar una migración de EF Core**.

#### 2.c — Guardado de la contraseña nueva

**Antes**

```csharp
usuario.UsContrasena = nuevaContrasena;
```

**Después**

```csharp
usuario.UsContrasena = PasswordService.HashPassword(nuevaContrasena);
```

**Por qué se modificó:** el formulario de cambio de contraseña guardaba el
texto plano directamente en la base.

---

### 3. `ISFDyT124/Controllers/AdminController.cs`

**Antes**

```csharp
UsContrasena = model.UsDni.ToString(),
```

**Después**

```csharp
UsContrasena = PasswordService.HashPassword(model.UsDni.ToString()),
```

**Por qué se modificó:** al dar de alta un usuario (docente, alumno o
administrativo), el sistema le asigna su propio DNI como contraseña inicial.
Sigue siendo el mismo valor de contraseña, solo cambia cómo se almacena.

---

### 4. `ISFDyT124/Program.cs`

**Antes**

```csharp
UsContrasena = "12345678",
```

**Después**

```csharp
UsContrasena = PasswordService.HashPassword("12345678"),
```

**Por qué se modificó:** al arrancar, la app siembra un usuario Admin de prueba
si no existe. Su contraseña también quedaba en texto plano en la base.

---

## Archivos que NO se modificaron

Se revisó todo el proyecto (Vistas, DTOs, Modelos, Migraciones) para confirmar
que no quedara ningún punto suelto:

- **`Models/Usuario.cs`** — no hace falta cambio de esquema: `UsContrasena` no
  tiene límite de longitud y la columna SQL es `nvarchar(max)`, así que el hash
  más largo del nuevo formato entra sin problema.
- **`DTO/UsuarioLoginDto.cs` y `Models/LoginViewModel.cs`** — transportan la
  contraseña en texto plano desde el formulario, que es lo correcto: el texto
  plano solo existe durante el request y nunca se guarda.
- **Vistas (`CambiarContrasena.cshtml`, `UsuarioAgregar.cshtml`)** — son
  formularios HTML comunes, no contienen lógica de hasheo.
- **Migraciones** — ninguna migración nueva es necesaria.




## Fuentes

- [Hash passwords in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing)
- [PasswordHasher\<TUser\> Class — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1)
- [Código fuente de PasswordHasher.cs — dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordHasher.cs)

---


## Actualización: contraseñas viejas en texto plano

Los dos cambios de esta sección son consecuencia directa de un mismo hecho: la
base todavía tenía usuarios con `UsContrasena` en texto plano, sembrados antes
de este cambio, y ese texto plano **no es un hash válido** para
`PasswordHasher`.

### 1. `PasswordService.VerifyPassword` — manejo de `FormatException`

**Antes**

```csharp
public static bool VerifyPassword(string password, string hashedPassword)
{
    var resultado = _hasher.VerifyHashedPassword(null!, hashedPassword, password);
    return resultado != PasswordVerificationResult.Failed;
}
```

**Después**

```csharp
public static bool VerifyPassword(string password, string hashedPassword)
{
    if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(password))
        return false;

    try
    {
        var resultado = _hasher.VerifyHashedPassword(null!, hashedPassword, password);
        return resultado != PasswordVerificationResult.Failed;
    }
    catch (FormatException)
    {
        return false;
    }
}
```

**Por qué se modificó:** `VerifyHashedPassword` hace
`Convert.FromBase64String(hashedPassword)` sin ningún try/catch propio, así
que lanza `FormatException` apenas el valor guardado no es Base64 válido. Eso
es exactamente lo que pasa con las contraseñas viejas en texto plano que
todavía quedan en la base (por ejemplo `"3011122"` o `"miClave123"`): no son
hashes, así que el intento de decodificarlas como Base64 explota. No es un
error de la aplicación, es un valor que nunca debió llegar ahí — pero sin este
catch, el login le devolvía un 500 al usuario en vez de simplemente rechazar
la contraseña y dejar que el flujo de migración (punto 2) se hiciera cargo.

### 2. `AccountController.Login` — migración automática (transitoria)

**Después de buscar al usuario por DNI, antes de crear los claims:**

```csharp
if (!PasswordService.VerifyPassword(model.Contrasena, usuario.UsContrasena))
{
    if (usuario.UsContrasena == model.Contrasena)
    {
        usuario.UsContrasena = PasswordService.HashPassword(model.Contrasena);
        await _context.SaveChangesAsync();
    }
    else
    {
        ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
        return View(model);
    }
}
```

**Por qué se agregó:** Se había descartado migrar las contraseñas viejas automáticamente,
proponiendo en cambio reiniciar la base de prueba. Ese enfoque no sirve una
vez que la base deja de ser solo de prueba y ya tiene usuarios reales dados de
alta con contraseña en texto plano: no se los puede dejar afuera del sistema.

La solución no intenta "convertir" el texto plano a hash de forma masiva (eso
requeriría un script aparte y tocar la base compartida). En cambio, aprovecha
el momento en que el propio usuario inicia sesión: si `VerifyPassword` falla
porque lo guardado no es un hash, se compara `usuario.UsContrasena` contra
`model.Contrasena` como texto plano. Si coincide, es una contraseña vieja
todavía sin migrar — se acepta el login y en el mismo request se sobreescribe
`UsContrasena` con su hash real. Si no coincide con ninguna de las dos formas,
la contraseña es simplemente incorrecta.

De esta manera cada usuario se auto-migra la primera vez que vuelve a entrar
después del despliegue, sin downtime ni script manual, y sin que el usuario
note ninguna diferencia en el flujo de login.

**Este bloque es transitorio.** Una vez que se confirme que ya no quedan
contraseñas en texto plano en la base (por ejemplo, revisando que ningún
`UsContrasena` deje de tener el formato de hash de `PasswordHasher`), se puede
borrar y dejar el login con la validación simple original (solo
`PasswordService.VerifyPassword`).

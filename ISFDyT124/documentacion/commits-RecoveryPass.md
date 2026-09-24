# Recuperación de contraseña — detalle de cambios

Documento de referencia para el equipo. Resume **qué se agregó, qué se modificó
y por qué** para implementar el "Olvidé mi contraseña" del sistema.

No es un cambio grande en volumen, pero sí **transversal**: toca modelo, base de
datos, controlador, vistas y CSS. La idea de este documento es que cualquiera
pueda ver de un vistazo qué archivos se tocaron y qué se cambió exactamente en
cada uno.

- **Rama:** `SantiagoCasiLocal`
- **Commit principal (backend):** `e3c4abc` — modelo, ViewModels y acciones del controlador
- **Resto (vistas, estilos y ajustes del mail):** cambios posteriores en la misma rama

---

## Cómo funciona, de punta a punta

```
Login.cshtml
   │  "Olvidé mi contraseña"
   ▼
GET  /Account/StartRecovery        → StartRecovery.cshtml (pide el email)
   │
   ▼
POST /Account/StartRecovery        → busca el usuario por UsEmail
   │                                 genera un token (GUID)
   │                                 lo guarda en Usuario.UsTokenRecovery
   │                                 envía el mail con el link
   ▼
Redirige a Login con mensaje de éxito
   │
   │  (el usuario abre el link del mail)
   ▼
GET  /Account/Recovery?token=xxxx  → valida el token
   │                                 → Recovery.cshtml (pide contraseña nueva)
   ▼
POST /Account/Recovery             → valida que coincidan las dos contraseñas
                                     hashea con PasswordService.HashPassword
                                     deja el token en "tokenbloqueado"
   │
   ▼
Redirige a Login con mensaje de éxito
```

El token es de **un solo uso**: una vez utilizado se sobrescribe con el valor
`"tokenbloqueado"`, que es el mismo valor por defecto que tiene cualquier usuario
que nunca pidió recuperar su contraseña.

---

## Mapa de archivos tocados

| Archivo | Estado |
| --- | --- |
| `Models/Usuario.cs` | Modificado — campo nuevo |
| `Models/ViewModels/RecoveryViewModel.cs` | **Nuevo** |
| `Models/ViewModels/RecoveryPasswordViewModel.cs` | **Nuevo** |
| `Controllers/AccountController.cs` | Modificado — 4 acciones + 1 método privado |
| `Views/Account/Login.cshtml` | Modificado — link y bloque de mensaje |
| `Views/Account/StartRecovery.cshtml` | **Nuevo Nombre** (reemplaza a `RecuperoContrasena.cshtml`) |
| `Views/Account/Recovery.cshtml` | **Nuevo** |
| `wwwroot/css/style.css` | Modificado — una clase nueva |

---

## 1. `Models/Usuario.cs` — campo nuevo

Se agregó una sola propiedad al modelo:

```csharp
// Token para recuperación de contraseña, inicializado como bloqueado
public string? UsTokenRecovery { get; set; } = "tokenbloqueado";
```

Es `string?` (nullable) y arranca con el valor `"tokenbloqueado"`, de modo que un
usuario recién creado no tenga un token válido colgando. Guarda el GUID mientras
hay una recuperación en curso, y vuelve a `"tokenbloqueado"` cuando el link se usa.

No se tocó ninguna otra propiedad del modelo.




---

## 2. ViewModels nuevos

No se utilizó DTO porque ya funcinaba asi, no me pareció necesario cambiarlo. De igual manera Cumplen los mismos obetivos.

Se creó la carpeta `Models/ViewModels/` con dos clases. La idea fue **no usar la
entidad `Usuario` directamente en los formularios**, para que la vista solo reciba
y envíe los campos que realmente necesita.

### `RecoveryViewModel.cs` — paso 1 (pedir el email)

```csharp
public class RecoveryViewModel
{
    [EmailAddress]
    [Required(ErrorMessage = "El campo Email es obligatorio")]
    public string? UsEmail { get; set; }
}
```

### `RecoveryPasswordViewModel.cs` — paso 2 (contraseña nueva)

```csharp
public class RecoveryPasswordViewModel
{
    public string? UsTokenRecovery { get; set; }   // viaja en un input hidden

    [Required]
    public string? UsContrasena { get; set; }      // contraseña nueva

    [Required]
    public string? UsContrasena2 { get; set; }     // confirmación
}
```

La comparación entre `UsContrasena` y `UsContrasena2` **no** se hace con un
atributo `[Compare]`, sino manualmente en el controlador (ver más abajo).

---

## 3. `Controllers/AccountController.cs`

Es el archivo con más cambios. Todo lo nuevo está **agregado al final de la clase**,
después de `Logout()`, bajo el comentario `//Recuperación de contraseña`. No se
modificó ninguna acción existente (`Login`, `Logout`, etc. quedaron intactas).

### Imports agregados

```csharp
using ISFDyT124.Models.ViewModels;
using System.Net.Mail;
```

### `GET StartRecovery()`

Devuelve la vista con un `RecoveryViewModel` vacío. Nada más.

### `POST StartRecovery(RecoveryViewModel model)`

1. Valida el `ModelState` (formato de email y campo obligatorio).
2. Busca el usuario por `UsEmail` con `FirstOrDefaultAsync`.
3. Si no existe → `ViewBag.Error` y vuelve a la misma vista.
4. Si existe → genera el token con `Guid.NewGuid().ToString("N")`, lo guarda en
   `usuario.UsTokenRecovery` y hace `SaveChangesAsync()`.
5. Llama a `Sendemail(usuario.UsEmail, token)`.
6. Carga `TempData["MensajeExito"]` y redirige a `Login`.

### `GET Recovery(string token)`

1. Si el token viene vacío → `TempData["Error"]` y redirige a `StartRecovery`.
2. Busca el usuario cuyo `UsTokenRecovery` coincida con el token.
3. Si no hay coincidencia → `TempData["Error"]` y redirige a `StartRecovery`.
4. Si hay coincidencia → arma un `RecoveryPasswordViewModel` **con el token adentro**
   y muestra la vista. El token viaja a la vista para volver en el POST.

### `POST Recovery(RecoveryPasswordViewModel model)`

Lleva `[ValidateAntiForgeryToken]`.

1. Valida el `ModelState`.
2. Compara `UsContrasena` con `UsContrasena2`; si no coinciden, agrega el error al
   `ModelState` y vuelve a la vista.
3. Busca el usuario por el token que vino en el hidden.
4. Guarda la contraseña **hasheada**, reutilizando el servicio que ya existía en el
   proyecto:
   ```csharp
   usuario.UsContrasena = PasswordService.HashPassword(model.UsContrasena!);
   usuario.UsTokenRecovery = "tokenbloqueado"; // invalida el link
   ```
5. `TempData["MensajeExito"]` y redirige a `Login`.

Acá es importante que no se inventó nada nuevo para las contraseñas: se usa el
mismo `PasswordService` que ya usaban el login y el alta de usuarios (ver
`Cambios para hash de contraseña.md` en esta misma carpeta).

### `private void Sendemail(string EmailDestino, string token)`

Método privado, al final de la clase. Arma y envía el correo:

- Construye la URL absoluta con
  `Url.Action("Recovery", "Account", new { token }, Request.Scheme)`, así el link
  sale correcto en cualquier entorno sin hardcodear el dominio.
- Escapa la URL con `HtmlEncoder.Default.Encode(...)` antes de meterla en el HTML.
- Cuerpo HTML con estilos inline (los clientes de correo no leen hojas de estilo
  externas), con el botón "Restablecer mi contraseña", la advertencia de "si no
  solicitaste esto, ignorá el mensaje" y el pie institucional.
- Envía por SMTP de Gmail (`smtp.gmail.com`, puerto 587, SSL) usando la casilla
  `softwareinstituto@gmail.com` con una contraseña de aplicación.

---

## 4. `Views/Account/Login.cshtml`

Dos cambios chicos:

**a) El link ahora apunta a la acción real.** Antes era un resto del prototipo
estático:

```diff
- <a href="recupero-contraseña.html" class="forgot-password">Olvidé mi contraseña</a>
+ <a asp-controller="Account" asp-action="StartRecovery" class="forgot-password">Olvidé mi contraseña</a>
```

**b) Bloque para mostrar el mensaje de éxito**, agregado dentro del `<form>`, justo
arriba del bloque de validación que ya existía:

```cshtml
@if (TempData["MensajeExito"] != null)
{
    <div class="alerta-exito" role="alert">
        <div class="alerta-texto">@TempData["MensajeExito"]</div>
    </div>
}
```

Es el que muestra tanto "el enlace se envió a tu correo" como "contraseña
modificada con éxito", porque ambos flujos terminan redirigiendo al login.

No se tocó nada del formulario de login en sí, ni el bloque `alerta-error` que ya
estaba.

---

## 5. Vistas renombrada

### `Views/Account/StartRecovery.cshtml` (reemplaza a `RecuperoContrasena.cshtml`)

La vista `RecuperoContrasena.cshtml` que estaba en el repo era el HTML del
prototipo: no tenía `@model`, el form no apuntaba a ninguna acción y las rutas de
los archivos eran relativas al prototipo (`style.css`, `img/logo.png`,
`index.html`). Se la renombró a `StartRecovery.cshtml` para que coincida con el
nombre de la acción y se la conectó al backend.

**Se respetó el diseño y la estructura HTML original tal cual.** Los cambios fueron
solo de cableado:

| Antes (prototipo) | Ahora |
| --- | --- |
| sin `@model` | `@model RecoveryViewModel` + `Layout = null` |
| `href="style.css"` | `href="~/css/style.css" asp-append-version="true"` |
| `src="img/logo.png"` | `src="~/images/logo.png"` |
| `<form id="formRecupero">` | `<form method="post" asp-action="StartRecovery">` |
| `<input id="emailRecupero">` | `<input asp-for="UsEmail">` + `<span asp-validation-for>` |
| `href="index.html"` | `asp-action="Login"` |
| `src="script.js"` | `src="~/js/script.js"` |

Y se agregaron los dos bloques de error (`ViewBag.Error` y `TempData["Error"]`).

### `Views/Account/Recovery.cshtml`

Vista nueva, construida **con la misma estructura y las mismas clases CSS** que
`StartRecovery.cshtml` para que las tres pantallas del flujo se vean iguales.
Contiene:

- El `<input type="hidden" asp-for="UsTokenRecovery" />` que devuelve el token.
- Dos campos de contraseña (`UsContrasena` y `UsContrasena2`), con `type="password"`,
  `required`, `minlength="6"` y `autocomplete="new-password"`.
- El bloque de `ValidationSummary` para mostrar "Las contraseñas no coinciden".
- Botón "Guardar Contraseña" y link de volver al login.

El formulario usa el tag helper `asp-action`, así que ASP.NET inserta el
antiforgery token automáticamente — por eso funciona con el
`[ValidateAntiForgeryToken]` de la acción.

---

## 6. `wwwroot/css/style.css`

**Un solo agregado**, y a propósito: no se modificó ni una regla existente.

Se agregó `.alerta-exito` (línea ~1889), copiando exactamente la estructura de
`.alerta-error` que ya estaba justo arriba, pero en verde:

```css
.alerta-exito {
    display: flex;
    align-items: center;
    background-color: #f0fdf4;  /* verde muy suave */
    border: 1px solid #4ade80;
    color: #166534;
    padding: 12px 16px;
    border-radius: 8px;
    margin-bottom: 20px;
    font-size: 14px;
    width: 100%;
    box-sizing: border-box;
}
```

Las clases que usan las vistas nuevas (`.auth-wrapper-global`, `.auth-card`,
`.form-title-auth`, `.auth-instruction`, `.input-field-auth`,
`.btn-enviar-recupero`, `.link-volver`, `.alerta-error`) **ya existían** en el
archivo. No hizo falta crear ni retocar ninguna: las vistas nuevas se armaron para
encajar en el diseño que ya estaba.

---

## 7. Mensajes de error y de éxito

| Mensaje | Mecanismo | Dónde se muestra |
| --- | --- | --- |
| "El campo Email es obligatorio" / formato inválido | DataAnnotations → `asp-validation-for` | StartRecovery |
| "No se encontró una cuenta asociada a ese correo electrónico." | `ViewBag.Error` | StartRecovery |
| "Token no válido." | `TempData["Error"]` | StartRecovery (tras redirect) |
| "El enlace de recuperación es inválido o ha expirado." | `TempData["Error"]` | StartRecovery (tras redirect) |
| "Las contraseñas no coinciden." | `ModelState` → `ValidationSummary` | Recovery |
| "Token inválido. Solicite un nuevo enlace de recuperación." | `TempData["Error"]` | StartRecovery (tras redirect) |
| "El enlace de recuperación se ha enviado a su correo registrado correctamente." | `TempData["MensajeExito"]` | Login |
| "Contraseña modificada con éxito. Ya puede iniciar sesión." | `TempData["MensajeExito"]` | Login |

Criterio: `ViewBag`/`ModelState` cuando se vuelve a la **misma** vista, `TempData`
cuando hay un **redirect** de por medio.

---



## 8. Cosas pendientes / a tener en cuenta

Las dejo anotadas acá para que estén a la vista y podamos decidirlas entre todos,
no porque haya que resolverlas ya:

- **El token no expira.** El mail dice "expirará en 30 minutos", pero todavía no
  hay lógica de vencimiento: haría falta una columna con la fecha de generación y
  chequearla en `Recovery`. Por ahora el único límite real es que sea de un solo uso.
- **El valor por defecto `"tokenbloqueado"` es compartido.** Como todos los usuarios
  arrancan con ese mismo valor, la búsqueda por token podría encontrar a alguien si
  ese string llegara a usarse como token en la URL. Conviene cambiarlo a `null` y
  filtrar los nulos en la consulta.
- **Credenciales SMTP hardcodeadas** en `AccountController.cs`. Lo natural sería
  moverlas a `appsettings.json` o a user-secrets, como ya hacemos con la cadena de
  conexión.
- **`POST StartRecovery` no tiene `[ValidateAntiForgeryToken]`**, mientras que
  `POST Recovery` sí. Es una inconsistencia menor, conviene unificarlo.
- **El mensaje "no se encontró una cuenta con ese correo"** confirma qué emails
  están registrados. Lo habitual es mostrar siempre el mismo mensaje neutro.
- **Quedó una variable `urlDomain` sin usar** dentro de `Sendemail` (sobró al pasar
  a `Url.Action`). Se puede borrar.


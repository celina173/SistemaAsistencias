# Revisión de la vista de alta de usuario — decisiones de diseño (ticket 3.1)

## El problema que la originó

El ticket pedía repasar `UsuarioAgregar`: validaciones, campos condicionales por rol,
mensajes de error. Al revisar el formulario (y su contraparte `UsuarioEditar`, que
comparte la misma lógica) aparecieron varios problemas encadenados: un DTO sin
reglas de validación, una librería de validación de cliente cargada pero rota,
un bug que rompía la vista al fallar una validación, y ninguna regla que exigiera
Carrera/Cohorte o Materias según el rol elegido. Se resolvió en conjunto con
[[correccion-roles-sembrados]] (ticket 3.2), porque los campos condicionales de
este formulario dependen de qué `RoId` tiene cada rol.

## Hallazgos y cómo se resolvió cada uno

### 1. Validación de cliente muerta por falta de jQuery

`_ValidationScriptsPartial` carga `jquery.validate.min.js` y
`jquery.validate.unobtrusive.min.js`, pero `_Layout.cshtml` nunca incluía jQuery
(solo `script.js` y Bootstrap Icons por CDN). Sin jQuery, esos scripts tiraban
error y no validaban nada — toda la carga recaía en el servidor.

**Solución:** agregar `<script src="~/lib/jquery/dist/jquery.min.js">` en
`_Layout.cshtml`, **antes** de `@RenderSectionAsync("Scripts")` (que es donde cada
vista incluye `_ValidationScriptsPartial`). El orden importa: los scripts se
ejecutan en el orden en que aparecen, y `jquery.validate` asume que `window.jQuery`
ya existe.

Con esto, las `DataAnnotations` que ya tenía `UsuarioCrearDto` (`Required`,
`RegularExpression`, `MaxLength`, `Range`, `EmailAddress`) empezaron a validar en
el navegador sin escribir JS nuevo — Razor genera los atributos `data-val-*` a
partir de esas anotaciones, y `jquery.validate.unobtrusive` los lee.

### 2. `UsEmail` sin mensaje de error propio

`[Required]` en `UsEmail` no tenía `ErrorMessage`, a diferencia de los demás
campos. Sin cultura fijada en `Program.cs`, no estaba garantizado que el mensaje
por defecto de ASP.NET saliera en español. Se agregó
`ErrorMessage = "Debe ingresar un Email válido"`, igual que el resto de los campos.

### 3. Bug que rompía la vista: `ViewBag` no repuesto en los errores del POST

Los `return View(model)` de `UsuarioAgregar` y `UsuarioEditar` (POST) solo
reponían `ViewBag.RolesList`. Si el rol elegido era Estudiante o Docente y algo
fallaba (por ejemplo DNI duplicado), la vista reventaba con
`NullReferenceException` al recorrer `ViewBag.CarreraCohortesList`/
`CarreraMateriasList`, que quedaban en `null`.

**Solución:** se extrajo la carga de esas 3 listas a un método privado,
`CargarListasFormularioUsuarioAsync()`, y se lo llama desde los 4 puntos que antes
duplicaban el código (GET y POST de ambas acciones). Además de arreglar el bug,
elimina la duplicación.

### 4. Sin validación condicional por rol (servidor)

Nada exigía Carrera/Cohorte para Estudiante ni materias para Docente — son
reglas cruzadas entre campos (dependen de `selectedRoleId`, que ni siquiera es
una `DataAnnotation` posible porque `RoId` no se bindea desde el formulario, ver
punto 7).

**Solución:** un chequeo manual con `ModelState.AddModelError`, **antes** de
tocar o crear la entidad `Usuario` (mismo principio que ya usa el chequeo de DNI
duplicado: validar primero, mutar después — se corrigió un primer intento en
`UsuarioEditar` que validaba después de haber tocado `usuario.CarreraMaterias`):

```csharp
if (selectedRoleId == 3 && model.CaCoId == null)
    ModelState.AddModelError("CaCoId", "El usuario debe estar asociado a una carrera/cohorte.");

if (selectedRoleId == 2 && (model.SelectedCaMaIds == null || model.SelectedCaMaIds.Count == 0))
    ModelState.AddModelError("SelectedCaMaIds", "Debe seleccionar al menos una materia para un Docente.");
```

Aplicado en los 4 lugares (Agregar y Editar). En `UsuarioEditar` el campo de
materias es un parámetro suelto del método (`selectedCaMaIds`), no una propiedad
del DTO — ver punto 6.

### 5. Sin mensaje visible para esas dos reglas

Ninguna de las dos vistas tenía `<span asp-validation-for>` para `CaCoId` ni para
`SelectedCaMaIds` — sin eso, el error quedaba en `ModelState` pero no se veía en
pantalla.

**Solución:** se agregó el span en los 4 lugares, con una diferencia técnica entre
vistas:

- `UsuarioAgregar.cshtml` usa `@model UsuarioCrearDto`, que **sí** tiene la
  propiedad `SelectedCaMaIds` → `asp-validation-for="SelectedCaMaIds"` funciona
  igual que los demás campos.
- `UsuarioEditar.cshtml` usa `@model UsuarioDetalleDto`, que **no** tiene esa
  propiedad (la lista llega como parámetro aparte del método de acción). El tag
  helper `asp-validation-for` necesita que la propiedad exista en el tipo del
  modelo de la vista, así que ahí se usó el helper clásico
  `@Html.ValidationMessage("SelectedCaMaIds", "", new { @class = "text-danger" })`,
  que busca por nombre de clave en `ModelState` sin depender del modelo.

### 6. Validación de cliente para las reglas condicionales (sin usar una `DataAnnotation`)

Estas dos reglas dependen de otro campo (el rol elegido), así que no hay ninguna
`DataAnnotation` de la que Razor pueda generar el HTML automáticamente — a
diferencia del resto de los campos, acá hizo falta JS a mano.

**Solución:** se aprovechó el mismo script que ya mostraba/ocultaba los `<div>`
según el rol (`toggleFields()`), agregándole que también ponga/saque el atributo
HTML `required` en el `<select>` correspondiente:

```js
caCoIdSelect.required = esEstudiante;
materiasSelect.required = esDocente;
```

Es importante sacar el `required` cuando el campo se oculta, no solo ponerlo
cuando se muestra: un campo `required` mientras está `display:none` puede
bloquear el envío del formulario para siempre. Probado en vivo: el navegador sí
frena el envío sin ir al servidor. `required` en un `<select multiple>` significa
"al menos una opción marcada" — comportamiento nativo, no hizo falta lógica extra
para "lista vacía".

**Mensaje en inglés (bug encontrado en la prueba en vivo):** al frenar el envío,
el mensaje mostrado era el default de la librería ("This field is required."),
no el texto en español del servidor. Como `required` es un atributo HTML plano
(no viene de una `DataAnnotation` con `data-val-required="mensaje"`), jQuery
Validate no tenía de dónde sacar un mensaje propio. Se corrigió agregando
`data-msg-required="<mismo texto que usa el servidor>"` en el `<select>` — es un
mecanismo del propio `jquery.validate.js` (`customDataMessage`, ver
`wwwroot/lib/jquery-validation/dist/jquery.validate.js`) para asociarle un
mensaje a una regla puntual de un elemento, sin necesidad de que la regla venga
de `data-val`/unobtrusive.

### 7. `UsuarioAgregar` no inicializaba el toggle al cargar

A diferencia de `UsuarioEditar` (que ya llamaba `toggleFields()` una vez al
cargar la página), `UsuarioAgregar` solo reaccionaba al evento `change`. Se
unificaron los dos scripts con la misma estructura (función nombrada + llamada
inicial), aunque el impacto era menor porque el formulario arranca sin rol
preseleccionado.

### 8. Pendientes que quedan sin resolver (fuera de este pase)

- **`userPassword` es un campo muerto**: se escribe en el formulario pero el
  controlador ignora lo tipeado y siempre usa `UsContrasena = UsDni.ToString()`.
  Se cruza con el pedido de Magali de encriptar contraseñas — a definir en otro
  ticket.
- **`RoId` en `UsuarioCrearDto`** tiene `[Required]`/`[ForeignKey]` que no hacen
  nada: el binding real del rol es el parámetro suelto `selectedRoleId`, no esa
  propiedad. Limpieza pendiente.
- **Números mágicos `2`/`3`** para Docente/Estudiante siguen hardcodeados en el
  JS y en `AdminController`. No se tocaron porque 3.2 mantuvo esos mismos
  números — sigue siendo frágil ante una futura renumeración de roles.

## Bug encontrado en la prueba en vivo (no es de este formulario, pero lo afecta)

Al probar el alta con los 3 roles nuevos contra la base compartida real, apareció
un bug en `AccountController.Login`: el `switch` por rol redirigía a
`RedirectToAction("Index", "Docente")`, pero el controlador se sigue llamando
`ProfesorController` (ruta `/Profesor`) — nunca se renombró la clase, solo el
rol. Se corrigió a `RedirectToAction("Index", "Profesor")`.

**Pendiente, no corregido:** `AccountController.CambiarContrasena` (línea ~137)
redirige siempre a `Home` sin mirar el rol, a diferencia del `Login`, que sí lo
hace. Como todo usuario nuevo tiene contraseña = DNI y pasa por ese flujo
forzado en su primer login, **cualquier alta reciente** (incluida Dirección)
termina en Home la primera vez, en vez de su panel correspondiente. Es un bug
preexistente, no introducido por 3.1/3.2, pero se detectó probando este mismo
formulario — queda anotado para quien lo tome.

## Prueba en vivo realizada

Contra la base compartida real (Railway), logueado como Admin: se creó un
usuario de cada rol nuevo (Docente, Estudiante, Dirección) desde el propio
formulario, confirmando en el navegador:
- Bloqueo del envío sin completar el campo condicional correspondiente.
- Alta exitosa al completarlo.
- Redirección correcta de cada rol tras loguearse (Docente → sus cátedras,
  Estudiante → Home, Dirección → panel Admin).
- El rename de roles no rompió usuarios ya existentes en la base.

Los usuarios de prueba se borraron al terminar (por la propia UI de "Eliminar").

## Archivos

- `ISFDyT124/Views/Shared/_Layout.cshtml` — script de jQuery
- `ISFDyT124/DTO/UsuarioCrearDto.cs` — mensaje de `UsEmail`
- `ISFDyT124/Controllers/AdminController.cs` — `CargarListasFormularioUsuarioAsync()`,
  validaciones condicionales en `UsuarioAgregar`/`UsuarioEditar` (GET y POST)
- `ISFDyT124/Views/Admin/UsuarioAgregar.cshtml`,
  `ISFDyT124/Views/Admin/UsuarioEditar.cshtml` — spans de validación, JS de
  `required` dinámico + `data-msg-required`, toggle inicial
- `ISFDyT124/Controllers/AccountController.cs` — fix del redirect a "Docente"

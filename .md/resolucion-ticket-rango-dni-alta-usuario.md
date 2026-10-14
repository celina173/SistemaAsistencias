# Resolución de Ticket: Falta validar rango de DNI en Alta de Usuario [Post-MVP]

> **Ticket:** Falta validar rango de DNI en Alta de Usuario [Post-MVP]  
> **Problema reportado:** El campo DNI del formulario de alta de usuario acepta valores negativos, cero, o de una sola cifra, sin ninguna validación de rango.  
> **Sugerencia original:** Agregar un `[Range]` razonable (ej. 1000000 a 99999999) al campo DNI del modelo/DTO.  
> **Estado:** Resuelto ✅  
> **Fecha de implementación:** 14/10/2026  

---

## 1. Diagnóstico y Causa Raíz

Al analizar el flujo de **Alta de Usuario** (`AdminController.UsuarioAgregar`), se identificaron tres niveles donde la validación de rango de DNI debía asegurarse y robustecerse:

1. **Frontend / Vista Razor (`ISFDyT124/Views/Admin/UsuarioAgregar.cshtml`):**
   * El elemento de entrada `<input asp-for="UsDni" autocomplete="new-password" value="" placeholder="Ej: 39123456">` no contaba con los atributos HTML5 `type="number"`, `min="1000000"` y `max="99999999"`.
   * En consecuencia, los controles de incremento/decremento (steppers) del navegador permitían bajar a valores negativos o `0`, y no se aplicaba la restricción nativa del navegador previa al envío del formulario.

2. **Backend / Controlador (`ISFDyT124/Controllers/AdminController.cs`):**
   * En la acción POST de `UsuarioAgregar`, se realizaban validaciones manuales para email único y DNI duplicado, pero no existía una comprobación explícita defensiva del rango numérico antes de intentar procesar el usuario.
   * Si bien `ModelState.IsValid` evalúa DataAnnotations, agregar la validación explícita con `ModelState.AddModelError` en el controlador garantiza que ningún valor menor a `1.000.000` o mayor a `99.999.999` continúe el flujo hacia la base de datos, asegurando la repoblación de `ViewBag` mediante `CargarListasFormularioUsuarioAsync()` para que la vista no colapse.

3. **Data Transfer Object (`ISFDyT124/DTO/UsuarioCrearDto.cs`):**
   * El DTO disponía de `[Range(1000000, 99999999)]`, pero el mensaje de error era genérico (`"Debe ingresar los 7-8 dígitos del DNI."`). Se mejoró la claridad del mensaje para indicar explícitamente el rango numérico válido (`1.000.000 a 99.999.999`).

---

## 2. Archivos Modificados y Cambios Implementados

### 2.1. Vista de Alta de Usuario: `ISFDyT124/Views/Admin/UsuarioAgregar.cshtml`
Se agregaron los atributos `type="number"`, `min="1000000"`, `max="99999999"` y los mensajes de validación en español (`data-msg-min`, `data-msg-max`, `data-msg-number`) al input del DNI, junto con la extensión de mensajes por defecto de `$.validator.messages`:

```html
<!-- Antes -->
<div class="input-field">
    <label asp-for="UsDni">DNI</label>
    <input asp-for="UsDni" autocomplete="new-password" value="" placeholder="Ej: 39123456">
    <span asp-validation-for="UsDni" class="text-danger"></span>
</div>

<!-- Después -->
<div class="input-field">
    <label asp-for="UsDni">DNI</label>
    <input asp-for="UsDni" type="number" min="1000000" max="99999999"
           data-msg-min="Debe ingresar un valor mayor o igual a 1000000."
           data-msg-max="Debe ingresar un valor menor o igual a 99999999."
           data-msg-number="Debe ingresar un número válido."
           autocomplete="new-password" value="" placeholder="Ej: 39123456">
    <span asp-validation-for="UsDni" class="text-danger"></span>
</div>
```

Y en la sección `@section Scripts`:
```javascript
if (window.jQuery && $.validator) {
    $.extend($.validator.messages, {
        min: $.validator.format("Debe ingresar un valor mayor o igual a {0}."),
        max: $.validator.format("Debe ingresar un valor menor o igual a {0}."),
        number: "Debe ingresar un número válido."
    });
}
```

* **Beneficio:** Bloquea en el cliente valores negativos, cero o números con menos de 7 dígitos mediante la validación nativa de HTML5 y los steppers del navegador, y asegura que tanto las reglas `min`, `max` y `number` de jQuery Validate muestren sus mensajes de error en español consistentes con el resto del formulario.

---

### 2.2. Controlador de Administración: `ISFDyT124/Controllers/AdminController.cs`
En el método `UsuarioAgregar` (POST), se añadió la validación explícita del rango numérico antes de verificar la duplicidad en la base de datos:

```csharp
// Validación de rango de DNI (previene valores negativos, cero o fuera de rango de 7-8 dígitos)
if (model.UsDni < 1000000 || model.UsDni > 99999999)
{
    ModelState.AddModelError("UsDni", "Debe ingresar un DNI válido de 7 u 8 dígitos (entre 1.000.000 y 99.999.999).");
    await CargarListasFormularioUsuarioAsync();
    return View(model);
}

if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni))
{
    ModelState.AddModelError("UsDni", "El DNI ya se encuentra registrado.");
    await CargarListasFormularioUsuarioAsync();
    return View(model);
}
```

* **Beneficio:** Blindaje del backend ante solicitudes manipuladas (insomnia/curl/postman o scripts que salteen la validación de cliente). Si el DNI es negativo, cero o menor a 1.000.000, el controlador rechaza la petición de forma segura, repobla los combos (`ViewBag`) y retorna el error amigable a la vista.

---

### 2.3. Data Transfer Object: `ISFDyT124/DTO/UsuarioCrearDto.cs`
Se estandarizó y clarificó el mensaje de error del atributo `[Range]`:

```csharp
// DNI
[RegularExpression(
    @"^[1-9][0-9]*$",
    ErrorMessage = "Sólo se permiten números de DNI válidos."
)]
[Range(1000000, 99999999, ErrorMessage = "Debe ingresar un DNI válido de 7 u 8 dígitos (entre 1.000.000 y 99.999.999).")] // Valida que el DNI tenga un rango válido entre 7 y 8 dígitos (1.000.000 a 99.999.999)
[Required(ErrorMessage = "Debe ingresar un número de DNI válido (7-8 dígitos).")] // Campo obligatorio
[Display(Name = "DNI")] // Etiqueta para mostrar en vistas y formularios
public int UsDni { get; set; }
```

* **Beneficio:** Unificación del mensaje de error tanto para la validación automática de DataAnnotations como para la validación de servidor.

---

## 3. Pruebas y Verificación

1. **Compilación de la solución:**
   * Se ejecutó `dotnet build .\ISFDyT124\ISFDyT124.csproj`.
   * **Resultado:** Compilación correcta sin errores (`0 Errores`).

2. **Casos de prueba cubiertos:**
   * **Valores negativos (ej. `-5`):** Rechazados en frontend (`min="1000000"`) y en backend por `Range` y check del controlador.
   * **Valor cero (`0`):** Rechazado en frontend y backend (fuera del rango de 1.000.000 a 99.999.999).
   * **Valores de 1 a 6 cifras (ej. `1`, `123456`):** Rechazados por ser menores al límite inferior `1.000.000`.
   * **Valores válidos de 7 y 8 dígitos (ej. `8456123`, `39123456`):** Aceptados correctamente para su procesamiento.
   * **Valores mayores a 8 dígitos (ej. `100000000`):** Rechazados por exceder el límite superior `99.999.999`.

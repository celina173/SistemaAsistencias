# Vistas de Materias, Usuarios y Carreras — investigación y solución

Se investigaron los tres módulos por el mismo motivo: sospecha de que el
CRUD no estaba mostrando el diseño real hecho por el equipo.

**Nota sobre esta investigación:** la primera revisión de Usuarios y Carreras
concluyó (mal) que ya estaban bien, basándose en que el código tenía nombres
de clases CSS que *parecían* del diseño real (`.form-card-agregar`,
`.main-container-agregar`, `.input-group`, etc.) y en que las páginas cargaban
sin errores de servidor. Al ver capturas reales del navegador quedó claro que
esas clases **no existen en el CSS** — la página carga bien (200, sin
excepciones), pero se ve completamente pelada, porque el navegador ignora en
silencio cualquier clase que no tenga una regla definida. La lección: revisar
el código fuente no alcanza para confirmar que un diseño se ve bien: hay que
mirar el resultado renderizado.

## Módulo Materias — tenía el problema

### El problema

Al entrar a **Materias** desde el panel Admin, no se veía el diseño del resto
de la aplicación: aparecía una tabla pelada, sin estilos, con textos como
"Index", "Create New", "Edit | Details | Delete" en inglés.

La causa no era un problema de CSS ni de configuración del servidor — el
archivo de estilos se sirve correctamente y el resto de la app (Carreras,
Usuarios, la Auditoría de Docentes) se ve bien. El problema era específico
de **Materias**: las 5 vistas (`Index`, `Create`, `Edit`, `Details`, `Delete`
en `Views/Materias/`) eran las **plantillas genéricas que genera
automáticamente el andamiaje** de Visual Studio / `dotnet-aspnet-codegenerator`
al crear un controlador con vistas CRUD — nadie las había reemplazado nunca
por el diseño real. No usaban el layout compartido (`_Layout.cshtml`) ni
ninguna de las clases visuales del resto del sistema.

Al revisar el proyecto se encontró que **el diseño real ya existía**, hecho
por un compañero, pero como un prototipo HTML suelto sin conectar a ningún
controlador:

- `Views/Admin/gestion-materias.cshtml` (la grilla)
- `Views/Admin/agregar-materia.cshtml` (el formulario de alta)
- `Views/Admin/modificar-materia.cshtml` (el formulario de edición)

Eran páginas HTML completas y autónomas (no usaban `_Layout`), con datos de
ejemplo hardcodeados, links a páginas `.html` que no existen en el proyecto
(`inicio.html`, `index.html`, `gestion-materias.html`), y campos de formulario
que **no existen en el modelo real** de `Materia` (código de materia, carrera,
año/cuatrimestre, carga horaria en horas) — el modelo real solo tiene
`MaDenominacion`, `MaModalidad` y `MaCantModulos`.

### Cómo se solucionó

Se tomó el diseño visual de esos tres archivos (mismas clases CSS, misma
estructura, mismos íconos) y se convirtió en las 5 vistas Razor reales que
usa `MateriasController`, adaptando lo que hacía falta:

1. **Se sacó todo el HTML que ya provee el layout compartido** (header,
   logo, menú lateral) — eso ya lo pone `_Layout.cshtml` automáticamente,
   no hay que repetirlo en cada vista. Cada vista ahora solo define
   `ViewData["Title"]` y `ViewData["BodyClass"]`, más su contenido propio.

2. **Se reemplazaron los campos inventados por los campos reales** del
   modelo `Materia` — la grilla y los formularios muestran y editan
   `MaDenominacion` (nombre), `MaModalidad` y `MaCantModulos`, en vez de
   código/carrera/año-cuatrimestre que no existen como columnas.

3. **Se conectaron los formularios a Entity Framework de verdad**, usando
   los helpers de ASP.NET Core (`asp-for`, `asp-validation-for`,
   `asp-action`) en vez de inputs sueltos sin `name` real — así el token
   antifalsificación (`[ValidateAntiForgeryToken]` que ya tenía el
   controlador) funciona, y las validaciones del modelo (largo máximo,
   caracteres permitidos, rango de módulos entre 1 y 4) se muestran en
   pantalla en vez de fallar en silencio.

4. **`Details` y `Delete` no tenían mockup propio** (los compañeros solo
   habían maquetado la grilla y el alta/edición) — se armaron con la misma
   familia de clases (`.main-container-form`, `.form-card`) para que las 5
   pantallas de Materias se vean coherentes entre sí, siguiendo el mismo
   criterio con el que ya está resuelto en `Carreras`.

Los archivos de mockup originales (`gestion-materias.cshtml`,
`agregar-materia.cshtml`, `modificar-materia.cshtml`) quedan sin usar en
`Views/Admin/` — no se borraron, pero ya no hace falta tocarlos.

### Verificación

Se probó el flujo completo como Admin, de punta a punta (no solo que
compile): entrar al listado, dar de alta una materia nueva, verla en la
grilla, editarla, ver su detalle, y eliminarla — confirmando en cada paso
que el dato quedaba bien guardado en la base y que la pantalla mostraba el
diseño real, no la plantilla genérica.

### Archivos modificados

- `ISFDyT124/Views/Materias/Index.cshtml`
- `ISFDyT124/Views/Materias/Create.cshtml`
- `ISFDyT124/Views/Materias/Edit.cshtml`
- `ISFDyT124/Views/Materias/Details.cshtml`
- `ISFDyT124/Views/Materias/Delete.cshtml`

## Módulo Usuarios — también tenía el problema (los formularios)

`AdminController` maneja Usuarios sin controlador de scaffolding propio
(`UsuariosABM`, `UsuarioAgregar`, `UsuarioEditar`, `UsuarioEliminar`), con
vistas en `Views/Admin/`. El **listado** (`UsuariosABM.cshtml`) sí se veía
bien (`.admin-banner`, `.crud-table` son clases reales). Pero
`UsuarioAgregar.cshtml` y `UsuarioEditar.cshtml` usaban `.main-container-agregar`,
`.form-card-agregar`, `.input-group`, `.btn-guardar-estudiante`,
`.btn-cancelar`, `.custom-select` — **ninguna de esas seis clases existe en
`style.css`**. En el navegador se veían como un formulario sin ningún estilo
(inputs pelados, sin tarjeta, sin layout de columnas), tal cual se confirmó
con capturas de pantalla reales.

### Cómo se solucionó

Se migraron los dos formularios a la misma familia de clases que ya se había
confirmado funcionando en Materias (`.main-container-form`, `.form-card`,
`.input-field`, `.form-buttons`, `.btn-save`, `.btn-cancel`), sin tocar la
lógica de negocio (el JavaScript que muestra/oculta "Carrera / Cohorte" o
"Materias Asignadas" según el rol elegido se mantuvo igual). De paso se
corrigió que `UsuarioEditar` no dejaba preseleccionado el rol actual del
usuario en el combo (`SelectList` no recibía el valor actual) — sin eso, al
entrar a editar un Alumno o un Profesor, los campos extra de esos roles no
aparecían hasta tocar el combo.

`UsuarioEliminar` sigue sin pantalla de confirmación propia — se dispara por
`confirm()` de JavaScript directo desde el botón de la lista, eso no cambió.

## Módulo Carreras — también tenía el problema (formularios y detalle)

El **listado** (`Views/Carreras/Index.cshtml`) ya usaba clases reales
(`.admin-banner`, `.crud-table`). Pero `Create.cshtml` y `Edit.cshtml` tenían
el mismo problema que Usuarios (`.main-container-agregar`, `.form-card-agregar`,
`.btn-guardar-estudiante`, `.btn-cancelar` inexistentes), y `Details.cshtml` /
`Delete.cshtml` usaban una grilla de Bootstrap (`<dl class="row">`,
`.col-sm-2`, `.col-sm-10`) que tampoco hace nada porque **Bootstrap no está
cargado en el proyecto** — solo se linkea el ícono (`bootstrap-icons`), nunca
`bootstrap.css`. Por eso el Detalle se veía como texto plano apilado, sin
ninguna grilla.

Este mismo error de Bootstrap sin cargar también estaba en las primeras
versiones de `Materias/Details.cshtml` y `Materias/Delete.cshtml` que se
habían armado copiando este mismo patrón de Carreras — se corrigieron
también, reemplazando el `<dl class="row">` por inputs de solo lectura
dentro de `.input-field` (reutilizando el estilo "dashed" que el CSS ya
define para campos `readonly`).

### Cómo se solucionó

Las 4 vistas (`Create`, `Edit`, `Details`, `Delete`) pasaron a la misma
familia de clases que Materias y Usuarios. Se agregó además la regla
`.text-danger` al CSS (tampoco existía — los mensajes de validación de
jQuery Unobtrusive se mostraban sin color, invisibles al lado del resto del
texto), usando el mismo rojo que ya usa toda la app para acciones
destructivas (`#ef4444`).

(Quedan, sin usar, los prototipos HTML sueltos `gestion-carreras.cshtml`,
`agregar-carrera.cshtml`, `modificar-carrera.cshtml` en `Views/Admin/` — no
se tocaron.)

## Los 3 listados no se veían iguales entre sí

Después de arreglar los formularios, quedó un problema más: **los listados**
de Materias, Carreras y Usuarios usaban tres combinaciones de clases
distintas para lo mismo — `.info-banner` vs `.admin-banner`,
`.main-container-global` vs `.main-container-gestion`, con o sin
`.page-title-container`. Cada uno se veía "bien" por separado (todas esas
clases sí existen), pero no se veían iguales entre sí.

Se unificaron los 3 (`Views/Materias/Index.cshtml`, `Views/Carreras/Index.cshtml`,
`Views/Admin/UsuariosABM.cshtml`) a la misma estructura exacta: `.info-banner`
de dos columnas, `.page-title-container` con el título de la sección,
`.main-container-global`, y `.global-table` con el mismo estilo de ícono de
edición (SVG) + emoji para detalle/eliminar.

### Verificación

Se probó en vivo, logueado como Admin: los 3 listados devuelven `200`, con
las mismas 4 clases estructurales presentes en los tres, y los datos de
prueba existentes (2 carreras, 3 usuarios) se siguen mostrando correctamente
después del cambio.

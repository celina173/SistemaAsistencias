# Vistas de Materias — el problema y cómo se solucionó

## El problema

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

## Cómo se solucionó

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

## Verificación

Se probó el flujo completo como Admin, de punta a punta (no solo que
compile): entrar al listado, dar de alta una materia nueva, verla en la
grilla, editarla, ver su detalle, y eliminarla — confirmando en cada paso
que el dato quedaba bien guardado en la base y que la pantalla mostraba el
diseño real, no la plantilla genérica.

## Archivos modificados

- `ISFDyT124/Views/Materias/Index.cshtml`
- `ISFDyT124/Views/Materias/Create.cshtml`
- `ISFDyT124/Views/Materias/Edit.cshtml`
- `ISFDyT124/Views/Materias/Details.cshtml`
- `ISFDyT124/Views/Materias/Delete.cshtml`

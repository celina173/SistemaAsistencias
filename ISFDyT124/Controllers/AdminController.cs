using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ISFDyT124.Controllers
{
    [Authorize(Roles = "Admin,Dirección")]
    public class AdminController : Controller
    {
        private readonly InstitutoDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(InstitutoDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Carga en ViewBag las 3 listas que necesita el formulario de alta/edición
        /// de Usuario (roles, carrera/cohorte y carrera/materia). Se llama tanto en
        /// el GET como en cada rama de error del POST: si no se repone acá, la vista
        /// revienta con NullReferenceException al recorrer las listas vacías.
        /// </summary>
        private async Task CargarListasFormularioUsuarioAsync()
        {
            ViewBag.RolesList = await _context.Roles.ToListAsync();
            ViewBag.CarreraCohortesList = await _context
                .CarreraCohortes.Include(cc => cc.Carrera)
                .Include(cc => cc.Cohorte)
                .Select(cc => new
                {
                    cc.CaCoId,
                    Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio,
                })
                .ToListAsync();

            // Cargar todas las filas y filtrar las inválidas en memoria para poder loguearlas
            var materiasRaw = await _context
                .CarreraMaterias
                .Include(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(cm => cm.Materia)
                .ToListAsync();

            var materiasLimpias = materiasRaw
                .Where(cm =>
                    cm.CaMaId > 0 &&
                    cm.Materia != null &&
                    !string.IsNullOrWhiteSpace(cm.Materia.MaDenominacion))
                .Select(cm => new
                {
                    cm.CaMaId,
                    Denominacion =
                        (cm.CarreraCohorte != null && cm.CarreraCohorte.Carrera != null
                            ? cm.CarreraCohorte.Carrera.CaDenominacion
                            : "Sin carrera")
                        + " / " + cm.Materia!.MaDenominacion
                })
                .ToList();

            var excluidos = materiasRaw
                .Where(cm => cm.CaMaId <= 0 || cm.Materia == null || string.IsNullOrWhiteSpace(cm.Materia.MaDenominacion))
                .Select(cm => new { cm.CaMaId, cm.MaId })
                .ToList();

            if (excluidos.Any())
            {
                _logger.LogWarning("CarreraMaterias inválidas excluidas al poblar formulario. CaMaId/MaId: {Items}", 
                    string.Join(",", excluidos.Select(e => $"{e.CaMaId}/{e.MaId}")));
            }

            ViewBag.CarreraMateriasList = materiasLimpias;
        }

        // Admin es exclusivo del desarrollo (ticket 3.34): el alta de usuario no debe
        // poder crear otro Admin. Se filtra en una lista aparte en vez de tocar
        // ViewBag.RolesList del método de arriba porque UsuarioEditar sigue necesitando
        // la lista completa (si no, al editar un Admin existente se perdería la opción
        // de su propio rol en el select).
        private async Task CargarListasFormularioAltaUsuarioAsync()
        {
            await CargarListasFormularioUsuarioAsync();
            ViewBag.RolesList = ((List<Rol>)ViewBag.RolesList).Where(r => r.RoId != RolId.Admin).ToList();
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalAlumnos = await _context.Usuarios.Where(u => u.RoId == RolId.Estudiante).CountAsync();

            ViewBag.TotalDocentes = await _context.Usuarios.Where(u => u.RoId == RolId.Docente).CountAsync();

            ViewBag.TotalMaterias = await _context.Materias.CountAsync();
            ViewBag.TotalCarreras = await _context.Carreras.CountAsync();
            ViewBag.TotalAsistencias = await _context.Asistencias.CountAsync();

            var hoy = DateTime.Today;
            ViewBag.AsistenciasHoy = await _context
                .Asistencias.Where(a => a.AsFecha != null && a.AsFecha.Value.Date == hoy)
                .CountAsync();

            return View();
        }

        /// <summary>
        /// Auditoría de docentes: por cada (docente, cátedra asignada), cuántas fechas
        /// distintas cargó asistencia y cuál fue la última vez. No hay en el modelo una
        /// fecha de carga real ni quién cargó cada registro, así que esto es una auditoría
        /// de actividad (¿usa el sistema?), no de cumplimiento contra un calendario.
        /// </summary>
        public async Task<IActionResult> AuditoriaDocentes()
        {
            var docentes = await _context
                .Usuarios.Where(u => u.RoId == RolId.Docente)
                .Include(u => u.CarreraMaterias)
                    .ThenInclude(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(u => u.CarreraMaterias)
                    .ThenInclude(cm => cm.Materia)
                .ToListAsync();

            var filas = new List<AuditoriaDocenteDto>();

            foreach (var docente in docentes)
            {
                foreach (var catedra in docente.CarreraMaterias)
                {
                    // Antes se contaban todos los alumnos de cualquier cohorte de la misma
                    // Carrera (CarreraMateria no sabía a qué cohorte pertenecía la cátedra).
                    // Ahora que la cátedra tiene su propio CaCoId, se cuenta solo esa cohorte.
                    var cantidadAlumnos = catedra.CaCoId.HasValue
                        ? await _context.Usuarios.CountAsync(u =>
                            u.RoId == RolId.Estudiante && u.CaCoId == catedra.CaCoId.Value
                        )
                        : 0;

                    // Se matchea por MaId (no CaMaId): ProfesorController guarda las
                    // asistencias con MaId y deja CaMaId en null.
                    var fechas = await _context
                        .Asistencias.Where(a => a.MaId == catedra.MaId && a.AsFecha != null)
                        .Select(a => a.AsFecha!.Value.Date)
                        .Distinct()
                        .ToListAsync();

                    filas.Add(
                        new AuditoriaDocenteDto
                        {
                            UsId = docente.UsId,
                            DocenteNombre = $"{docente.UsApellido}, {docente.UsNombre}",
                            CaMaId = catedra.CaMaId,
                            CarreraDenominacion = catedra.CarreraCohorte?.Carrera?.CaDenominacion ?? "-",
                            MateriaDenominacion = catedra.Materia?.MaDenominacion ?? "-",
                            CantidadAlumnos = cantidadAlumnos,
                            CantidadFechasCargadas = fechas.Count,
                            UltimaFechaCargada = fechas.Count > 0 ? fechas.Max() : null,
                        }
                    );
                }
            }

            var ordenadas = filas
                .OrderBy(f => f.UltimaFechaCargada.HasValue ? 1 : 0)
                .ThenBy(f => f.UltimaFechaCargada)
                .ToList();

            return View(ordenadas);
        }

        public async Task<IActionResult> UsuariosABM()
        {
            var usuarios = await _context
                .Usuarios.Include(u => u.Rol)
                .Include(u => u.CarreraCohorte)
                    .ThenInclude(cc => cc.Carrera)
                .Include(u => u.CarreraCohorte)
                    .ThenInclude(cc => cc.Cohorte)
                .Include(u => u.CarreraMaterias)
                    .ThenInclude(cm => cm.CarreraCohorte)
                    .ThenInclude(cc => cc!.Carrera)
                .Include(u => u.CarreraMaterias)
                    .ThenInclude(cm => cm.Materia)
                .Select(u => new UsuarioDetalleDto
                {
                    UsId = u.UsId,
                    UsApellido = u.UsApellido,
                    UsNombre = u.UsNombre,
                    UsEmail = u.UsEmail,
                    UsDni = u.UsDni,
                    RoId = u.RoId,
                    RoDenominacion = u.Rol != null ? u.Rol.RoDenominacion : null,
                    CaCoId = u.CaCoId,
                    CarreraCohorteDenominacion =
                        u.CaCoId != null && u.CarreraCohorte != null
                            ? u.CarreraCohorte.Carrera.CaDenominacion
                                + " - "
                                + u.CarreraCohorte.Cohorte.CoAnio
                            : null,
                    MateriasDenominacion = u.CarreraMaterias.Any()
                        ? string.Join(
                            ", ",
                            u.CarreraMaterias.Select(cm =>
                                (cm.CarreraCohorte != null
                                    ? cm.CarreraCohorte.Carrera!.CaDenominacion
                                    : "Sin carrera")
                                + " / " + cm.Materia!.MaDenominacion
                            )
                        )
                        : null,
                })
                .ToListAsync();

            return View(usuarios);
        }

        [HttpGet]
        public async Task<IActionResult> UsuarioAgregar()
        {
            await CargarListasFormularioAltaUsuarioAsync();
            return View(new UsuarioCrearDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioAgregar(UsuarioCrearDto model, int selectedRoleId)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }

            // Admin es exclusivo del desarrollo (ticket 3.34): se valida en el servidor,
            // no solo ocultando la opción en el <select>, porque nada impide mandar
            // selectedRoleId=1 por POST sin pasar por la vista.
            if (selectedRoleId == RolId.Admin)
            {
                ModelState.AddModelError("selectedRoleId", "No se puede asignar el rol Admin desde el alta de usuario.");
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }

            // Validación de email único (previene duplicados a nivel de aplicación)
            if (!string.IsNullOrWhiteSpace(model.UsEmail))
            {
                var emailExists = await _context.Usuarios.AnyAsync(u => u.UsEmail == model.UsEmail);
                if (emailExists)
                {
                    ModelState.AddModelError("UsEmail", "El email ya se encuentra registrado.");
                    await CargarListasFormularioAltaUsuarioAsync();
                    return View(model);
                }
            }

            if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni))
            {
                ModelState.AddModelError("UsDni", "El DNI ya se encuentra registrado.");
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }

            if (selectedRoleId == 3 && model.CaCoId == null)
            {
                ModelState.AddModelError(
                    "CaCoId",
                    "El usuario debe estar asociado a una carrera/cohorte."
                );
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }

            if (selectedRoleId == 2 && (model.SelectedCaMaIds == null || model.SelectedCaMaIds.Count == 0))
            {
                ModelState.AddModelError(
                    "SelectedCaMaIds",
                    "Debe seleccionar al menos una materia para un Docente."
                );
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }

            using var transaccionId = await _context.Database.BeginTransactionAsync();

            int nuevoUsId = await _context
                .Database.SqlQuery<int>(
                    $"SELECT ISNULL(MAX(UsId), 0) + 1 AS Value FROM Usuarios WITH (UPDLOCK, HOLDLOCK)"
                )
                .FirstAsync();

            var usuario = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDni = model.UsDni,
                UsEmail = model.UsEmail,
                UsContrasena = PasswordService.HashPassword(model.UsDni.ToString()),
                RoId = selectedRoleId,
                CaCoId = selectedRoleId == 3 ? model.CaCoId : null,
            };

            try
            {
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                if (selectedRoleId == 2 && model.SelectedCaMaIds != null)
                {
                    _context.Entry(usuario).Collection(u => u.CarreraMaterias).IsLoaded = true;

                    var materias = await _context.CarreraMaterias
                        .Where(cm => model.SelectedCaMaIds.Contains(cm.CaMaId) && cm.CaMaId > 0 && cm.Materia != null)
                        .ToListAsync();

                    var invalidIds = model.SelectedCaMaIds.Except(materias.Select(m => m.CaMaId)).ToList();
                    if (invalidIds.Any())
                    {
                        _logger.LogWarning("Se ignoraron CaMaIds inválidos seleccionados por UI: {Ids}", string.Join(",", invalidIds));
                    }
                    foreach (var cm in materias)
                    {
                        usuario.CarreraMaterias.Add(cm);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaccionId.CommitAsync();

                // Añadir mensaje de éxito
                TempData["SuccessMessage"] = $"Usuario '{usuario.UsApellido}, {usuario.UsNombre}' creado correctamente.";

                return RedirectToAction(nameof(UsuariosABM));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException al guardar usuario. Dni: {Dni}, RoleId: {RoleId}, UsId: {UsId}", model.UsDni, selectedRoleId, usuario?.UsId);
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar el usuario. Verifique que todos los campos obligatorios estén completos e intente nuevamente."
                );
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear usuario. RoleId: {RoleId}, Dni: {Dni}", selectedRoleId, model.UsDni);
                throw;
            }

            return RedirectToAction(nameof(UsuariosABM));
        }

        [HttpGet]
        public async Task<IActionResult> UsuarioEditar(int id)
        {
            var usuario = await _context
                .Usuarios.Include(u => u.Rol)
                .Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null)
                return NotFound();

            await CargarListasFormularioUsuarioAsync();

            var dto = new UsuarioEditarDto
            {
                UsId = usuario.UsId,
                UsApellido = usuario.UsApellido,
                UsNombre = usuario.UsNombre,
                UsEmail = usuario.UsEmail,
                UsDni = usuario.UsDni,
                RoId = usuario.RoId,
                CaCoId = usuario.CaCoId,
                MateriasDenominacion = string.Join(
                    ",",
                    usuario.CarreraMaterias.Select(cm => cm.CaMaId)
                ),
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEditar(
            int id,
            UsuarioEditarDto model,
            int selectedRoleId,
            List<int>? selectedCaMaIds
        )
        {
            if (id != model.UsId)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await CargarListasFormularioUsuarioAsync();
                return View(model);
            }

            var usuario = await _context
                .Usuarios.Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null)
                return NotFound();

            // Validación de email único al editar (ignora el propio usuario)
            if (!string.IsNullOrWhiteSpace(model.UsEmail))
            {
                var emailExists = await _context.Usuarios.AnyAsync(u => u.UsEmail == model.UsEmail && u.UsId != id);
                if (emailExists)
                {
                    ModelState.AddModelError("UsEmail", "El email ya se encuentra registrado por otro usuario.");
                    await CargarListasFormularioUsuarioAsync();
                    return View(model);
                }
            }

            if (selectedRoleId == 3 && model.CaCoId == null)
            {
                ModelState.AddModelError(
                    "CaCoId",
                    "El usuario debe estar asociado a una carrera/cohorte."
                );
                await CargarListasFormularioUsuarioAsync();
                return View(model);
            }

            if (selectedRoleId == 2 && (selectedCaMaIds == null || selectedCaMaIds.Count == 0))
            {
                ModelState.AddModelError(
                    "SelectedCaMaIds",
                    "Debe seleccionar al menos una materia para un Docente."
                );
                await CargarListasFormularioUsuarioAsync();
                return View(model);
            }

            usuario.UsApellido = model.UsApellido;
            usuario.UsNombre = model.UsNombre;
            usuario.UsDni = model.UsDni;
            usuario.UsEmail = model.UsEmail;
            usuario.RoId = selectedRoleId;
            usuario.CaCoId = selectedRoleId == 3 ? model.CaCoId : null;

            if (selectedRoleId == 2)
            {
                usuario.CarreraMaterias.Clear();
                if (selectedCaMaIds != null)
                {
                    var materias = await _context
                        .CarreraMaterias.Where(cm => selectedCaMaIds.Contains(cm.CaMaId))
                        .ToListAsync();
                    foreach (var cm in materias)
                    {
                        usuario.CarreraMaterias.Add(cm);
                    }
                }
            }
            else
            {
                usuario.CarreraMaterias.Clear();
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException al editar usuario. UsId: {UsId}, Dni: {Dni}", id, model.UsDni);
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar el usuario. Verifique que todos los campos obligatorios estén completos e intente nuevamente."
                );
                await CargarListasFormularioUsuarioAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Usuario '{usuario.UsApellido}, {usuario.UsNombre}' actualizado correctamente.";
            return RedirectToAction(nameof(UsuariosABM));
        }

        [HttpGet]
        public async Task<IActionResult> UsuarioEliminar(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }



        [HttpPost, ActionName("UsuarioEliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEliminarConfirmado(int id)
        {
            var usuario = await _context
                .Usuarios.Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario != null)
            {
                int usuarioLogueadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                if (usuario.UsId == usuarioLogueadoId)
                {
                    TempData["Error"] = "No podés eliminar tu propio usuario.";
                    return RedirectToAction(nameof(UsuariosABM));
                }

                if (usuario.RoId == RolId.Admin && await _context.Usuarios.CountAsync(u => u.RoId == RolId.Admin) <= 1)
                {
                    TempData["Error"] = "No se puede eliminar el último Admin del sistema.";
                    return RedirectToAction(nameof(UsuariosABM));
                }

                usuario.CarreraMaterias.Clear();
                _context.Usuarios.Remove(usuario);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "No se pudo eliminar el usuario. Puede tener datos relacionados que lo impiden.";
                    return RedirectToAction(nameof(UsuariosABM));
                }

                // Mensaje de éxito al eliminar
                TempData["SuccessMessage"] = "Usuario eliminado correctamente.";
                return RedirectToAction(nameof(UsuariosABM));
            }

            return RedirectToAction(nameof(UsuariosABM));
        }

        #region Carga Masiva de Alumnos vía Excel

        private async Task CargarListasCargaMasivaAsync()
        {
            ViewBag.CarreraCohortesList = await _context
                .CarreraCohortes.Include(cc => cc.Carrera)
                .Include(cc => cc.Cohorte)
                .Select(cc => new
                {
                    cc.CaCoId,
                    Denominacion = cc.Carrera.CaDenominacion + " - " + cc.Cohorte.CoAnio,
                })
                .ToListAsync();
        }

        private string UsuarioImportacionId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(ClaimTypes.Email)
               ?? User.Identity?.Name
               ?? string.Empty;

        private IActionResult ImportacionExpirada()
        {
            TempData["Error"] = "La importación no existe, expiró o no pertenece al usuario actual. Inicie nuevamente el paso 1.";
            return RedirectToAction(nameof(CargaMasivaAlumnos));
        }

        [HttpGet]
        [Route("Importacion/Paso1")]
        public async Task<IActionResult> CargaMasivaAlumnos()
        {
            await CargarListasCargaMasivaAsync();
            return View(new CargaMasivaPaso1Dto());
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerMateriasPorCarreraCohorte(int caCoId)
        {
            var cc = await _context.CarreraCohortes.FirstOrDefaultAsync(x => x.CaCoId == caCoId);
            if (cc == null) return Json(new List<object>());

            var materias = await _context.CarreraMaterias
                .Where(cm => cm.CaCoId == cc.CaCoId && cm.CaMaId > 0 && cm.Materia != null && !string.IsNullOrWhiteSpace(cm.Materia.MaDenominacion))
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    caMaId = cm.CaMaId,
                    denominacion = (cm.CarreraCohorte != null && cm.CarreraCohorte.Carrera != null ? cm.CarreraCohorte.Carrera.CaDenominacion + " / " : string.Empty)
                                  + cm.Materia!.MaDenominacion,
                    modalidad = cm.Materia!.MaModalidad,
                    modulos = cm.Materia.MaCantModulos,
                    anio = cm.CaMaAnio
                })
                .ToListAsync();

            return Json(materias);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Importacion/Paso2")]
        public async Task<IActionResult> CargaMasivaMapear(CargaMasivaPaso1Dto model)
        {
            if (model.CaCoId == null)
            {
                ModelState.AddModelError("CaCoId", "Debe seleccionar una Carrera / Cohorte.");
            }

            if (model.SelectedCaMaIds == null || !model.SelectedCaMaIds.Any())
            {
                ModelState.AddModelError("SelectedCaMaIds", "Debe seleccionar al menos una materia para inscribir a los estudiantes.");
            }

            if (model.ArchivoExcel == null || model.ArchivoExcel.Length == 0)
            {
                ModelState.AddModelError("ArchivoExcel", "Debe seleccionar un archivo Excel (.xlsx).");
            }
            else
            {
                var extension = Path.GetExtension(model.ArchivoExcel.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    ModelState.AddModelError("ArchivoExcel", "El archivo debe tener formato .xlsx (Excel).");
                }
                else if (model.ArchivoExcel.Length > ExcelImportService.TamanoMaximoArchivo)
                {
                    ModelState.AddModelError("ArchivoExcel", "El archivo no puede superar los 10 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarListasCargaMasivaAsync();
                return View("CargaMasivaAlumnos", model);
            }

            string tempToken;
            string tempPath;
            try
            {
                tempToken = ExcelImportService.GuardarArchivoTemporal(model.ArchivoExcel!);
                tempPath = ExcelImportService.ObtenerRutaArchivoTemporal(tempToken);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ArchivoExcel", $"Error al guardar el archivo temporal: {ex.Message}");
                await CargarListasCargaMasivaAsync();
                return View("CargaMasivaAlumnos", model);
            }

            List<string> columnas;
            List<Dictionary<string, string>> previewRows;
            try
            {
                var leido = ExcelImportService.LeerCabecerasYPreview(tempPath);
                columnas = leido.Columnas;
                previewRows = leido.PreviewRows;

                if (!columnas.Any())
                {
                    ExcelImportService.EliminarArchivoTemporal(tempToken);
                    ModelState.AddModelError("ArchivoExcel", "El archivo Excel no contiene cabeceras o filas legibles.");
                    await CargarListasCargaMasivaAsync();
                    return View("CargaMasivaAlumnos", model);
                }
            }
            catch (Exception ex)
            {
                ExcelImportService.EliminarArchivoTemporal(tempToken);
                ModelState.AddModelError("ArchivoExcel", $"No se pudo leer el archivo Excel: {ex.Message}");
                await CargarListasCargaMasivaAsync();
                return View("CargaMasivaAlumnos", model);
            }

            // Obtener denominación de la Carrera/Cohorte
            var cc = await _context.CarreraCohortes
                .Include(c => c.Carrera)
                .Include(c => c.Cohorte)
                .FirstOrDefaultAsync(c => c.CaCoId == model.CaCoId.Value);

            string ccDenom = cc != null
                ? $"{cc.Carrera.CaDenominacion} - {cc.Cohorte.CoAnio}"
                : "Carrera seleccionada";

            // Obtener nombres de materias seleccionadas
            var nombresMaterias = await _context.CarreraMaterias
                .Where(cm => model.SelectedCaMaIds.Contains(cm.CaMaId))
                .Include(cm => cm.Materia)
                .Select(cm => cm.Materia.MaDenominacion)
                .ToListAsync();

            var importId = Guid.Parse(tempToken);
            var estado = new ImportacionMasivaEstado
            {
                ImportId = importId,
                UsuarioId = UsuarioImportacionId(),
                CreadoUtc = DateTime.UtcNow,
                UltimoAccesoUtc = DateTime.UtcNow,
                CaCoId = model.CaCoId.Value,
                SelectedCaMaIds = model.SelectedCaMaIds.Distinct().ToList(),
                CarreraCohorteDenominacion = ccDenom,
                MateriasSeleccionadasNombres = nombresMaterias,
                ColumnasExcel = columnas,
                PreviewRows = previewRows
            };

            // Inferir mapeo inicial y persistir todo el estado antes de redirigir.
            var (dniSugerido, apellidoSugerido, nombreSugerido, emailSugerido) = ExcelImportService.InferirMapeo(columnas);
            estado.ColumnaDni = dniSugerido;
            estado.ColumnaApellido = apellidoSugerido;
            estado.ColumnaNombre = nombreSugerido;
            estado.ColumnaEmail = emailSugerido;
            ExcelImportService.GuardarEstado(estado);
            return RedirectToAction(nameof(CargaMasivaPaso2), new { importId });
        }

        [HttpGet("Importacion/Paso2/{importId:guid}")]
        public IActionResult CargaMasivaPaso2(Guid importId)
        {
            var estado = ExcelImportService.ObtenerEstado(importId, UsuarioImportacionId());
            if (estado == null) return ImportacionExpirada();
            return View("CargaMasivaMapear", CrearMapeoDto(estado));
        }

        [HttpPost("Importacion/Paso2/{importId:guid}")]
        [ValidateAntiForgeryToken]
        public IActionResult CargaMasivaGuardarMapeo(Guid importId, CargaMasivaMapeoDto model)
        {
            var estado = ExcelImportService.ObtenerEstado(importId, UsuarioImportacionId());
            if (estado == null) return ImportacionExpirada();
            if (string.IsNullOrWhiteSpace(model.ColumnaDni) || string.IsNullOrWhiteSpace(model.ColumnaApellido) ||
                string.IsNullOrWhiteSpace(model.ColumnaNombre) || string.IsNullOrWhiteSpace(model.ColumnaEmail))
            {
                ModelState.AddModelError(string.Empty, "Debe mapear las 4 columnas obligatorias (DNI, Apellido, Nombre y Email).");
                model = CrearMapeoDto(estado);
                model.ColumnaDni = Request.Form[nameof(model.ColumnaDni)].ToString();
                model.ColumnaApellido = Request.Form[nameof(model.ColumnaApellido)].ToString();
                model.ColumnaNombre = Request.Form[nameof(model.ColumnaNombre)].ToString();
                model.ColumnaEmail = Request.Form[nameof(model.ColumnaEmail)].ToString();
                return View("CargaMasivaMapear", model);
            }
            estado.ColumnaDni = model.ColumnaDni;
            estado.ColumnaApellido = model.ColumnaApellido;
            estado.ColumnaNombre = model.ColumnaNombre;
            estado.ColumnaEmail = model.ColumnaEmail;
            ExcelImportService.GuardarEstado(estado);
            return RedirectToAction(nameof(CargaMasivaPaso3), new { importId });
        }

        private static CargaMasivaMapeoDto CrearMapeoDto(ImportacionMasivaEstado estado)
            => new()
            {
                ImportId = estado.ImportId,
                TempFileToken = estado.ImportId.ToString("N"),
                CaCoId = estado.CaCoId,
                CarreraCohorteDenominacion = estado.CarreraCohorteDenominacion,
                SelectedCaMaIds = estado.SelectedCaMaIds,
                MateriasSeleccionadasNombres = estado.MateriasSeleccionadasNombres,
                ColumnasExcel = estado.ColumnasExcel,
                ColumnaDni = estado.ColumnaDni,
                ColumnaApellido = estado.ColumnaApellido,
                ColumnaNombre = estado.ColumnaNombre,
                ColumnaEmail = estado.ColumnaEmail,
                PreviewRows = estado.PreviewRows
            };

        [HttpGet("Importacion/Paso3/{importId:guid}")]
        public IActionResult CargaMasivaPaso3(Guid importId)
        {
            var estado = ExcelImportService.ObtenerEstado(importId, UsuarioImportacionId());
            if (estado == null) return ImportacionExpirada();
            var filas = ExcelImportService.ValidarYLeerFilas(
                ExcelImportService.ObtenerRutaArchivoTemporal(importId.ToString("N")),
                estado.ColumnaDni, estado.ColumnaApellido, estado.ColumnaNombre, estado.ColumnaEmail);
            return View("CargaMasivaResultado", new CargaMasivaResultadoDto
            {
                EsConfirmacion = true,
                ImportId = importId,
                CarreraCohorteDenominacion = estado.CarreraCohorteDenominacion,
                TotalFilas = filas.TotalFilas,
                MateriasInscriptas = estado.MateriasSeleccionadasNombres
            });
        }

        [HttpPost("Importacion/Paso3/{importId:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCargaMasiva(Guid importId)
        {
            var estado = ExcelImportService.ObtenerEstado(importId, UsuarioImportacionId());
            if (estado == null) return ImportacionExpirada();
            var model = CrearMapeoDto(estado);
            return await CargaMasivaProcesar(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargaMasivaProcesar(CargaMasivaMapeoDto model)
        {
            if (!Guid.TryParse(model.TempFileToken, out var importId))
                return ImportacionExpirada();
            var estado = ExcelImportService.ObtenerEstado(importId, UsuarioImportacionId());
            if (estado == null) return ImportacionExpirada();

            model = CrearMapeoDto(estado);
            var tempPath = ExcelImportService.ObtenerRutaArchivoTemporal(model.TempFileToken);
            if (!System.IO.File.Exists(tempPath))
            {
                TempData["Error"] = "El archivo temporal ha expirado. Por favor, suba el archivo nuevamente.";
                return RedirectToAction(nameof(CargaMasivaAlumnos));
            }

            if (string.IsNullOrWhiteSpace(model.ColumnaDni) ||
                string.IsNullOrWhiteSpace(model.ColumnaApellido) ||
                string.IsNullOrWhiteSpace(model.ColumnaNombre) ||
                string.IsNullOrWhiteSpace(model.ColumnaEmail))
            {
                ModelState.AddModelError("", "Debe mapear las 4 columnas obligatorias (DNI, Apellido, Nombre y Email).");
                return View("CargaMasivaMapear", model);
            }

            // Validar y parsear filas del Excel
            var parseResult = ExcelImportService.ValidarYLeerFilas(
                tempPath,
                model.ColumnaDni,
                model.ColumnaApellido,
                model.ColumnaNombre,
                model.ColumnaEmail
            );

            // Obtener denominación para el reporte
            var cc = await _context.CarreraCohortes
                .Include(c => c.Carrera)
                .Include(c => c.Cohorte)
                .FirstOrDefaultAsync(c => c.CaCoId == model.CaCoId);

            string ccDenom = cc != null
                ? $"{cc.Carrera.CaDenominacion} - {cc.Cohorte.CoAnio}"
                : "Carrera seleccionada";

            var nombresMaterias = await _context.CarreraMaterias
                .Where(cm => model.SelectedCaMaIds.Contains(cm.CaMaId))
                .Include(cm => cm.Materia)
                .Select(cm => cm.Materia.MaDenominacion)
                .ToListAsync();

            // Si hay filas válidas, comprobar conflictos de DNI con la base de datos
            var dnis = parseResult.FilasValidas.Select(f => f.Dni).Distinct().ToList();
            var usuariosExistentes = await _context.Usuarios
                .Where(u => dnis.Contains(u.UsDni))
                .ToListAsync();
            var dictUsuarios = usuariosExistentes.ToDictionary(u => u.UsDni);

            foreach (var fila in parseResult.FilasValidas)
            {
                if (dictUsuarios.TryGetValue(fila.Dni, out var userExistente))
                {
                    // Si existe pero no es Alumno (RoId != 3) -> es error
                    if (userExistente.RoId != 3)
                    {
                        var rolNombre = userExistente.RoId == RolId.Docente ? "Docente" : (userExistente.RoId == RolId.Admin ? "Admin" : "Dirección");
                        parseResult.Errores.Add(new CargaMasivaFilaErrorDto
                        {
                            Fila = fila.Fila,
                            Dni = fila.Dni.ToString(),
                            Apellido = fila.Apellido,
                            Nombre = fila.Nombre,
                            Email = fila.Email,
                            Motivo = $"El DNI pertenece a un usuario existente con rol {rolNombre}. No puede registrarse como Estudiante."
                        });
                    }
                }
            }

            // ESTRATEGIA TODO O NADA: Si hay al menos un error, abortar sin guardar nada
            if (parseResult.Errores.Any())
            {
                ExcelImportService.EliminarImportacion(importId);

                // Ordenar errores por número de fila
                parseResult.Errores = parseResult.Errores.OrderBy(e => e.Fila).ToList();

                var resultadoError = new CargaMasivaResultadoDto
                {
                    EsExitoso = false,
                    Mensaje = $"Se detectaron {parseResult.Errores.Count} filas con errores en el archivo. La carga fue abortada sin aplicar cambios en la base de datos.",
                    CarreraCohorteDenominacion = ccDenom,
                    TotalFilas = parseResult.TotalFilas,
                    Errores = parseResult.Errores,
                    MateriasInscriptas = nombresMaterias
                };

                return View("CargaMasivaResultado", resultadoError);
            }

            // Si no hay filas de datos
            if (!parseResult.FilasValidas.Any())
            {
                ExcelImportService.EliminarImportacion(importId);
                var resultadoVacio = new CargaMasivaResultadoDto
                {
                    EsExitoso = false,
                    Mensaje = "El archivo no contenía filas con datos válidos para procesar.",
                    CarreraCohorteDenominacion = ccDenom,
                    TotalFilas = 0
                };
                return View("CargaMasivaResultado", resultadoVacio);
            }

            // TRANSACCIÓN: Aplicar cambios en la base de datos
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // UPDLOCK+HOLDLOCK: mismo motivo que en UsuarioAgregar (ticket 6.13) -- sin
                // esto, una alta individual simultánea a esta carga masiva podía leer el
                // mismo MAX y chocar de UsId con la primera fila del lote.
                int maxUsId = await _context
                    .Database.SqlQuery<int>(
                        $"SELECT ISNULL(MAX(UsId), 0) AS Value FROM Usuarios WITH (UPDLOCK, HOLDLOCK)"
                    )
                    .FirstAsync();
                int alumnosNuevos = 0;
                int alumnosReutilizados = 0;
                int inscripcionesCreadas = 0;
                var listaProcesados = new List<CargaMasivaAlumnoProcesadoDto>();

                // Precargar inscripciones existentes de los alumnos existentes en las materias seleccionadas
                var existingUsIds = usuariosExistentes.Select(u => u.UsId).ToList();
                var inscripcionesExistentes = await _context.Inscripciones
                    .Where(i => existingUsIds.Contains(i.UsId) && model.SelectedCaMaIds.Contains(i.CaMaId))
                    .ToListAsync();
                var setInscripciones = new HashSet<(int UsId, int CaMaId)>(
                    inscripcionesExistentes.Select(i => (i.UsId, i.CaMaId))
                );

                foreach (var fila in parseResult.FilasValidas)
                {
                    int currentUsId;
                    string estadoAlumno;
                    int inscripcionesFila = 0;

                    if (dictUsuarios.TryGetValue(fila.Dni, out var existingUser))
                    {
                        currentUsId = existingUser.UsId;
                        estadoAlumno = "Alumno existente";
                        alumnosReutilizados++;

                        // Si no tenía carrera asignada, asignarle la carrera elegida
                        if (existingUser.CaCoId == null)
                        {
                            existingUser.CaCoId = model.CaCoId;
                        }
                    }
                    else
                    {
                        maxUsId++;
                        currentUsId = maxUsId;
                        estadoAlumno = "Nuevo alumno";
                        alumnosNuevos++;

                        var nuevoUsuario = new Usuario
                        {
                            UsId = currentUsId,
                            UsDni = fila.Dni,
                            UsApellido = fila.Apellido,
                            UsNombre = fila.Nombre,
                            UsEmail = fila.Email,
                            UsContrasena = PasswordService.HashPassword(fila.Dni.ToString()),
                            RoId = RolId.Estudiante,
                            CaCoId = model.CaCoId
                        };
                        _context.Usuarios.Add(nuevoUsuario);
                    }

                    // Inscribir en las materias seleccionadas que no tenga aún
                    foreach (var caMaId in model.SelectedCaMaIds)
                    {
                        if (!setInscripciones.Contains((currentUsId, caMaId)))
                        {
                            _context.Inscripciones.Add(new Inscripciones
                            {
                                UsId = currentUsId,
                                CaMaId = caMaId
                            });
                            setInscripciones.Add((currentUsId, caMaId));
                            inscripcionesCreadas++;
                            inscripcionesFila++;
                        }
                    }

                    listaProcesados.Add(new CargaMasivaAlumnoProcesadoDto
                    {
                        Fila = fila.Fila,
                        Dni = fila.Dni,
                        NombreCompleto = $"{fila.Apellido}, {fila.Nombre}",
                        Email = fila.Email,
                        Estado = estadoAlumno,
                        MateriasInscriptas = inscripcionesFila
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                ExcelImportService.EliminarImportacion(importId);

                var resultadoExitoso = new CargaMasivaResultadoDto
                {
                    EsExitoso = true,
                    Mensaje = $"Se procesaron correctamente {parseResult.FilasValidas.Count} estudiantes ({alumnosNuevos} nuevos y {alumnosReutilizados} existentes). Se crearon {inscripcionesCreadas} inscripciones a materias.",
                    CarreraCohorteDenominacion = ccDenom,
                    TotalFilas = parseResult.FilasValidas.Count,
                    AlumnosNuevos = alumnosNuevos,
                    AlumnosReutilizados = alumnosReutilizados,
                    InscripcionesCreadas = inscripcionesCreadas,
                    MateriasInscriptas = nombresMaterias,
                    AlumnosProcesados = listaProcesados
                };

                return View("CargaMasivaResultado", resultadoExitoso);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ExcelImportService.EliminarImportacion(importId);

                var resultadoFallo = new CargaMasivaResultadoDto
                {
                    EsExitoso = false,
                    // No se expone ex.Message al usuario: podía filtrar detalles internos
                    // (nombres de tabla/columna) de la excepción de base de datos (ticket 6.14).
                    Mensaje = "Ocurrió un error inesperado al guardar los datos. Se cancelaron todas las operaciones.",
                    CarreraCohorteDenominacion = ccDenom,
                    TotalFilas = parseResult.FilasValidas.Count
                };

                return View("CargaMasivaResultado", resultadoFallo);
            }
        }

        #endregion
    }
}

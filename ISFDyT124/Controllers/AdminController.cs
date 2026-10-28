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
        // 1. AGREGA 'string? userPassword' COMO PARÁMETRO PARA RECIBIR EL INPUT DEL FORMULARIO[cite: 6]
        public async Task<IActionResult> UsuarioAgregar(UsuarioCrearDto model, int selectedRoleId, string? userPassword)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }

            if (selectedRoleId == RolId.Admin)
            {
                ModelState.AddModelError("selectedRoleId", "No se puede asignar el rol Admin desde el alta de usuario.");
                await CargarListasFormularioAltaUsuarioAsync();
                return View(model);
            }

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

            // 2. REEMPLAZA LA CREACIÓN DEL OBJETO "Usuario" CON ESTA LÓGICA:
            bool requiereActivacion = string.IsNullOrEmpty(userPassword);
            string tokenGenerado = requiereActivacion ? Guid.NewGuid().ToString("N") : "tokenbloqueado"; //[cite: 3]
            string contrasenaFinal = requiereActivacion ? Guid.NewGuid().ToString() : userPassword!;

            var usuario = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDni = model.UsDni,
                UsEmail = model.UsEmail,
                UsContrasena = PasswordService.HashPassword(contrasenaFinal),
                UsTokenRecovery = tokenGenerado, //[cite: 3]
                UsTokenRecoveryVencimiento = requiereActivacion ? DateTime.UtcNow.AddHours(24) : null, //[cite: 3]
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

                // 3. ENVÍA EL CORREO JUSTO DESPUÉS DE ASEGURARTE DE QUE LA TRANSACCIÓN SE GUARDÓ
                if (requiereActivacion && !string.IsNullOrWhiteSpace(usuario.UsEmail))
                {
                    try
                    {
                        SendWelcomeEmail(usuario.UsEmail, tokenGenerado);
                        TempData["SuccessMessage"] = $"Usuario '{usuario.UsApellido}, {usuario.UsNombre}' creado correctamente. Se envió un correo de activación.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar correo de activación al usuario {UsId}", usuario.UsId);
                        TempData["SuccessMessage"] = $"Usuario '{usuario.UsApellido}' creado, pero no se pudo enviar el correo de activación.";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = $"Usuario '{usuario.UsApellido}, {usuario.UsNombre}' creado correctamente.";
                }

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

        [HttpGet]
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
                    modulos = cm.Materia.MaCantModulos
                })
                .ToListAsync();

            return Json(materias);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargaMasivaMapear(CargaMasivaPaso1Dto model)
        {
            var materiasSeleccionadas = model.SelectedCaMaIds ?? new List<int>();
            model.SelectedCaMaIds = materiasSeleccionadas;
            if (model.CaCoId == null)
            {
                ModelState.AddModelError("CaCoId", "Debe seleccionar una Carrera / Cohorte.");
            }

            if (!materiasSeleccionadas.Any())
            {
                ModelState.AddModelError("SelectedCaMaIds", "Debe seleccionar al menos una materia para inscribir a los estudiantes.");
            }

            if (model.FechaInscripcion.HasValue && model.FechaInscripcion.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("FechaInscripcion", "La fecha de inscripción no puede ser posterior al día de hoy.");
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
                else if (model.ArchivoExcel.Length > ExcelImportService.MaximoBytes)
                {
                    ModelState.AddModelError("ArchivoExcel", "El archivo no puede superar los 5 MB.");
                }
            }

            var caCoIdSeleccionado = model.CaCoId.GetValueOrDefault();
            var materiasValidas = model.CaCoId.HasValue
                ? await _context.CarreraMaterias
                    .Where(cm => cm.CaCoId == caCoIdSeleccionado && materiasSeleccionadas.Contains(cm.CaMaId))
                    .Select(cm => cm.CaMaId)
                    .ToListAsync()
                : new List<int>();
            if (materiasSeleccionadas.Distinct().Count() != materiasValidas.Count)
            {
                ModelState.AddModelError("SelectedCaMaIds", "Todas las materias deben pertenecer a la carrera y cohorte seleccionadas.");
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
                tempToken = ExcelImportService.GuardarArchivoTemporal(model.ArchivoExcel!, User.FindFirstValue(ClaimTypes.NameIdentifier));
                tempPath = ExcelImportService.ObtenerRutaArchivoTemporal(tempToken);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ArchivoExcel", $"Error al guardar el archivo temporal: {ex.Message}");
                await CargarListasCargaMasivaAsync();
                return View("CargaMasivaAlumnos", model);
            }

            List<CargaMasivaHojaDto> hojas;
            try
            {
                hojas = ExcelImportService.ObtenerHojas(tempPath);
                foreach (var hoja in hojas.Where(h => h.TieneDatos))
                {
                    var leida = ExcelImportService.LeerHoja(tempPath, hoja.Nombre);
                    CompletarHojaMapeo(hoja, leida, aplicarSugerido: true);
                    // Sólo se marca de entrada la pestaña donde el sistema reconoció los cuatro datos.
                    hoja.Incluir = MapeoCompleto(hoja);
                }

                if (!hojas.Any(h => h.TieneDatos))
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
                .FirstOrDefaultAsync(c => c.CaCoId == caCoIdSeleccionado);

            string ccDenom = cc != null
                ? $"{cc.Carrera!.CaDenominacion} - {cc.Cohorte!.CoAnio}"
                : "Carrera seleccionada";

            // Obtener nombres de materias seleccionadas
            var nombresMaterias = await _context.CarreraMaterias
                .Where(cm => materiasSeleccionadas.Contains(cm.CaMaId))
                .Include(cm => cm.Materia)
                .Select(cm => cm.Materia!.MaDenominacion)
                .ToListAsync();

            var mapeoDto = new CargaMasivaMapeoDto
            {
                TempFileToken = tempToken,
                CaCoId = caCoIdSeleccionado,
                FechaInscripcion = model.FechaInscripcion?.Date,
                CarreraCohorteDenominacion = ccDenom,
                SelectedCaMaIds = materiasSeleccionadas,
                MateriasSeleccionadasNombres = nombresMaterias,
                Hojas = hojas
            };

            return View("CargaMasivaMapear", mapeoDto);
        }

        private static bool MapeoCompleto(CargaMasivaHojaDto hoja) =>
            hoja.ColDni > 0 && hoja.ColApellido > 0 && hoja.ColEmail > 0 && (hoja.ApellidoYNombre || hoja.ColNombre > 0);

        private static void CompletarHojaMapeo(CargaMasivaHojaDto hoja, ExcelImportService.HojaLeida leida, bool aplicarSugerido)
        {
            hoja.FilaCabecera = leida.FilaCabecera;
            hoja.Columnas = leida.Columnas;
            hoja.PreviewRows = leida.PreviewRows;
            if (!aplicarSugerido) return;

            hoja.ColDni = leida.Sugerido.Dni;
            hoja.ColApellido = leida.Sugerido.Apellido;
            hoja.ColNombre = leida.Sugerido.Nombre;
            hoja.ColEmail = leida.Sugerido.Email;
            hoja.ApellidoYNombre = leida.Sugerido.ApellidoYNombre;
        }

        /// <summary>
        /// Vuelve a leer del archivo las columnas y la muestra de cada pestaña para volver a mostrar el mapeo
        /// sin perder lo que el usuario había elegido.
        /// </summary>
        private static void RehidratarHojasMapeo(CargaMasivaMapeoDto model, string tempPath)
        {
            var delArchivo = ExcelImportService.ObtenerHojas(tempPath);
            var posteadas = model.Hojas ?? new List<CargaMasivaHojaDto>();
            var resultado = new List<CargaMasivaHojaDto>();

            foreach (var real in delArchivo)
            {
                var posteada = posteadas.FirstOrDefault(h => string.Equals(h.Nombre, real.Nombre, StringComparison.OrdinalIgnoreCase));
                var hoja = posteada ?? real;
                hoja.Nombre = real.Nombre;
                hoja.TotalFilas = real.TotalFilas;
                hoja.TieneDatos = real.TieneDatos;
                if (real.TieneDatos)
                {
                    var leida = ExcelImportService.LeerHoja(tempPath, real.Nombre, hoja.FilaCabecera > 0 ? hoja.FilaCabecera : null);
                    CompletarHojaMapeo(hoja, leida, aplicarSugerido: posteada == null);
                }
                else
                {
                    hoja.Incluir = false;
                }
                resultado.Add(hoja);
            }

            model.Hojas = resultado;
        }

        [HttpGet]
        public IActionResult CargaMasivaColumnas(string token, string hoja, int? fila)
        {
            if (!ExcelImportService.ArchivoPerteneceA(token, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty))
                return NotFound();

            try
            {
                var leida = ExcelImportService.LeerHoja(ExcelImportService.ObtenerRutaArchivoTemporal(token), hoja, fila);
                return Json(new
                {
                    filaCabecera = leida.FilaCabecera,
                    columnas = leida.Columnas,
                    filas = leida.PreviewRows,
                    sugerido = new
                    {
                        dni = leida.Sugerido.Dni,
                        apellido = leida.Sugerido.Apellido,
                        nombre = leida.Sugerido.Nombre,
                        email = leida.Sugerido.Email,
                        apellidoYNombre = leida.Sugerido.ApellidoYNombre
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron leer las columnas de la pestaña {Hoja}", hoja);
                return BadRequest();
            }
        }

        private async Task CompletarCatalogosPrevisualizacionAsync(CargaMasivaPrevisualizacionDto model)
        {
            model.Carreras = await _context.CarreraCohortes
                .Include(cc => cc.Carrera).Include(cc => cc.Cohorte)
                .OrderBy(cc => cc.Carrera!.CaDenominacion).ThenBy(cc => cc.Cohorte!.CoAnio)
                .Select(cc => new CargaMasivaCarreraOpcionDto
                {
                    CaCoId = cc.CaCoId,
                    Denominacion = cc.Carrera!.CaDenominacion + " - " + cc.Cohorte!.CoAnio
                }).ToListAsync();

            model.Materias = await _context.CarreraMaterias
                .Include(cm => cm.Materia)
                .OrderBy(cm => cm.Materia!.MaDenominacion)
                .Select(cm => new CargaMasivaMateriaOpcionDto
                {
                    CaMaId = cm.CaMaId,
                    CaCoId = cm.CaCoId,
                    Denominacion = cm.Materia!.MaDenominacion
                }).ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargaMasivaPrevisualizar(CargaMasivaMapeoDto model)
        {
            var tempPath = ExcelImportService.ObtenerRutaArchivoTemporal(model.TempFileToken);
            if (!System.IO.File.Exists(tempPath) || !ExcelImportService.ArchivoPerteneceA(model.TempFileToken, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty))
            {
                TempData["Error"] = "El archivo temporal ha expirado. Vuelva a subirlo.";
                return RedirectToAction(nameof(CargaMasivaAlumnos));
            }

            model.Hojas ??= new List<CargaMasivaHojaDto>();
            var seleccion = model.Hojas.Where(h => h.Incluir).ToList();

            if (seleccion.Count == 0)
            {
                ModelState.AddModelError("", "Marque al menos una pestaña para cargar.");
                RehidratarHojasMapeo(model, tempPath);
                return View("CargaMasivaMapear", model);
            }

            var hojasDisponibles = ExcelImportService.ObtenerHojas(tempPath)
                .Where(h => h.TieneDatos).Select(h => h.Nombre).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (seleccion.Select(h => h.Nombre).Distinct(StringComparer.OrdinalIgnoreCase).Count() != seleccion.Count
                || seleccion.Any(h => !hojasDisponibles.Contains(h.Nombre)))
            {
                ModelState.AddModelError("", "La selección de pestañas no coincide con el archivo cargado.");
                RehidratarHojasMapeo(model, tempPath);
                return View("CargaMasivaMapear", model);
            }

            // Cada pestaña elegida necesita cuatro columnas distintas (tres si el apellido y el nombre vienen juntos).
            var hayErrorDeMapeo = false;
            foreach (var hoja in seleccion)
            {
                var columnas = new List<int> { hoja.ColDni, hoja.ColApellido, hoja.ColEmail };
                if (!hoja.ApellidoYNombre) columnas.Add(hoja.ColNombre);
                if (hoja.FilaCabecera < 1 || columnas.Any(c => c < 1) || columnas.Distinct().Count() != columnas.Count)
                {
                    ModelState.AddModelError("", $"Pestaña '{hoja.Nombre}': indique la fila de títulos y una columna distinta para DNI, apellido, nombre y correo.");
                    hayErrorDeMapeo = true;
                }
            }
            if (hayErrorDeMapeo)
            {
                RehidratarHojasMapeo(model, tempPath);
                return View("CargaMasivaMapear", model);
            }

            var mapeos = seleccion.Select(h => new ExcelImportService.MapeoHoja
            {
                Nombre = h.Nombre,
                FilaCabecera = h.FilaCabecera,
                Columnas = new ExcelImportService.MapeoColumnas
                {
                    Dni = h.ColDni,
                    Apellido = h.ColApellido,
                    Nombre = h.ApellidoYNombre ? 0 : h.ColNombre,
                    Email = h.ColEmail,
                    ApellidoYNombre = h.ApellidoYNombre
                }
            }).ToList();

            ResultadoValidacionExcel parseResult;
            try
            {
                parseResult = ExcelImportService.ValidarYLeerFilasDeHojas(tempPath, mapeos);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo leer el Excel de carga masiva");
                ModelState.AddModelError("", "No se pudo leer el archivo. Verifique que sea un Excel válido y no esté protegido.");
                RehidratarHojasMapeo(model, tempPath);
                return View("CargaMasivaMapear", model);
            }

            var erroresEstructurales = parseResult.Errores.Where(e =>
                string.IsNullOrWhiteSpace(e.Dni) && string.IsNullOrWhiteSpace(e.Apellido)
                && string.IsNullOrWhiteSpace(e.Nombre) && string.IsNullOrWhiteSpace(e.Email)).ToList();
            if (erroresEstructurales.Count > 0)
            {
                foreach (var error in erroresEstructurales)
                    ModelState.AddModelError("", $"{(string.IsNullOrWhiteSpace(error.Hoja) ? "Archivo" : error.Hoja)}: {error.Motivo}");
                RehidratarHojasMapeo(model, tempPath);
                return View("CargaMasivaMapear", model);
            }

            var preview = new CargaMasivaPrevisualizacionDto
            {
                TempFileToken = model.TempFileToken,
                HojasSeleccionadas = seleccion.Select(h => h.Nombre).ToList(),
                // Copia de la lista: si fuera la misma, el error de conflicto entre pestañas que se agrega
                // más abajo volvería a leerse como si fuera una fila del archivo con errores y aparecería
                // como una fila editable vacía en la previsualización.
                Errores = parseResult.Errores.ToList()
            };

            foreach (var grupo in parseResult.FilasValidas.GroupBy(f => f.Dni))
            {
                var primera = grupo.First();
                if (grupo.Any(f => !string.Equals(f.Apellido, primera.Apellido, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(f.Nombre, primera.Nombre, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(f.Email, primera.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    preview.Errores.Add(new CargaMasivaFilaErrorDto
                    {
                        Hoja = string.Join(", ", grupo.Select(f => f.Hoja).Distinct()), Fila = primera.Fila,
                        Dni = primera.Dni.ToString(), Motivo = "El DNI aparece en varias pestañas con datos personales diferentes."
                    });
                    foreach (var filaConflicto in grupo)
                    {
                        preview.Alumnos.Add(new CargaMasivaAlumnoEditableDto
                        {
                            Hoja = filaConflicto.Hoja, Fila = filaConflicto.Fila, Dni = filaConflicto.Dni.ToString(),
                            Apellido = filaConflicto.Apellido, Nombre = filaConflicto.Nombre, Email = filaConflicto.Email,
                            CaCoId = model.CaCoId, FechaInscripcion = model.FechaInscripcion?.Date,
                            SelectedCaMaIds = model.SelectedCaMaIds.Distinct().ToList()
                        });
                    }
                    continue;
                }

                preview.Alumnos.Add(new CargaMasivaAlumnoEditableDto
                {
                    Hoja = string.Join(", ", grupo.Select(f => f.Hoja).Distinct()), Fila = primera.Fila,
                    Dni = primera.Dni.ToString(), Apellido = primera.Apellido, Nombre = primera.Nombre,
                    Email = primera.Email, CaCoId = model.CaCoId, FechaInscripcion = model.FechaInscripcion?.Date,
                    SelectedCaMaIds = model.SelectedCaMaIds.Distinct().ToList()
                });
            }

            foreach (var error in parseResult.Errores.Where(e => e.Fila > 1 &&
                         (!string.IsNullOrWhiteSpace(e.Dni) || !string.IsNullOrWhiteSpace(e.Apellido)
                          || !string.IsNullOrWhiteSpace(e.Nombre) || !string.IsNullOrWhiteSpace(e.Email))))
            {
                preview.Alumnos.Add(new CargaMasivaAlumnoEditableDto
                {
                    Hoja = error.Hoja ?? string.Empty,
                    Fila = error.Fila,
                    Dni = error.Dni ?? string.Empty,
                    Apellido = error.Apellido ?? string.Empty,
                    Nombre = error.Nombre ?? string.Empty,
                    Email = error.Email ?? string.Empty,
                    CaCoId = model.CaCoId,
                    FechaInscripcion = model.FechaInscripcion?.Date,
                    SelectedCaMaIds = model.SelectedCaMaIds.Distinct().ToList()
                });
            }

            await CompletarCatalogosPrevisualizacionAsync(preview);
            return View("CargaMasivaPrevisualizar", preview);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargaMasivaConfirmar(CargaMasivaPrevisualizacionDto model)
        {
            var propietario = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (!ExcelImportService.ArchivoPerteneceA(model.TempFileToken, propietario))
            {
                TempData["Error"] = "La carga temporal no existe, venció o pertenece a otra sesión.";
                return RedirectToAction(nameof(CargaMasivaAlumnos));
            }

            model.Alumnos ??= new List<CargaMasivaAlumnoEditableDto>();
            model.Errores = new List<CargaMasivaFilaErrorDto>();
            model.Alumnos = model.Alumnos.Where(a => a.Incluir).Take(ExcelImportService.MaximoFilas + 1).ToList();
            if (model.Alumnos.Count == 0)
                model.Errores.Add(new CargaMasivaFilaErrorDto { Fila = 1, Motivo = "Debe incluir al menos un alumno." });
            if (model.Alumnos.Count > ExcelImportService.MaximoFilas)
                model.Errores.Add(new CargaMasivaFilaErrorDto { Fila = 1, Motivo = $"La carga supera el máximo de {ExcelImportService.MaximoFilas} alumnos." });

            var carrerasIds = model.Alumnos.Select(a => a.CaCoId).Distinct().ToList();
            var carrerasValidas = await _context.CarreraCohortes
                .Where(cc => carrerasIds.Contains(cc.CaCoId))
                .Select(cc => cc.CaCoId).ToListAsync();
            var materiasIds = model.Alumnos.SelectMany(a => a.SelectedCaMaIds).Distinct().ToList();
            var materias = await _context.CarreraMaterias
                .Include(cm => cm.Materia)
                .Where(cm => materiasIds.Contains(cm.CaMaId))
                .ToDictionaryAsync(cm => cm.CaMaId);

            var filasValidas = new List<(CargaMasivaAlumnoEditableDto Fila, int Dni)>();
            var dnisVistos = new HashSet<int>();
            var emailsVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nombreValido = new System.Text.RegularExpressions.Regex(@"^[\p{L}\p{M}\s'’-]+$");
            foreach (var fila in model.Alumnos)
            {
                var errores = new List<string>();
                fila.Dni ??= string.Empty;
                fila.Apellido ??= string.Empty;
                fila.Nombre ??= string.Empty;
                fila.Email ??= string.Empty;
                fila.SelectedCaMaIds ??= new List<int>();
                var dniLimpio = fila.Dni.Replace(".", "").Replace("-", "").Replace(" ", "");
                if (!int.TryParse(dniLimpio, out var dni) || dni < 1000000 || dni > 99999999)
                    errores.Add("DNI inválido");
                else if (!dnisVistos.Add(dni)) errores.Add("DNI duplicado en la previsualización");
                if (string.IsNullOrWhiteSpace(fila.Apellido) || fila.Apellido.Trim().Length > 100 || !nombreValido.IsMatch(fila.Apellido.Trim())) errores.Add("Apellido inválido");
                if (string.IsNullOrWhiteSpace(fila.Nombre) || fila.Nombre.Trim().Length > 100 || !nombreValido.IsMatch(fila.Nombre.Trim())) errores.Add("Nombre inválido");
                fila.Email = fila.Email?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(fila.Email))
                {
                    if (fila.Email.Length > 254 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(fila.Email))
                        errores.Add("Correo electrónico inválido");
                    else if (!emailsVistos.Add(fila.Email))
                        errores.Add("Correo electrónico duplicado en la previsualización");
                }
                if (!carrerasValidas.Contains(fila.CaCoId)) errores.Add("Carrera/cohorte inexistente");
                if (fila.FechaInscripcion.HasValue && fila.FechaInscripcion.Value.Date > DateTime.Today)
                    errores.Add("La fecha de inscripción no puede ser posterior a hoy");
                else if (fila.FechaInscripcion.HasValue && fila.FechaInscripcion.Value.Date < new DateTime(2000, 1, 1))
                    errores.Add("Fecha de inscripción inválida");
                fila.SelectedCaMaIds = fila.SelectedCaMaIds.Distinct().ToList();
                if (fila.SelectedCaMaIds.Count == 0) errores.Add("Debe seleccionar al menos una materia");
                if (fila.SelectedCaMaIds.Any(id => !materias.TryGetValue(id, out var materia) || materia.CaCoId != fila.CaCoId))
                    errores.Add("Hay materias que no pertenecen a la carrera/cohorte elegida");

                if (errores.Count > 0)
                    model.Errores.Add(new CargaMasivaFilaErrorDto { Hoja = fila.Hoja, Fila = fila.Fila, Dni = fila.Dni, Apellido = fila.Apellido, Nombre = fila.Nombre, Email = fila.Email, Motivo = string.Join(" | ", errores) });
                else
                    filasValidas.Add((fila, dni));
            }

            var dnis = filasValidas.Select(x => x.Dni).ToList();
            var emails = filasValidas.Select(x => x.Fila.Email).ToList();
            var usuarios = await _context.Usuarios.Where(u => dnis.Contains(u.UsDni) || emails.Contains(u.UsEmail!)).ToListAsync();
            var porDni = usuarios.GroupBy(u => u.UsDni).ToDictionary(g => g.Key, g => g.First());
            var porEmail = usuarios.Where(u => u.UsEmail != null).GroupBy(u => u.UsEmail!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in filasValidas)
            {
                var fila = item.Fila;
                var errores = new List<string>();
                if (porEmail.TryGetValue(fila.Email, out var usuarioEmail) && usuarioEmail.UsDni != item.Dni)
                    errores.Add("El correo electrónico ya pertenece a otro DNI");
                if (porDni.TryGetValue(item.Dni, out var existente))
                {
                    if (existente.RoId != RolId.Estudiante) errores.Add("El DNI pertenece a un usuario con otro rol");
                    if (!existente.UsActivo) errores.Add("El alumno está inactivo");
                    if (existente.CaCoId.HasValue && existente.CaCoId.Value != fila.CaCoId) errores.Add("El alumno pertenece a otra carrera/cohorte");
                    if (!string.Equals(existente.UsEmail?.Trim(), fila.Email, StringComparison.OrdinalIgnoreCase)) errores.Add("El correo no coincide con el registrado para este DNI");
                }
                if (errores.Count > 0)
                    model.Errores.Add(new CargaMasivaFilaErrorDto { Hoja = fila.Hoja, Fila = fila.Fila, Dni = fila.Dni, Apellido = fila.Apellido, Nombre = fila.Nombre, Email = fila.Email, Motivo = string.Join(" | ", errores) });
            }

            if (model.Errores.Count > 0)
            {
                await CompletarCatalogosPrevisualizacionAsync(model);
                return View("CargaMasivaPrevisualizar", model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var maxUsId = await _context.Database.SqlQuery<int>($"SELECT ISNULL(MAX(UsId), 0) AS Value FROM Usuarios WITH (UPDLOCK, HOLDLOCK)").FirstAsync();
                var existentesIds = porDni.Values.Where(u => u.RoId == RolId.Estudiante).Select(u => u.UsId).ToList();
                var inscripcionesExistentes = await _context.Inscripciones
                    .Where(i => existentesIds.Contains(i.UsId) && materiasIds.Contains(i.CaMaId))
                    .Select(i => new { i.UsId, i.CaMaId }).ToListAsync();
                var existentesSet = new HashSet<(int, int)>(inscripcionesExistentes.Select(i => (i.UsId, i.CaMaId)));
                var procesados = new List<CargaMasivaAlumnoProcesadoDto>();
                var nuevos = 0; var reutilizados = 0; var creadas = 0;

                foreach (var item in filasValidas)
                {
                    var fila = item.Fila;
                    Usuario usuario;
                    var estado = "Alumno existente";
                    if (!porDni.TryGetValue(item.Dni, out usuario!))
                    {
                        usuario = new Usuario
                        {
                            UsId = ++maxUsId, UsDni = item.Dni, UsApellido = fila.Apellido.Trim(), UsNombre = fila.Nombre.Trim(),
                            UsEmail = fila.Email, UsContrasena = PasswordService.HashPassword(item.Dni.ToString()),
                            RoId = RolId.Estudiante, CaCoId = fila.CaCoId, UsActivo = true
                        };
                        _context.Usuarios.Add(usuario); porDni[item.Dni] = usuario; nuevos++; estado = "Nuevo alumno";
                    }
                    else
                    {
                        if (usuario.CaCoId == null) usuario.CaCoId = fila.CaCoId;
                        reutilizados++;
                    }

                    var inscripcionesFila = 0;
                    foreach (var materiaId in fila.SelectedCaMaIds)
                    {
                        if (existentesSet.Add((usuario.UsId, materiaId)))
                        {
                            _context.Inscripciones.Add(new Inscripciones
                            {
                                UsId = usuario.UsId,
                                CaMaId = materiaId,
                                InFechaInscripcion = fila.FechaInscripcion?.Date ?? DateTime.Today
                            });
                            creadas++; inscripcionesFila++;
                        }
                    }
                    procesados.Add(new CargaMasivaAlumnoProcesadoDto { Fila = fila.Fila, Dni = item.Dni, NombreCompleto = $"{fila.Apellido}, {fila.Nombre}", Email = fila.Email, Estado = estado, MateriasInscriptas = inscripcionesFila });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                ExcelImportService.EliminarArchivoTemporal(model.TempFileToken);
                var carreras = await _context.CarreraCohortes.Include(cc => cc.Carrera).Include(cc => cc.Cohorte).Where(cc => carrerasIds.Contains(cc.CaCoId)).ToListAsync();
                return View("CargaMasivaResultado", new CargaMasivaResultadoDto
                {
                    EsExitoso = true, TotalFilas = procesados.Count, AlumnosNuevos = nuevos, AlumnosReutilizados = reutilizados,
                    InscripcionesCreadas = creadas, AlumnosProcesados = procesados,
                    CarreraCohorteDenominacion = string.Join("; ", carreras.Select(cc => $"{cc.Carrera?.CaDenominacion} - {cc.Cohorte?.CoAnio}")),
                    MateriasInscriptas = materias.Values.Select(m => m.Materia?.MaDenominacion ?? $"Materia {m.CaMaId}").Distinct().ToList(),
                    Mensaje = $"Se procesaron {procesados.Count} alumnos y se crearon {creadas} inscripciones."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al confirmar la carga masiva");
                model.Errores.Add(new CargaMasivaFilaErrorDto { Fila = 1, Motivo = "No se pudieron guardar los cambios. No se modificó ningún dato." });
                await CompletarCatalogosPrevisualizacionAsync(model);
                return View("CargaMasivaPrevisualizar", model);
            }
        }

        #endregion


        private void SendWelcomeEmail(string EmailDestino, string token)
        {
            // Reutilizamos la lógica del Url.Action para apuntar al Recovery existente
            var url = Url.Action("Recovery", "Account", new { token = token }, Request.Scheme);
            var urlEscaped = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(url ?? string.Empty);

            var oMailMessage = new System.Net.Mail.MailMessage(
                "softwareinstituto@gmail.com",
                EmailDestino,
                "Activación de cuenta – Sistema de asistencias - ISFDyT124",
                $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 650px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #004d80;'>¡Bienvenido/a al Sistema!</h2>
                        <p>Se ha creado su cuenta para acceder al <strong>Sistema de control de asistencias del instituto ISFDyT124</strong>.</p>
                        <p>Para activar su cuenta y generar su contraseña de acceso, haga clic en el siguiente enlace:</p>
                        <div style='text-align: center; margin: 25px 0;'>
                            <a href='{urlEscaped}'
                               style='display: inline-block; padding: 12px 24px; background-color: #004d80; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                Activar cuenta y crear contraseña
                            </a>
                        </div>
                        <p>Este enlace es válido por una sola vez y expirará en 24 horas.</p>
                        <hr style='border: 0; border-top: 1px solid #eee; margin: 30px 0;' />
                        <p style='font-size: 0.9em; color: #666;'>
                            Sistema creado por estudiantes de Tecnicatura Superior en Desarrollo de Software – 2026<br>
                            <em>Instituto Superior de Formación Docente y Técnica N.º 124</em>
                        </p>
                    </div>"
            );

            oMailMessage.IsBodyHtml = true;

            using var oSmtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com")
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Port = 587,
                Credentials = new System.Net.NetworkCredential("softwareinstituto@gmail.com", "bdyfkwgqtxfahslt")
            };

            oSmtpClient.Send(oMailMessage);
        }

    }
}

using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using ISFDyT124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers
{
    [Authorize(Roles = "Admin,Dirección")]
    public class AdminController : Controller
    {
        private readonly InstitutoDbContext _context;

        public AdminController(InstitutoDbContext context)
        {
            _context = context;
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
            ViewBag.CarreraMateriasList = await _context
                .CarreraMaterias.Include(cm => cm.Carrera)
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    cm.CaMaId,
                    Denominacion = cm.Carrera.CaDenominacion + " / " + cm.Materia.MaDenominacion,
                })
                .ToListAsync();
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalAlumnos = await _context.Usuarios.Where(u => u.RoId == 3).CountAsync();

            ViewBag.TotalDocentes = await _context.Usuarios.Where(u => u.RoId == 2).CountAsync();

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
                .Usuarios.Where(u => u.RoId == 2)
                .Include(u => u.CarreraMaterias)
                    .ThenInclude(cm => cm.Carrera)
                .Include(u => u.CarreraMaterias)
                    .ThenInclude(cm => cm.Materia)
                .ToListAsync();

            var filas = new List<AuditoriaDocenteDto>();

            foreach (var docente in docentes)
            {
                foreach (var catedra in docente.CarreraMaterias)
                {
                    var caCoIds = await _context
                        .CarreraCohortes.Where(cc => cc.CaId == catedra.CaId)
                        .Select(cc => cc.CaCoId)
                        .ToListAsync();

                    var cantidadAlumnos = await _context.Usuarios.CountAsync(u =>
                        u.RoId == 3 && u.CaCoId != null && caCoIds.Contains(u.CaCoId.Value)
                    );

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
                            CarreraDenominacion = catedra.Carrera?.CaDenominacion ?? "-",
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
                    .ThenInclude(cm => cm.Carrera)
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
                                cm.Carrera.CaDenominacion + " / " + cm.Materia.MaDenominacion
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
            await CargarListasFormularioUsuarioAsync();
            return View(new UsuarioCrearDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioAgregar(UsuarioCrearDto model, int selectedRoleId)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasFormularioUsuarioAsync();
                return View(model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.UsDni == model.UsDni))
            {
                ModelState.AddModelError("UsDni", "El DNI ya se encuentra registrado.");
                await CargarListasFormularioUsuarioAsync();
                return View(model);
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

            if (selectedRoleId == 2 && (model.SelectedCaMaIds == null || model.SelectedCaMaIds.Count == 0))
            {
                ModelState.AddModelError(
                    "SelectedCaMaIds",
                    "Debe seleccionar al menos una materia para un Docente."
                );
                await CargarListasFormularioUsuarioAsync();
                return View(model);
            }

            int nuevoUsId = _context.Usuarios.Any()
                ? await _context.Usuarios.MaxAsync(u => u.UsId) + 1
                : 1;

            var usuario = new Usuario
            {
                UsId = nuevoUsId,
                UsApellido = model.UsApellido,
                UsNombre = model.UsNombre,
                UsDni = model.UsDni,
                UsEmail = model.UsEmail,
                // CAMBIO: la contraseña inicial (el propio DNI) se guarda hasheada. Sigue
                // siendo el mismo valor de contraseña por defecto, solo cambia cómo se
                // almacena; el chequeo de "debe cambiar contraseña" en AccountController
                // ahora verifica el hash en vez de comparar strings.
                UsContrasena = PasswordService.HashPassword(model.UsDni.ToString()),
                RoId = selectedRoleId,
                CaCoId = selectedRoleId == 3 ? model.CaCoId : null,
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            if (selectedRoleId == 2 && model.SelectedCaMaIds != null)
            {
                var materias = await _context
                    .CarreraMaterias.Where(cm => model.SelectedCaMaIds.Contains(cm.CaMaId))
                    .ToListAsync();
                foreach (var cm in materias)
                {
                    usuario.CarreraMaterias.Add(cm);
                }
                await _context.SaveChangesAsync();
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

            var dto = new UsuarioDetalleDto
            {
                UsId = usuario.UsId,
                UsApellido = usuario.UsApellido,
                UsNombre = usuario.UsNombre,
                UsEmail = usuario.UsEmail,
                UsDni = usuario.UsDni,
                RoId = usuario.RoId,
                RoDenominacion = usuario.Rol?.RoDenominacion,
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
            UsuarioDetalleDto model,
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

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(UsuariosABM));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsuarioEliminar(int id)
        {
            var usuario = await _context
                .Usuarios.Include(u => u.UsuarioRoles)
                .Include(u => u.CarreraMaterias)
                .FirstOrDefaultAsync(u => u.UsId == id);

            if (usuario != null)
            {
                usuario.CarreraMaterias.Clear();
                _context.UsuarioRoles.RemoveRange(usuario.UsuarioRoles);
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
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
            if (cc == null)
            {
                return Json(new List<object>());
            }

            var materias = await _context.CarreraMaterias
                .Where(cm => cm.CaId == cc.CaId)
                .Include(cm => cm.Materia)
                .Select(cm => new
                {
                    caMaId = cm.CaMaId,
                    denominacion = cm.Materia.MaDenominacion,
                    modalidad = cm.Materia.MaModalidad,
                    modulos = cm.Materia.MaCantModulos
                })
                .ToListAsync();

            return Json(materias);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargaMasivaMapear(CargaMasivaPaso1Dto model)
        {
            if (model.CaCoId == null)
            {
                ModelState.AddModelError("CaCoId", "Debe seleccionar una Carrera / Cohorte.");
            }

            if (model.SelectedCaMaIds == null || !model.SelectedCaMaIds.Any())
            {
                ModelState.AddModelError("SelectedCaMaIds", "Debe seleccionar al menos una materia para inscribir a los alumnos.");
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

            // Inferir mapeo inicial
            var (dniSugerido, apellidoSugerido, nombreSugerido, emailSugerido) = ExcelImportService.InferirMapeo(columnas);

            var mapeoDto = new CargaMasivaMapeoDto
            {
                TempFileToken = tempToken,
                CaCoId = model.CaCoId.Value,
                CarreraCohorteDenominacion = ccDenom,
                SelectedCaMaIds = model.SelectedCaMaIds,
                MateriasSeleccionadasNombres = nombresMaterias,
                ColumnasExcel = columnas,
                ColumnaDni = dniSugerido,
                ColumnaApellido = apellidoSugerido,
                ColumnaNombre = nombreSugerido,
                ColumnaEmail = emailSugerido,
                PreviewRows = previewRows
            };

            return View("CargaMasivaMapear", mapeoDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargaMasivaProcesar(CargaMasivaMapeoDto model)
        {
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
                        var rolNombre = userExistente.RoId == 2 ? "Docente" : (userExistente.RoId == 1 ? "Admin" : "Dirección");
                        parseResult.Errores.Add(new CargaMasivaFilaErrorDto
                        {
                            Fila = fila.Fila,
                            Dni = fila.Dni.ToString(),
                            Apellido = fila.Apellido,
                            Nombre = fila.Nombre,
                            Email = fila.Email,
                            Motivo = $"El DNI pertenece a un usuario existente con rol {rolNombre}. No puede registrarse como Alumno."
                        });
                    }
                }
            }

            // ESTRATEGIA TODO O NADA: Si hay al menos un error, abortar sin guardar nada
            if (parseResult.Errores.Any())
            {
                ExcelImportService.EliminarArchivoTemporal(model.TempFileToken);

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
                ExcelImportService.EliminarArchivoTemporal(model.TempFileToken);
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
                int maxUsId = _context.Usuarios.Any() ? await _context.Usuarios.MaxAsync(u => u.UsId) : 0;
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
                    string estado;
                    int inscripcionesFila = 0;

                    if (dictUsuarios.TryGetValue(fila.Dni, out var existingUser))
                    {
                        currentUsId = existingUser.UsId;
                        estado = "Alumno existente";
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
                        estado = "Nuevo alumno";
                        alumnosNuevos++;

                        var nuevoUsuario = new Usuario
                        {
                            UsId = currentUsId,
                            UsDni = fila.Dni,
                            UsApellido = fila.Apellido,
                            UsNombre = fila.Nombre,
                            UsEmail = fila.Email,
                            UsContrasena = PasswordService.HashPassword(fila.Dni.ToString()),
                            RoId = 3, // Rol Alumno
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
                        Estado = estado,
                        MateriasInscriptas = inscripcionesFila
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                ExcelImportService.EliminarArchivoTemporal(model.TempFileToken);

                var resultadoExitoso = new CargaMasivaResultadoDto
                {
                    EsExitoso = true,
                    Mensaje = $"Se procesaron correctamente {parseResult.FilasValidas.Count} alumnos ({alumnosNuevos} nuevos y {alumnosReutilizados} existentes). Se crearon {inscripcionesCreadas} inscripciones a materias.",
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ExcelImportService.EliminarArchivoTemporal(model.TempFileToken);

                var resultadoFallo = new CargaMasivaResultadoDto
                {
                    EsExitoso = false,
                    Mensaje = $"Ocurrió un error inesperado al guardar los datos en la base de datos: {ex.Message}. Se cancelaron todas las operaciones.",
                    CarreraCohorteDenominacion = ccDenom,
                    TotalFilas = parseResult.FilasValidas.Count
                };

                return View("CargaMasivaResultado", resultadoFallo);
            }
        }

        #endregion
    }
}

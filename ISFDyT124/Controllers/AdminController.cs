using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
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
                UsContrasena = model.UsDni.ToString(),
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
    }
}

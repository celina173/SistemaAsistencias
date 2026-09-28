using System.Security.Claims;
using ISFDyT124.Data;
using ISFDyT124.DTO;
using ISFDyT124.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISFDyT124.Controllers.Api
{
    /// <summary>
    /// Endpoint para la cola offline de asistencia (frente 7 / PWA): recibe por JSON los
    /// registros que el docente cargó sin conexión y quedaron guardados en IndexedDB en su
    /// dispositivo. Separado de ProfesorController porque este lo llama JS vía fetch(), no
    /// un submit de formulario.
    /// </summary>
    [Authorize(Roles = "Docente")]
    [ApiController]
    [Route("api/asistencias")]
    public class AsistenciasApiController : ControllerBase
    {
        private readonly InstitutoDbContext _context;

        public AsistenciasApiController(InstitutoDbContext context)
        {
            _context = context;
        }

        public class SincronizarResultadoItemDto
        {
            public Guid ClientGuid { get; set; }
            public bool Ok { get; set; }
            public string? Error { get; set; }
        }

        /// <summary>
        /// Recibe la cola pendiente y la guarda. Cada fila se procesa de forma independiente
        /// (si una falla, no aborta el resto) y la respuesta indica, por GUID, cuáles quedaron
        /// guardadas — el JS solo borra de IndexedDB las que vinieron marcadas Ok.
        /// </summary>
        [HttpPost("sincronizar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sincronizar([FromBody] List<AsistenciaSincronizarDto> registros)
        {
            var docenteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(docenteIdClaim, out int docenteId))
                return Unauthorized();

            if (registros == null || registros.Count == 0)
                return BadRequest("No se recibieron registros para sincronizar.");

            // Cátedras (CarreraMateria puntuales) que realmente son del docente logueado —
            // un docente no puede sincronizar asistencia de una cátedra que no tiene
            // asignada, ni aunque la mande a mano armando el JSON él mismo. Se valida por
            // CaMaId (la cátedra concreta), no por MaId (la materia en general): antes,
            // tener una cátedra de "Programación I" en una carrera alcanzaba para
            // sincronizar asistencia de OTRA cátedra de "Programación I" en una carrera
            // que no era suya (ticket 5.12/5.16).
            var catedrasPermitidas = await _context
                .Usuarios.Where(u => u.UsId == docenteId)
                .SelectMany(u => u.CarreraMaterias)
                .Select(cm => cm.CaMaId)
                .Distinct()
                .ToListAsync();

            var resultados = new List<SincronizarResultadoItemDto>();

            // Filas ya tocadas en ESTE mismo lote. Sin esto se duplicaba: el chequeo de más
            // abajo consulta la base, pero SaveChanges recién corre al final del bucle, así que
            // al procesar una segunda tanda del mismo alumno/materia/fecha (la cola manda todo
            // lo pendiente junto) la fila agregada un momento antes todavía no estaba en la base
            // y se insertaba de nuevo. Si el mismo día viene repetido, vale la última carga.
            var enEsteLote = new Dictionary<(int? UsId, int CaMaId, DateTime Fecha), Asistencia>();

            foreach (var dto in registros)
            {
                // Fila encolada antes de este cambio (cola v1, sin caMaId todavía) — no hay
                // forma segura de saber a qué cátedra puntual corresponde. Se rechaza con un
                // error claro en vez de guardar a ciegas o adivinar.
                if (dto.CaMaId == null)
                {
                    resultados.Add(new SincronizarResultadoItemDto
                    {
                        ClientGuid = dto.ClientGuid,
                        Ok = false,
                        Error = "Este registro quedó pendiente de una versión anterior de la app. Volvé a cargar esta asistencia.",
                    });
                    continue;
                }

                if (!catedrasPermitidas.Contains(dto.CaMaId!.Value))
                {
                    resultados.Add(new SincronizarResultadoItemDto
                    {
                        ClientGuid = dto.ClientGuid,
                        Ok = false,
                        Error = "No tenés esa cátedra asignada.",
                    });
                    continue;
                }

                if (dto.Fecha.Date > DateTime.Today)
                {
                    resultados.Add(new SincronizarResultadoItemDto
                    {
                        ClientGuid = dto.ClientGuid,
                        Ok = false,
                        Error = "No se puede registrar asistencia de fechas futuras.",
                    });
                    continue;
                }

                // 1) ¿Este mismo envío ya se sincronizó antes? (reintento de la cola, por ejemplo
                //    si la respuesta anterior se cortó a mitad de camino). Si ya existe con este
                //    GUID, no hay nada más que hacer.
                var yaSincronizado = await _context.Asistencias.AnyAsync(a => a.AsClientGuid == dto.ClientGuid);
                if (yaSincronizado)
                {
                    resultados.Add(new SincronizarResultadoItemDto { ClientGuid = dto.ClientGuid, Ok = true });
                    continue;
                }

                bool justificado = dto.Presente ? false : dto.Justificacion;

                // 2) Mismo criterio de upsert que ya usa ProfesorController.Asistencia: si el
                //    docente (u otro con acceso a la misma cátedra) ya cargó ese alumno+cátedra+
                //    fecha por otra vía mientras el dispositivo estaba offline, se actualiza esa
                //    fila en vez de duplicarla. Se matchea por CaMaId (cátedra puntual), no por
                //    MaId, por la misma razón que en ProfesorController (ticket 5.12).
                var clave = (dto.UsId, dto.CaMaId.Value, dto.Fecha.Date);

                if (!enEsteLote.TryGetValue(clave, out var existente))
                {
                    existente = await _context.Asistencias.FirstOrDefaultAsync(a =>
                        a.UsId == dto.UsId
                        && a.CaMaId == dto.CaMaId
                        && a.AsFecha != null
                        && a.AsFecha.Value.Date == dto.Fecha.Date
                    );
                }

                if (existente != null)
                {
                    existente.AsPresente = dto.Presente;
                    existente.AsJustificacion = justificado;
                    existente.AsClientGuid = dto.ClientGuid;
                    existente.AsFechaCarga = dto.FechaCarga;
                }
                else
                {
                    existente = new Asistencia
                    {
                        AsFecha = dto.Fecha.Date,
                        AsFechaCarga = dto.FechaCarga,
                        AsPresente = dto.Presente,
                        AsJustificacion = justificado,
                        UsId = dto.UsId,
                        MaId = dto.MaId,
                        CaMaId = dto.CaMaId,
                        AsClientGuid = dto.ClientGuid,
                    };
                    _context.Asistencias.Add(existente);
                }

                enEsteLote[clave] = existente;

                resultados.Add(new SincronizarResultadoItemDto { ClientGuid = dto.ClientGuid, Ok = true });
            }

            await _context.SaveChangesAsync();
            return Ok(resultados);
        }
    }
}

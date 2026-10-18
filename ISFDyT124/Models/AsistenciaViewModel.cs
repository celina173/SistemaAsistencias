
using System;
using System.Collections.Generic;

namespace ISFDyT124.Models
{
    public class AsistenciaRowViewModel
    {
        public int UsId { get; set; }
        public string FullName { get; set; } = string.Empty;

        public bool AsPresente { get; set; }
        public bool AsJustificacion { get; set; }
        public string? AsObservacion { get; set; }

        // Porcentaje original o editado de una jornada histórica.
        public decimal PorcentajeEditado { get; set; }

        // Indica que el estudiante se inscribió después
        // de la fecha de asistencia que se está editando.
        public bool BloqueadaPorInscripcion { get; set; }

        public List<bool> Modulos { get; set; } = new List<bool> { false, false, false, false };
        public bool TieneDetalleModulos { get; set; }
    }

    public class AsistenciaFormViewModel
    {
        public int? CaMaId { get; set; }
        public DateTime? FechaAsistencia { get; set; }
        public int ModuleCount { get; set; } = 1;

        // Identifica el modo de edición histórica del formulario.
        public bool EsEdicionHistorica { get; set; }

        public List<AsistenciaRowViewModel> Rows { get; set; } =
            new List<AsistenciaRowViewModel>();
    }
}

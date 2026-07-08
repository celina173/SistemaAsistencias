using System.Collections.Generic;

namespace ISFDyT124.Models
{
    public class AsistenciaRowViewModel
    {
        public int UsId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool AsPresente { get; set; }
        public bool AsJustificacion { get; set; }
        public List<bool> Modulos { get; set; } = new List<bool> { false, false, false, false };
    }

    public class AsistenciaFormViewModel
    {
        public int? CaMaId { get; set; }
        // Número de módulos que tiene la materia (se usa en el controlador)
        public int ModuleCount { get; set; } = 1;
        public List<AsistenciaRowViewModel> Rows { get; set; } = new List<AsistenciaRowViewModel>();
    }
}
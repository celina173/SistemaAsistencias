using System.Collections.Generic;

namespace ISFDyT124.Models
{
    public class AsistenciaRowViewModel
    {
        public int UsId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool AsPresente { get; set; }
        public bool AsJustificacion { get; set; }
        public List<string> Modulos { get; set; } = new List<string>();
    }

    public class AsistenciaFormViewModel
    {
        public int ModuleCount { get; set; } = 1;
        public int? CaMaId { get; set; }
        public List<AsistenciaRowViewModel> Rows { get; set; } = new List<AsistenciaRowViewModel>();
    }
}
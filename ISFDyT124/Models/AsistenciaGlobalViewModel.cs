namespace ISFDyT124.Models
{
    public class AsistenciaGlobalViewModel
    {
        public int? CaMaId { get; set; }
        public List<DateTime> Fechas { get; set; } = new();        // columnas
        public List<AsistenciaGlobalRowViewModel> Rows { get; set; } = new(); // filas
    }


    public class AsistenciaGlobalRowViewModel
    {
        public int UsId { get; set; }
        public string FullName { get; set; } = string.Empty;
        // fecha → porcentaje de asistencia en esa fecha (0-100)
        public Dictionary<DateTime, decimal> AsistenciaPorFecha { get; set; } = new();
        public decimal PorcentajeAsistencia { get; set; }
    }
}

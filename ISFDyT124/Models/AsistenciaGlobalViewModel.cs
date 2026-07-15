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
        public string FullName { get; set; }
        public Dictionary<DateTime, decimal> AsistenciaPorFecha { get; set; } = new(); // ← decimal en lugar de bool
        public decimal PorcentajeAsistencia { get; set; }
    }
}

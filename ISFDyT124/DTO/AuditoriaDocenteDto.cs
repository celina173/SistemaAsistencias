namespace ISFDyT124.DTO
{
    public class AuditoriaDocenteDto
    {
        public int UsId { get; set; }
        public string? DocenteNombre { get; set; }
        public int CaMaId { get; set; }
        public string? CarreraDenominacion { get; set; }
        public string? MateriaDenominacion { get; set; }
        public int CantidadAlumnos { get; set; }
        public int CantidadFechasCargadas { get; set; }
        public DateTime? UltimaFechaCargada { get; set; }
    }
}

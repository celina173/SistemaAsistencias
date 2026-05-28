namespace ISFDyT124.DTO
{
    public class UsuarioDetalleDto
    {
        public int UsId { get; set; }
        public string? UsApellido { get; set; }
        public string? UsNombre { get; set; }
        public string? UsEmail { get; set; }
        public string NombreCompleto => $"{UsApellido}, {UsNombre}";
        public int UsDni { get; set; }
        public int RoId { get; set; }
        public string? RoDenominacion { get; set; }
    }
}

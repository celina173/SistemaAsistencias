namespace ISFDyT124.DTO
{
    public class AsistenciaDetalleDto
    {
        public int AsId { get; set; }
        public int? UsId { get; set; }
        public int? MaId { get; set; }
        public DateTime? AsFecha { get; set; }
        public bool AsPresente { get; set; }
        public bool AsJustificacion { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? MateriaDenominacion { get; set; }
    }
}

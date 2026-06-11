namespace ISFDyT124.DTO
{
    public class AsistenciaCrearDto
    {
        public int? UsId { get; set; }
        public int? MaId { get; set; }
        public DateTime? AsFecha { get; set; }
        public bool AsPresente { get; set; }
        public bool AsJustificacion { get; set; }
    }
}

namespace ISFDyT124.DTO
{
    public class UsuarioCrearDto
    {
        public string? UsApellido { get; set; }
        public string? UsNombre { get; set; }
        public string? UsEmail { get; set; }
        public int UsDni { get; set; }
        public int RoId { get; set; }
        public int? CaCoId { get; set; }
        public List<int>? SelectedCaMaIds { get; set; }
    }
}

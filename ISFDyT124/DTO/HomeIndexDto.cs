namespace ISFDyT124.DTO
{
    public class HomeIndexDto
    {
        public List<CarreraDetalleDto> Carreras { get; set; } = new();
        public List<MateriaDetalleDto> Materias { get; set; } = new();
        public int? SelectedCarreraId { get; set; }
        public int? SelectedMateriaId { get; set; }
    }
}

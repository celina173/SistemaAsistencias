namespace ISFDyT124.Models
{
    public class CarrerasMaterias
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("Carrera")]
        public int ca_id { get; set; }

        [ForeignKey("Materia")]
        public int ma_id { get; set; }

        public Carrera? Carrera { get; set; }

        public Materia? Materia { get; set; }
    }
}
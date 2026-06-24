namespace ISFDyT124.Models
{
    public class Materia
    {
        [Key]
        public int ma_id { get; set; }

        [Required]
        [StringLength(100)]
        public string ma_denominacion { get; set; }

        [StringLength(50)]
        public string ma_modalidad { get; set; }

        public int ma_cant_modulos { get; set; }

        public ICollection<CarrerasMaterias>? CarrerasMaterias { get; set; }
    }
}

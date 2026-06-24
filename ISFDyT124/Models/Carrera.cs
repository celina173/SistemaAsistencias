namespace ISFDyT124.Models
{
    public class Carrera
    {
        [Key]
        public int ca_id { get; set; }

        [Required]
        [StringLength(100)]
        public string ca_denominacion { get; set; }

        [Required]
        [StringLength(20)]
        public string ca_codigo { get; set; }

        
        public ICollection<CarrerasMaterias>? CarrerasMaterias { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Carrera
    {
        [Key]
        public int CaId { get; set; }

        [Required]
        [StringLength(100)]
        public string CaDenominacion { get; set; }

        
        public ICollection<CarrerasMaterias>? CarrerasMaterias { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Cohorte
    {
        [Key]
        public int CoId { get; set; }

        [Required]
        [Display(Name = "Año")]
        public int CoAnio { get; set; }

        public virtual ICollection<CarreraCohorte> CarreraCohortes { get; set; } = new List<CarreraCohorte>();
    }
}

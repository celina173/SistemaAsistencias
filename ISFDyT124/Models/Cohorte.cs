using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Cohorte
    {
        [Key]
        public int CoId { get; set; }

        [Required(ErrorMessage = "Debe ingresar un año para la cohorte.")]
        [Range(2000, 2100, ErrorMessage = "Ingrese un año válido.")]
        [Display(Name = "Año")]
        public int CoAnio { get; set; }

        [Display(Name = "Estado")]
        public bool CoEstado { get; set; }

        public virtual ICollection<CarreraCohorte> CarreraCohortes { get; set; } = new List<CarreraCohorte>();
    }
}

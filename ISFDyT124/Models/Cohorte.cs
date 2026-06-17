using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Cohorte
    {
        [Key]
        [Display(Name = "ID Cohorte")]
        public int CoId { get; set; }

        [Required(ErrorMessage = "Debe ingresar una año para la cohorte.")]
        [Display(Name = "Denominación")]
        public string CoAnio { get; set; } = null!;

        // Relación: una cohorte puede estar en muchas carreras
        public virtual ICollection<CarreraCohorte>? CarreraCohortes { get; set; }
    }
}

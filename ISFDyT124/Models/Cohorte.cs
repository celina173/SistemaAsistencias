using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Cohorte
    {
        [Key]
        [Display(Name = "ID Cohorte")]
        public int CoId { get; set; }

        [Required(ErrorMessage = "Debe ingresar una denominación para la cohorte.")]
        [StringLength(50, ErrorMessage = "No se permiten más de 50 caracteres.")]
        [Display(Name = "Denominación")]
        public string CoDenominacion { get; set; } = null!;

        // Relación: una cohorte puede estar en muchas carreras
        public virtual ICollection<CarreraCohorte>? CarreraCohortes { get; set; }
    }
}

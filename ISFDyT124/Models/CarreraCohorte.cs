using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarreraCohorte
    {
        [Key]
        [Display(Name = "ID Relación")]
        public int CaCoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una carrera.")]
        [Display(Name = "Carrera")]
        [ForeignKey("Carrera")]
        public int CaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una cohorte.")]
        [Display(Name = "Cohorte")]
        [ForeignKey("Cohorte")]
        public int CoId { get; set; }

        // Navegación
        public virtual Carrera? Carrera { get; set; }
        public virtual Cohorte? Cohorte { get; set; }
    }
}

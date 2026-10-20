using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarreraMateria
    {
        [Key]
        [Display(Name = "ID Relación")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CaMaId { get; set; }

        [Display(Name = "Carrera-Cohorte")]
        [ForeignKey("CarreraCohorte")]
        public int? CaCoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una materia.")]
        [Display(Name = "Materia")]
        [ForeignKey("Materia")]
        public int MaId { get; set; }

        [Display(Name = "Año de cursada")]
        public int? CaMaAnio { get; set; }

        // Navegación
        public virtual CarreraCohorte? CarreraCohorte { get; set; }
        public virtual Materia? Materia { get; set; }
        public virtual ICollection<CarreraMateriaDiaCursada>? DiasCursada { get; set; }
    }
}

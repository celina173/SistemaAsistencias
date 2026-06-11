using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarreraMateria
    {
        [Key]
        [Display(Name = "ID Relación")]
        public int CaMaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una carrera.")]
        [Display(Name = "Carrera")]
        [ForeignKey("Carrera")]
        public int CaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una materia.")]
        [Display(Name = "Materia")]
        [ForeignKey("Materia")]
        public int MaId { get; set; }

        // Navegación
        public virtual Carrera? Carrera { get; set; }
        public virtual Materia? Materia { get; set; }
    }
}

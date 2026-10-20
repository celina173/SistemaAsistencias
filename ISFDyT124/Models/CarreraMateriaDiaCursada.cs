using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarreraMateriaDiaCursada
    {
        [Key]
        [Display(Name = "ID Día de Cursada")]
        public int CaMaDiId { get; set; }

        [Required]
        [Display(Name = "Carrera-Materia")]
        [ForeignKey(nameof(CarreraMateria))]
        public int CaMaId { get; set; }

        [Required]
        [Display(Name = "Día de Cursada")]
        public DayOfWeek DiaSemana { get; set; }

        // Relación con la asignación de materia a la cohorte
        public virtual CarreraMateria? CarreraMateria { get; set; }
    }
}
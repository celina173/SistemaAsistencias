using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Materia
    {
        [Key]
        [Display(Name = "ID Materia")]
        public int MaId { get; set; }

        [Required(ErrorMessage = "Debe ingresar una denominacion para la materia.")]
        [StringLength(100, ErrorMessage = "No se permiten mas de 100 caracteres.")]
        [Display(Name = "Denominacion")]
        public string MaDenominacion { get; set; } = null!;

        [StringLength(50, ErrorMessage = "No se permiten mas de 50 caracteres.")]
        [Display(Name = "Modalidad")]
        public string? MaModalidad { get; set; }

        [Display(Name = "Cantidad de Modulos")]
        public int? MaCantModulos { get; set; }

        // RELACION
        public virtual ICollection<CarreraMateria>? CarreraMaterias { get; set; }
    }
}

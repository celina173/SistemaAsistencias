using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Carrera
    {
        [Key]
        [Display(Name = "ID Carrera")]
        public int CaId { get; set; }

        [Required(ErrorMessage = "Debe ingresar una denominación para la carrera.")]
        [StringLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")]
        [RegularExpression(
            @"^[A-Za-zÁÉÍÓÚáéíóúÜüÑñ\s.,()-]*$",
            ErrorMessage = "Ingrese una denominación válida."
        )]
        [Display(Name = "Denominación")]
        public string CaDenominacion { get; set; }



        // RELACION
        // CarreraMateria ya no cuelga directo de Carrera: una cátedra (Carrera+Materia)
        // ahora está atada a una cohorte concreta vía CarreraCohorte.CarreraMaterias.
        public virtual ICollection<CarreraCohorte>? CarreraCohortes { get; set; }
    }
}

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
            @"^[A-Za-zÁÉÍÓÚáéíóúÜüÑñ0-9\s.,()-]*$",
            ErrorMessage = "Ingrese una denominación válida."
        )]
        [Display(Name = "Denominación")]
        public string CaDenominacion { get; set; }



        // RELACION
        public virtual ICollection<CarreraMateria>? CarreraMaterias { get; set; }
        public virtual ICollection<CarreraCohorte>? CarreraCohortes { get; set; }
    }
}

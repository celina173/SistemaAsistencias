using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Carrera
    {
        [Key]
        [Display(Name = "ID Carrera")]
        public int ca_id { get; set; }


       
        [Required(ErrorMessage = "Debe ingresar una denominación para la carrera.")]
        [StringLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")]
        [RegularExpression(
            @"^[A-Za-zÁÉÍÓÚáéíóúÜüÑñ0-9\s.,()-]*$",
            ErrorMessage = "Ingrese una denominación válida."
        )]
        [Display(Name = "Denominación")]
        public string ca_denominacion { get; set; } = null!;



        // RELACION
        public virtual ICollection<CarrerasMaterias>? CarrerasMaterias { get; set; }
    }
}

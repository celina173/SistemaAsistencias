using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Materia
    {
        [Key]
        [Display(Name = "ID Materia")]
        public int MaId { get; set; }

        [Required(ErrorMessage = "Debe ingresar el nombre de la materia.")]
        [StringLength(30, ErrorMessage = "No se permiten más de 30 caracteres.")]
        [RegularExpression(
            @"^[A-Za-zÁÉÍÓÚáéíóúÜüÑñ0-9\s.,()-]*$",
            ErrorMessage = "Ingrese una materia válida."
        )]
        [Display(Name = "Materia")]
        public string MaDenominacion { get; set; }

        [Required(ErrorMessage = "Debe ingresar una modalidad.")]
        [StringLength(25, ErrorMessage = "No se permiten más de 25 caracteres.")]
        [RegularExpression(
            @"^[A-Za-zÁÉÍÓÚáéíóúÜüÑñ\s]*$",
            ErrorMessage = "Ingrese una modalidad válida."
        )]
        [Display(Name = "Modalidad")]
        public string? MaModalidad { get; set; }

        [Required(ErrorMessage = "Debe ingresar la cantidad de módulos.")]
        [Range(1, 4, ErrorMessage = "La cantidad de módulos debe estar entre 1 y 4.")]
        [Display(Name = "Cantidad de Módulos")]
        public int? MaCantModulos { get; set; }

        // RELACION
        public virtual ICollection<CarrerasMaterias>? CarrerasMaterias { get; set; }
    }
}

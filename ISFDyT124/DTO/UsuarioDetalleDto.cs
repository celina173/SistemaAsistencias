using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.DTO
{
    public class UsuarioDetalleDto
    {
        public int UsId { get; set; }

        [RegularExpression(
            @"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]*$",
            ErrorMessage = "Ingrese un apellido válido."
        )]
        [MaxLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")]
        [Required(ErrorMessage = "Debe ingresar un Apellido")]
        [Display(Name = "Apellido")]
        public string? UsApellido { get; set; }

        [RegularExpression(
            @"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]*$",
            ErrorMessage = "Ingrese un nombre válido."
        )]
        [MaxLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")]
        [Required(ErrorMessage = "Debe ingresar un Nombre.")]
        [Display(Name = "Nombres")]
        public string? UsNombre { get; set; }

        [Required(ErrorMessage = "Debe ingresar un Email válido")]
        [EmailAddress(ErrorMessage = "Ingrese una dirección de mail válida")]
        [Display(Name = "Email")]
        public string? UsEmail { get; set; }

        public string NombreCompleto => $"{UsApellido}, {UsNombre}";

        [RegularExpression(
            @"^[1-9][0-9]*$",
            ErrorMessage = "Sólo se permiten números de DNI válidos."
        )]
        [Range(6000000, 99999999, ErrorMessage = "Debe ingresar los 7-8 dígitos del DNI.")]
        [Required(ErrorMessage = "Debe ingresar un número de DNI válido (8 dígitos).")]
        [Display(Name = "DNI")]
        public int UsDni { get; set; }

        public int RoId { get; set; }
        public string? RoDenominacion { get; set; }
        public int? CaCoId { get; set; }
        public string? CarreraCohorteDenominacion { get; set; }
        public string? MateriasDenominacion { get; set; }
    }
}

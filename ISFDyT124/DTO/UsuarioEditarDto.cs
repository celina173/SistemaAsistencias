using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.DTO
{
    public class UsuarioEditarDto
    {
        public int UsId { get; set; }

        //APELLIDO
        [RegularExpression(
            @"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]*$",
            ErrorMessage = "Ingrese un apellido válido."
        )]
        [MaxLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")]
        [Required(ErrorMessage = "Debe ingresar un Apellido")]
        [Display(Name = "Apellido")]
        public string? UsApellido { get; set; }

        //NOMBRE
        [RegularExpression(
            @"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]*$",
            ErrorMessage = "Ingrese un nombre válido."
        )]
        [MaxLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")]
        [Required(ErrorMessage = "Debe ingresar un Nombre.")]
        [Display(Name = "Nombres")]
        public string? UsNombre { get; set; }

        //EMAIL
        [Required(ErrorMessage = "Debe ingresar un Email válido")]
        [EmailAddress(ErrorMessage = "Ingrese una dirección de mail válida")]
        [MaxLength(254, ErrorMessage = "No se permiten más de 254 caracteres.")]
        [Display(Name = "Email")]
        public string? UsEmail { get; set; }

        //DNI
        [RegularExpression(
            @"^[1-9][0-9]*$",
            ErrorMessage = "Sólo se permiten números de DNI válidos."
        )]
        [Range(1000000, 99999999, ErrorMessage = "Debe ingresar los 7-8 dígitos del DNI.")]
        [Required(ErrorMessage = "Debe ingresar un número de DNI válido (7-8 dígitos).")]
        [Display(Name = "DNI")]
        public int UsDni { get; set; }

        //Relacion de Usuario - Rol
        [ForeignKey("Rol")]
        [Required(ErrorMessage = "Debe elegir un rol.")]
        [Display(Name = "Rol")]
        public int RoId { get; set; }

        // CarreraCohorte asignado (solo para Alumnos)
        [Display(Name = "Carrera / Cohorte")]
        [ForeignKey("CarreraCohorte")]
        public int? CaCoId { get; set; }

        // Ids de CarreraMateria ya asignadas, para preseleccionar los checkboxes en la vista
        public string? MateriasDenominacion { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.DTO
{
    /// <summary>
    /// Datos del formulario de alta y edición de estudiante (AlumnosController).
    /// Mismas validaciones que UsuarioCrearDto pero sin rol, contraseña ni materias:
    /// el estudiante es un dato, no un usuario que se loguea, y su rol lo fija el
    /// servidor. En edición, UsId viene por hidden.
    /// </summary>
    public class AlumnoFormDto
    {
        // Solo se usa en edición; en alta queda null.
        public int? UsId { get; set; }

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

        [RegularExpression(
            @"^[1-9][0-9]*$",
            ErrorMessage = "Sólo se permiten números de DNI válidos."
        )]
        [Range(6000000, 99999999, ErrorMessage = "Debe ingresar los 7-8 dígitos del DNI.")]
        [Required(ErrorMessage = "Debe ingresar un número de DNI válido (8 dígitos).")]
        [Display(Name = "DNI")]
        public int UsDni { get; set; }

        // La obligatoriedad se valida en el controller (mensaje propio) para no
        // acoplar el DTO a la regla "un estudiante siempre tiene carrera".
        [Display(Name = "Carrera / Cohorte")]
        [ForeignKey("CarreraCohorte")]
        public int? CaCoId { get; set; }
    }
}

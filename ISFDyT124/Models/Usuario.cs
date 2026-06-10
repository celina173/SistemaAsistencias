using System.ComponentModel; // Importa atributos para descripción y propiedades de datos 
using System.ComponentModel.DataAnnotations; // Importa atributos para validaciones y anotaciones en propiedades 
using System.ComponentModel.DataAnnotations.Schema; // Importa atributos para relaciones y configuraciones con la base de datos 



namespace ISFDyT124.Models
{
    public class Usuario
    {
        //ID
        [Key] 
        public int UsId { get; set; }


        //APELLIDO
        [RegularExpression(@"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]*$", ErrorMessage = "Ingrese un apellido válido.")] 
        [MaxLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")] // Limita longitud a 100 caracteres 
        [Required(ErrorMessage = "Debe ingresar un Apellido")] // Campo obligatorio
        [Display(Name = "Apellido")] // Etiqueta para vistas
        public string UsApellido { get; set; } = null!; // Propiedad para el apellido - Negado nulo



        //NOMBRE
        [RegularExpression(@"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]*$", ErrorMessage = "Ingrese un nombre válido.")]// Valida que el nombre solo contenga letras y espacios 
        [MaxLength(100, ErrorMessage = "No se permiten más de 100 caracteres.")] // Limita longitud a 100 caracteres 
        [Required(ErrorMessage = "Debe ingresar un Nombre.")] // Campo obligatorio
        [Display(Name = "Nombres")] // Etiqueta para vistas 
        public string UsNombre { get; set; } = null!; // Propiedad para el nombre - Negado nulo


        //DNI
        [RegularExpression(@"^[1-9][0-9]*$", ErrorMessage = "Sólo se permiten números de DNI válidos.")]
        [Range(6000000, 99999999, ErrorMessage = "Debe ingresar los 7-8 dígitos del DNI.")] // Valida que el DNI tenga un rango válido entre 7 y 8 dígitos 
        [Required(ErrorMessage = "Debe ingresar un número de DNI válido (8 dígitos).")] // Campo obligatorio 
        [Display(Name = "DNI")] // Etiqueta para mostrar en vistas y formularios
        public int UsDni { get; set; } // Propiedad para almacenar el DNI del usuario


        //EMAIL
        [Required]
        [EmailAddress(ErrorMessage = "Ingrese una dirección de mail válida")]
        public string? UsEmail { get; set; }

        //CONTRASEÑA
        [Required(ErrorMessage = "Ingrese una contraseña válida")]
        [Display(Name = "Contraseña")]
        public string UsContrasena { get; set; }

        //Relacion de Usuario - Rol
        [ForeignKey("Rol")] // Indica que la propiedad RolId es clave foránea hacia la entidad Rol
        [Required(ErrorMessage = "Debe elegir un rol.")] // Campo obligatorio elegir rol
        [Display(Name = "Rol")] // Etiqueta para vistas 
        public int RoId { get; set; } // Clave foránea al rol del usuario

        // CarreraCohorte asignado (solo para Alumnos)
        [Display(Name = "Carrera / Cohorte")]
        public int? CaCoId { get; set; }



        public virtual Rol? Rol { get; set; } = null!; // Relaciónes de Usuario a Rol
        public virtual CarreraCohorte? CarreraCohorte { get; set; }

        public virtual ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
        public virtual ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
        public virtual ICollection<CarreraMateria> CarreraMaterias { get; set; } = new List<CarreraMateria>();
    }
}

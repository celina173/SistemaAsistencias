using System.ComponentModel.DataAnnotations; // Importa atributos para validación y anotaciones de datos 


namespace ISFDyT124.Models
{
    public class Rol
    {
    

        [Key] // Marca esta propiedad como clave primaria de la tabla 
        public int RoId { get; set; } 

        [RegularExpression(@"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]*$", ErrorMessage = "Solo se permiten letras.")]// Validación que solo permite letras (mayúsculas, minúsculas, letras con acentos y espacios) 

        [Display(Name = "Rol")] // Etiqueta para mostrar en vistas/formularios
        [MaxLength(50, ErrorMessage = "No se permiten más de 50 caracteres.")] // Limita la longitud máxima a 50 caracteres 
        [Required(ErrorMessage = "Debe ingresar un Rol")] // Campo obligatorio, debe contener un valor
        public string RoDenominacion { get; set; } = null!; 


        // Relación con usuarios: un rol puede estar asignado a muchos usuarios
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
        // Colección de usuarios que tienen este rol. Se inicializa como una lista vacía para evitar problemas de referencia nula al agregar usuarios a un rol.
    }
}

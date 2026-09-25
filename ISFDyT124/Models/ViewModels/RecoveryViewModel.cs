using System.ComponentModel.DataAnnotations; // Importa atributos para validación de datos 


namespace ISFDyT124.Models.ViewModels // Define el espacio de nombres para modelos de vista
{
    public class RecoveryViewModel // Modelo para la vista de inicio de recuperación por email 
    {
        [EmailAddress(ErrorMessage = "Ingrese una dirección de correo válida.")] // Valida formato de email correcto
        [Required(ErrorMessage = "El campo Email es obligatorio")] // Campo obligatorio con mensaje personalizado
        public string? UsEmail { get; set; } // Email del usuario para iniciar recuperación, puede ser nulo pero requerido
    }
}


using System.ComponentModel.DataAnnotations; // Importa atributos para validación de datos 


namespace ISFDyT124.Models.ViewModels // Define el espacio de nombres para modelos de vista
{
    public class RecoveryPasswordViewModel // Modelo para la vista de recuperación de contraseña con token y dos contraseñas
    {
        public string? UsTokenRecovery { get; set; } // Token enviado para validar la recuperación(puede ser nulo)


        [Required] // UsContrasena es obligatorio 
        public string? UsContrasena { get; set; } // Nueva contraseña que el usuario quiere establecer(puede ser nulo pero obligatorio)


        [Required] // UsContrasena2 es obligatorio 
        public string? UsContrasena2 { get; set; } // Confirmación de la contraseña nueva, obligatoria
    }
}
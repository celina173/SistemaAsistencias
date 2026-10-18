
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class AsistenciaModulo
    {
        // Identificador único del registro del módulo.
        [Key]
        public int AsMoId { get; set; }

        // Clave foránea de la asistencia a la que pertenece.
        [Required]
        public int AsId { get; set; }

        // Número del módulo dentro de la clase (1, 2, 3, 4, etc.).
        [Required]
        public int NumeroModulo { get; set; }

        // Indica si el estudiante estuvo presente en este módulo.
        [Required]
        public bool AsMoPresente { get; set; } = false;

        // Relación con la asistencia.
        [ForeignKey(nameof(AsId))]
        public virtual Asistencia Asistencia { get; set; } = null!;
    }
}

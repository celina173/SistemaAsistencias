using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Asistencia
    {
        // Identificador único del registro.
        [Key]
        public int AsId { get; set; }

        // Fecha y hora exacta en la toma de asistencia.
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha y Hora")]
        public DateTime? AsFecha { get; set; }

        //Bloque horario de la clase.
        [Display(Name = "Presente")]
        public bool AsPresente { get; set; } = false;

        // Motivo o justificación en caso de ausencia.
        [Display(Name = "Justificación")]
        public bool AsJustificacion { get; set; } = false;

        // Clave foránea que conecta con el estudiante.
        public int? UsId { get; set; }

        // Clave foránea que conecta con la a materia
        public int? MaId { get; set; }

        // Conexión hacia el modelo Usuario.
        public virtual Usuario? Usuario { get; set; }

        // Conexión hacia el modelo Materia.
        public virtual Materia? Materia { get; set; }
    }
}

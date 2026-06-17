using ISFDyT124.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{

    // Representa el modelo de datos para registrar la asistencia a una clase. 
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
        public int? UsId { get; set; } // Por el momento no se utiliza

        // Clave foránea que conecta con la a materia
        public int? MaId { get; set; }

        // Clave foránea que conecta con Carreras_Materias (opcional)
        public int? CaMaId { get; set; }

        // Conexión hacia el modelo Usuarios.
        public virtual Usuario? Usuario { get; set; } // Por el momento no se utiliza

        // Conexión hacia el modelo Materias.
        public virtual Materia? Materias { get; set; } // Por el momento no se utiliza

        // Relación opcional hacia Carreras_Materias cuando la asistencia se vincula a una carrera/materia
        public virtual CarrerasMaterias? CarreraMateria { get; set; }
    }
}

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

        // Cuándo se cargó/guardó el registro de verdad (distinto de AsFecha, que es la
        // fecha de la clase). Para el flujo online normal es prácticamente el mismo
        // momento; para un registro que llegó de la cola offline, es el momento real en
        // que el docente lo tomó en el dispositivo, no el momento en que llegó al servidor.
        [Display(Name = "Fecha de carga")]
        public DateTime? AsFechaCarga { get; set; }

        // Identificador generado en el cliente (navegador) al encolar un registro offline.
        // Permite que el servidor detecte reintentos de sincronización duplicados sin
        // depender únicamente del upsert por (UsId + MaId + Fecha).
        public Guid? AsClientGuid { get; set; }

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
        public virtual CarreraMateria? CarreraMateria { get; set; }
    }
}

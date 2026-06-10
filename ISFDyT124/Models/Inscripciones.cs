using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Inscripciones
    {
        [Key]
        public int InId { get; set; }
        public int UsId { get; set; }
        public int CaMaId { get; set; }
        public virtual Usuario? Usuarios { get; set; }
        public virtual CarrerasMaterias? Carreras_Materias { get; set; }
    }
}
